using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PetBehavior;

// Common interface for pet controllers that can execute commands
public interface IPetController
{
    IEnumerator ExecuteStateTransition(PetActionState targetState);
    IEnumerator ExecuteMovementCommand(Vector3 destination);
    IEnumerator ExecuteLLMMovementCommand(Vector3 destination, float speed);
    IEnumerator ExecuteRandomMovementCommand(float speed, float maxDistance);
    IEnumerator ExecuteDurationBasedStateTransition(PetActionState targetState, float duration);
    IEnumerator ExecuteFollowCameraCommand(Camera targetCamera, float updateInterval);
}

// Command system for queued actions
public abstract class PetCommand
{
    public abstract IEnumerator Execute(IPetController controller);
}

public class StateTransitionCommand : PetCommand
{
    public PetActionState targetState;
    
    public StateTransitionCommand(PetActionState state)
    {
        targetState = state;
    }
    
    public override IEnumerator Execute(IPetController controller)
    {
        yield return controller.ExecuteStateTransition(targetState);
    }
}

public class MovementCommand : PetCommand
{
    public Vector3 destination;
    
    public MovementCommand(Vector3 dest)
    {
        destination = dest;
    }
    
    public override IEnumerator Execute(IPetController controller)
    {
        yield return controller.ExecuteMovementCommand(destination);
    }
}

// LLM-specific command classes
public class LLMMovementCommand : PetCommand
{
    public Vector3 destination;
    public string movementType; // "Walk" or "Run"
    public float speed;
    
    public LLMMovementCommand(Vector3 dest, string type, float moveSpeed)
    {
        destination = dest;
        movementType = type;
        speed = moveSpeed;
    }
    
    public override IEnumerator Execute(IPetController controller)
    {
        yield return controller.ExecuteLLMMovementCommand(destination, speed);
    }
}

public class RandomMovementCommand : PetCommand
{
    public string movementType;
    public float speed;
    public float maxDistance;
    
    public RandomMovementCommand(string type, float moveSpeed, float maxDist = 5f)
    {
        movementType = type;
        speed = moveSpeed;
        maxDistance = maxDist;
    }
    
    public override IEnumerator Execute(IPetController controller)
    {
        yield return controller.ExecuteRandomMovementCommand(speed, maxDistance);
    }
}

public class DurationBasedStateCommand : PetCommand
{
    public PetActionState targetState;
    public float duration;
    
    public DurationBasedStateCommand(PetActionState state, float dur)
    {
        targetState = state;
        duration = dur;
    }
    
    public override IEnumerator Execute(IPetController controller)
    {
        yield return controller.ExecuteDurationBasedStateTransition(targetState, duration);
    }
}

public class FollowCameraCommand : PetCommand
{
    public Camera targetCamera;
    public float updateInterval;
    
    public FollowCameraCommand(Camera camera, float interval = 0.1f)
    {
        targetCamera = camera;
        updateInterval = interval;
    }
    
    public override IEnumerator Execute(IPetController controller)
    {
        yield return controller.ExecuteFollowCameraCommand(targetCamera, updateInterval);
    }
}

[RequireComponent(typeof(NavMeshAgent))]
public class PetMoveVR : MonoBehaviour, IPetController
{
    private NavMeshAgent agent;
    private Animator animator;
    [SerializeField] private Camera mainCamera;
    
    private PetAnimationController animationController;
    private PetActionStateMachine stateMachine;
    
    // Command queue system
    private Queue<PetCommand> commandQueue;
    private bool isExecutingCommand = false;
    private Coroutine commandExecutionCoroutine;
    
    // Movement readiness tracking
    private bool isReadyForMovement = false;
    
    [Header("LLM Integration")]
    [SerializeField] private float walkSpeed = 0.5f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float maxRandomMovementDistance = 5f;
    
    // Public properties for LLMCommandExecutor access
    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public float MaxRandomMovementDistance => maxRandomMovementDistance;
    
    // LLM Command integration
    private LLMCommandExecutor llmCommandExecutor;
    private float defaultAgentSpeed;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Camera Following")]
    [SerializeField] private float followDistance = 0.5f;
    [SerializeField] private float followUpdateInterval = 0.1f;
    
    // Following state management
    private bool isFollowingCamera = false;
    private Coroutine followCoroutine;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animationController = GetComponent<PetAnimationController>();
        stateMachine = GetComponent<PetActionStateMachine>();
        
        // Initialize command queue
        commandQueue = new Queue<PetCommand>();
        
        // Store default agent speed and find LLM command executor
        defaultAgentSpeed = agent.speed;
        llmCommandExecutor = FindObjectOfType<LLMCommandExecutor>();
        
        // Subscribe to LLM command events
        if (llmCommandExecutor != null)
        {
            llmCommandExecutor.OnUserTakesControl += OnUserTakesControl;
            llmCommandExecutor.OnLLMTakesControl += OnLLMTakesControl;
        }
        else
        {
            Debug.LogWarning("PetMoveVR: LLMCommandExecutor not found!");
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
    }

    void Update()
    {
        // Update movement and turn animations through the animation controller
        if (animationController != null)
        {
            animationController.SetMoveSpeed(agent.velocity.magnitude);
            
            // Only apply movement turn velocity if not looking at user
            if (!animationController.IsLookingAtUser)
            {
                // Simple turn calculation for movement
                Vector3 desiredVelocity = agent.desiredVelocity.normalized;
                Vector3 currentForward = transform.forward;
                float turnVelocity = Vector3.Cross(currentForward, desiredVelocity).y;
                
                animationController.SetTurnVelocity(turnVelocity);
            }
        }
        else if (animator != null)
        {
            // Fallback to direct animator control if no animation controller
            animator.SetFloat("moveSpeed", agent.velocity.magnitude);
            
            // Note: Direct animator control doesn't have look-at priority system
            Vector3 desiredVelocity = agent.desiredVelocity.normalized;
            Vector3 currentForward = transform.forward;
            float turnVelocity = Vector3.Cross(currentForward, desiredVelocity).y;
            
            animator.SetFloat("turnVelocity", turnVelocity);
        }

        HandleInput();
        
        // Update movement readiness
        UpdateMovementReadiness();
        
        // Process command queue
        ProcessCommandQueue();
    }
    
    private void UpdateMovementReadiness()
    {
        if (stateMachine == null || animationController == null)
        {
            isReadyForMovement = false;
            return;
        }
        
        bool wasReady = isReadyForMovement;
        
        // Pet is ready for movement when:
        // 1. Animation controller is not executing sequential transitions
        // 2. State machine is not transitioning
        // 3. Not currently executing any commands (to avoid conflicts)
        // Note: We no longer require Idle state since movement commands auto-transition to Idle
        isReadyForMovement = !animationController.IsExecutingSequentialTransition() &&
                            !stateMachine.IsTransitioning &&
                            !isExecutingCommand;
        
    }

    private void HandleInput()
    {
        // Mouse click for movement
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 targetDestination = hit.point;
                QueueMovementCommand(targetDestination);
            }
        }

        // Keyboard shortcuts for testing different behaviors
        HandleBehaviorInput();
    }
    
    private void QueueMovementCommand(Vector3 destination)
    {
        if (stateMachine == null) return;
        
        // Stop following when user clicks to move
        StopFollowing();
        
        PetActionState currentState = stateMachine.CurrentState;
        
        
        // Queue transition to Idle first if not already in Idle
        if (currentState != PetActionState.Idle)
        {
            commandQueue.Enqueue(new StateTransitionCommand(PetActionState.Idle));
        }
        
        // Queue the movement command (movement now handled in Idle state via blend tree)
        commandQueue.Enqueue(new MovementCommand(destination));
    }
    
    private void ProcessCommandQueue()
    {
        // Start processing commands if we're not already executing and have commands queued
        if (!isExecutingCommand && commandQueue.Count > 0)
        {
            // Check if the next command is a MovementCommand and if we're ready for movement
            PetCommand nextCommand = commandQueue.Peek();
            if (nextCommand is MovementCommand && !isReadyForMovement)
            {
                // Don't execute movement commands until ready
                return;
            }
            
            if (commandExecutionCoroutine != null)
            {
                StopCoroutine(commandExecutionCoroutine);
            }
            commandExecutionCoroutine = StartCoroutine(ExecuteNextCommand());
        }
    }
    
    private IEnumerator ExecuteNextCommand()
    {
        isExecutingCommand = true;
        
        while (commandQueue.Count > 0)
        {
            PetCommand command = commandQueue.Dequeue();
            
            
            yield return command.Execute(this);
            
            // Dynamic delay based on animation controller's timing
            float dynamicDelay = GetDynamicCommandDelay();
            
            if (dynamicDelay == 0f && IsCurrentlyMoving())
            {
                // Special case: wait for movement to complete
                yield return StartCoroutine(WaitForMovementToComplete());
            }
            else
            {
                yield return new WaitForSeconds(dynamicDelay);
            }
        }
        
        isExecutingCommand = false;
        commandExecutionCoroutine = null;
        
    }
    
    private float GetDynamicCommandDelay()
    {
        if (animationController == null) return 0.1f; // Fallback
        
        // Check if we're currently moving (even in Idle state)
        if (agent != null && IsCurrentlyMoving())
        {
            // If moving, wait for movement to complete instead of using fixed delay
            return 0f; // Return 0 to indicate we need special handling
        }
        
        // Special case: If we're in Idle state and not moving, use minimal delay
        if (stateMachine != null && stateMachine.CurrentState == PetActionState.Idle)
        {
            return 0.1f; // Minimal delay for Idle state
        }
        
        // Use the animation controller's dynamic timing for other states
        float delay = animationController.GetDynamicAnimationWaitTime();
        
        // Ensure minimum delay for system stability
        return Mathf.Max(delay, 0.1f);
    }
    
    private bool IsCurrentlyMoving()
    {
        if (agent == null) return false;
        
        // Check multiple conditions to determine if movement is in progress
        bool hasPath = agent.hasPath;
        bool pathPending = agent.pathPending;
        bool isMoving = agent.velocity.magnitude > 0.1f;
        bool farFromDestination = agent.remainingDistance > 0.5f;
        
        return hasPath || pathPending || isMoving || farFromDestination;
    }
    
    private IEnumerator WaitForMovementToComplete()
    {
        if (agent == null) yield break;
        
        float timeout = 30f; // Maximum wait time
        float elapsed = 0f;
        
        while (elapsed < timeout && IsCurrentlyMoving())
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        // Extra wait to ensure movement is fully settled
        yield return new WaitForSeconds(0.3f);
        
        if (showDebugLogs && elapsed >= timeout)
            Debug.LogWarning("[LLM] Movement completion timeout in command queue");
    }
    
    // Public methods for command execution (called by command objects)
    public IEnumerator ExecuteStateTransition(PetActionState targetState)
    {
        if (stateMachine == null) yield break;
        
        
        // Reset movement readiness during any state transition
        isReadyForMovement = false;
        
        bool transitionStarted = stateMachine.RequestStateChange(targetState);
        
        if (!transitionStarted)
        {
            yield break;
        }
        
        // Wait for transition to complete
        while ((animationController != null && animationController.IsExecutingSequentialTransition()) ||
               stateMachine.IsTransitioning)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Additional wait for animation to settle based on target state
        if (animationController != null)
        {
            float settleTime = animationController.GetAnimationDurationForState(targetState);
            yield return new WaitForSeconds(settleTime);
        }
        else
        {
            // Fallback for Idle state
            if (targetState == PetActionState.Idle)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        
    }
    
    public IEnumerator ExecuteMovementCommand(Vector3 destination)
    {
        if (stateMachine == null || agent == null) yield break;
        
        // Automatically transition to Idle state if not already there for movement
        if (stateMachine.CurrentState != PetActionState.Idle)
        {
            if (showDebugLogs)
                Debug.Log($"[LLM] Auto-transitioning from {stateMachine.CurrentState} to Idle for movement");
            
            // Transition to Idle state first
            yield return ExecuteStateTransition(PetActionState.Idle);
        }
        
        // Set NavMesh destination - movement handled via blend tree in Idle state
        agent.SetDestination(destination);
        
        // Wait for movement to complete - stay in Idle state, animation blend tree handles walk
        while (!agent.pathPending && agent.remainingDistance > 0.1f)
        {
            yield return new WaitForSeconds(0.2f);
        }
        
        // Wait a bit more for agent to fully stop
        yield return new WaitForSeconds(0.5f);
        
    }

    private void HandleBehaviorInput()
    {
        if (stateMachine == null) return;

        // Number keys for quick behavior testing - now using queue system
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            QueueStateCommandInternal(PetActionState.Idle);
        
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            QueueStateCommandInternal(PetActionState.Sit);
        
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            QueueStateCommandInternal(PetActionState.Lying);
        
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            QueueStateCommandInternal(PetActionState.Flat);
        
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            QueueStateCommandInternal(PetActionState.Sleep);
        
        // C key for camera following mode
        if (Keyboard.current.cKey.wasPressedThisFrame)
            QueueFollowCameraCommand();
        
        // Digit 6 key removed - Walk state no longer exists (handled via blend tree in Idle)
    }
    
    private void QueueStateCommandInternal(PetActionState targetState)
    {
        // Stop following when user triggers other behaviors
        StopFollowing();
        
        commandQueue.Enqueue(new StateTransitionCommand(targetState));
    }
    
    // Public methods for LLMCommandExecutor
    public void QueueStateCommand(PetActionState targetState)
    {
        commandQueue.Enqueue(new StateTransitionCommand(targetState));
    }
    
    public void QueueDurationBasedStateCommand(PetActionState targetState, float duration)
    {
        commandQueue.Enqueue(new DurationBasedStateCommand(targetState, duration));
    }
    
    public void QueueLLMMovementCommand(Vector3 destination, float speed)
    {
        string speedType = speed > walkSpeed ? "Run" : "Walk";
        commandQueue.Enqueue(new LLMMovementCommand(destination, speedType, speed));
        if (showDebugLogs)
            Debug.Log($"[LLM] Queued: Movement command to {destination} at speed {speed} ({speedType}) [walkSpeed={walkSpeed}, runSpeed={runSpeed}]");
    }
    
    private void QueueFollowCameraCommand()
    {
        // Force user control mode when C key is pressed
        if (llmCommandExecutor != null)
        {
            llmCommandExecutor.ForceUserControl();
        }
        
        // Stop any existing following
        StopFollowing();
        
        // Ensure pet is in idle state for movement
        if (stateMachine != null && stateMachine.CurrentState != PetActionState.Idle)
        {
            commandQueue.Enqueue(new StateTransitionCommand(PetActionState.Idle));
        }
        
        // Queue the follow command
        commandQueue.Enqueue(new FollowCameraCommand(mainCamera, followUpdateInterval));
        
        if (showDebugLogs)
            Debug.Log("[Follow] Camera following mode activated");
    }
    
    // ============= LLM INTEGRATION METHODS =============
    
    public void ProcessLLMCommands(List<LLMCommand> commands)
    {
        if (llmCommandExecutor != null && !llmCommandExecutor.IsLLMControlled)
        {
            if (showDebugLogs)
                Debug.Log("[LLM] Ignoring LLM commands - User is in control");
            return;
        }
        
        foreach (LLMCommand cmd in commands)
        {
            PetCommand petCommand = ConvertToPetCommand(cmd);
            if (petCommand != null)
            {
                commandQueue.Enqueue(petCommand);
                
                if (showDebugLogs)
                    Debug.Log($"[LLM] Command queued: {cmd.action}");
            }
        }
    }
    
    private PetCommand ConvertToPetCommand(LLMCommand cmd)
    {
        string actionLower = cmd.action.ToLower();
        
        if (cmd.IsMovementCommand)
        {
            // Handle movement commands
            if (cmd.target.ToLower() == "floor")
            {
                float speed = cmd.speed.ToLower() == "run" ? runSpeed : walkSpeed;
                return new RandomMovementCommand(cmd.speed, speed, maxRandomMovementDistance);
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[LLM] Unknown movement target: {cmd.target}");
                return null;
            }
        }
        else
        {
            // Handle behavior state commands
            PetActionState? targetState = actionLower switch
            {
                "idle" => PetActionState.Idle,
                "sit" => PetActionState.Sit,
                "lying" => PetActionState.Lying,
                "flat" => PetActionState.Flat,
                "sleep" => PetActionState.Sleep,
                _ => null
            };
            
            if (targetState.HasValue)
            {
                if (cmd.HasDuration)
                {
                    return new DurationBasedStateCommand(targetState.Value, cmd.duration);
                }
                else
                {
                    return new StateTransitionCommand(targetState.Value);
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[LLM] Unknown action: {cmd.action}");
                return null;
            }
        }
    }
    
    // Focus management event handlers
    private void OnUserTakesControl()
    {
        // Clear the command queue when user takes control
        commandQueue.Clear();
        
        // Stop any currently executing commands
        if (commandExecutionCoroutine != null)
        {
            StopCoroutine(commandExecutionCoroutine);
            commandExecutionCoroutine = null;
        }
        
        isExecutingCommand = false;
        
        // Reset agent speed to default
        if (agent != null)
        {
            agent.speed = defaultAgentSpeed;
        }
        
        // Trigger look-at behavior when user takes control
        if (animationController != null && mainCamera != null)
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            animationController.LookAtUser(cameraPosition);
        }
        
        if (showDebugLogs)
            Debug.Log("[LLM] User took control - cleared command queue and triggered look-at");
    }
    
    private void OnLLMTakesControl()
    {
        // Stop following when LLM takes control
        StopFollowing();
        
        if (showDebugLogs)
            Debug.Log("[LLM] LLM took control - ready for commands");
    }
    
    // New execution methods for LLM commands
    public IEnumerator ExecuteLLMMovementCommand(Vector3 destination, float speed)
    {
        if (agent == null) yield break;
        
        if (showDebugLogs)
            Debug.Log($"[LLM] Executing movement command with speed {speed}");
        
        // Set agent speed
        float originalSpeed = agent.speed;
        agent.speed = speed;
        
        if (showDebugLogs)
            Debug.Log($"[LLM] NavMeshAgent speed: {originalSpeed} → {speed} (actual: {agent.speed})");
        
        yield return ExecuteMovementCommand(destination);
    }
    
    public IEnumerator ExecuteRandomMovementCommand(float speed, float maxDistance)
    {
        Vector3? randomPosition = GetRandomNavMeshPosition(maxDistance);
        
        if (randomPosition.HasValue)
        {
            if (showDebugLogs)
                Debug.Log($"[LLM] Executing random movement to {randomPosition.Value} with speed {speed}");
            
            yield return ExecuteLLMMovementCommand(randomPosition.Value, speed);
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("[LLM] Could not find valid random position for movement");
        }
    }
    
    public IEnumerator ExecuteDurationBasedStateTransition(PetActionState targetState, float duration)
    {
        if (showDebugLogs)
            Debug.Log($"[LLM] Executing duration-based state transition to {targetState} for {duration}s");
        
        // Execute the state transition
        yield return ExecuteStateTransition(targetState);
        
        // Wait for the specified duration (if > 0)
        if (duration > 0)
        {
            if (showDebugLogs)
                Debug.Log($"[LLM] Waiting {duration}s in {targetState} state");
            
            yield return new WaitForSeconds(duration);
        }
        
        if (showDebugLogs)
            Debug.Log($"[LLM] Duration-based state command completed");
    }
    
    public IEnumerator ExecuteFollowCameraCommand(Camera targetCamera, float updateInterval)
    {
        if (targetCamera == null || agent == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[Follow] Cannot execute follow command - missing camera or agent");
            yield break;
        }
        
        isFollowingCamera = true;
        Vector3 lastTargetPosition = Vector3.zero;
        float pathUpdateThreshold = 0.8f; // Update path when camera moves this much
        NavMeshPath currentPath = new NavMeshPath();
        
        if (showDebugLogs)
            Debug.Log($"[Follow] Starting camera following with follow distance {followDistance}m, update interval {updateInterval}s");
        
        while (isFollowingCamera && targetCamera != null)
        {
            Vector3 cameraPosition = targetCamera.transform.position;
            Vector3 groundCameraPosition = GetGroundPositionFromCamera(cameraPosition, targetCamera);
            float distanceToCamera = Vector3.Distance(transform.position, groundCameraPosition);
            float cameraMoveDistance = Vector3.Distance(groundCameraPosition, lastTargetPosition);
            
            // Check if we need to update the path
            bool shouldUpdatePath = false;
            
            if (!agent.hasPath)
            {
                // No current path - create initial path
                shouldUpdatePath = true;
            }
            else if (cameraMoveDistance > pathUpdateThreshold)
            {
                // Camera moved significantly - update path smoothly
                shouldUpdatePath = true;
            }
            else if (agent.remainingDistance < 1f && distanceToCamera > followDistance * 1.5f)
            {
                // Close to completing path but still far from target
                shouldUpdatePath = true;
            }
            
            if (shouldUpdatePath && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                // Calculate new path to ground-projected camera position
                if (NavMesh.CalculatePath(transform.position, groundCameraPosition, NavMesh.AllAreas, currentPath))
                {
                    if (currentPath.status == NavMeshPathStatus.PathComplete)
                    {
                        // Smoothly update the path instead of setting destination
                        agent.SetPath(currentPath);
                        lastTargetPosition = groundCameraPosition;
                        
                        if (showDebugLogs)
                            Debug.Log($"[Follow] Updated path to {groundCameraPosition}, distance: {distanceToCamera:F2}m");
                    }
                    else
                    {
                        // Fallback to SetDestination if path calculation fails
                        agent.SetDestination(groundCameraPosition);
                        lastTargetPosition = groundCameraPosition;
                        
                        if (showDebugLogs)
                            Debug.Log($"[Follow] Fallback destination to {groundCameraPosition}, path status: {currentPath.status}");
                    }
                }
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
        
        if (showDebugLogs)
            Debug.Log("[Follow] Camera following stopped");
    }
    
    private void StopFollowing()
    {
        if (isFollowingCamera)
        {
            isFollowingCamera = false;
            
            if (followCoroutine != null)
            {
                StopCoroutine(followCoroutine);
                followCoroutine = null;
            }
            
            // Stop agent movement
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            
            if (showDebugLogs)
                Debug.Log("[Follow] Following stopped");
        }
    }
    
    private Vector3 GetGroundPositionFromCamera(Vector3 cameraPosition, Camera camera)
    {
        // Get camera's forward direction projected to ground plane
        Vector3 cameraForward = camera.transform.forward;
        Vector3 groundForward = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
        
        // Position pet in front of camera at specified follow distance
        Vector3 baseGroundPosition = new Vector3(cameraPosition.x, transform.position.y, cameraPosition.z);
        Vector3 targetPosition = baseGroundPosition + groundForward * followDistance;
        
        // Try to find valid NavMesh position for the forward offset position
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            if (showDebugLogs)
                Debug.Log($"[Follow] Forward offset position found: {cameraPosition} -> {hit.position} (distance: {followDistance}m)");
            return hit.position;
        }
        
        // Fallback: try the direct ground position if forward position is blocked
        if (NavMesh.SamplePosition(baseGroundPosition, out hit, 2f, NavMesh.AllAreas))
        {
            if (showDebugLogs)
                Debug.Log($"[Follow] Using fallback ground position: {cameraPosition} -> {hit.position}");
            return hit.position;
        }
        
        if (showDebugLogs)
            Debug.Log($"[Follow] Using direct target position: {cameraPosition} -> {targetPosition}");
        
        return targetPosition; // Use target anyway - NavMeshAgent will find closest valid point
    }
    
    public Vector3? GetRandomNavMeshPosition(float maxDistance)
    {
        Vector3 currentPosition = transform.position;
        
        for (int attempts = 0; attempts < 10; attempts++)
        {
            // Generate random direction and distance
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * maxDistance;
            Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 targetPosition = currentPosition + randomDirection;
            
            // Check if the position is valid on NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out UnityEngine.AI.NavMeshHit hit, 1f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        
        return null; // Could not find valid position
    }
    
    void OnDestroy()
    {
        // Unsubscribe from focus events
        if (llmCommandExecutor != null)
        {
            llmCommandExecutor.OnUserTakesControl -= OnUserTakesControl;
            llmCommandExecutor.OnLLMTakesControl -= OnLLMTakesControl;
        }
    }
}