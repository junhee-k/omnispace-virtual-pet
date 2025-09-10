using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;
using Unity.PolySpatial.InputDevices;
using PetBehavior;

[RequireComponent(typeof(NavMeshAgent))]
public class PetMove : MonoBehaviour, IPetController
{
    private NavMeshAgent agent;
    private Animator animator;
    private Camera mainCamera;
    
    private PetAnimationController animationController;
    private PetActionStateMachine stateMachine;
    
    // Command queue system - reusing the same command classes from PetMoveVR
    private Queue<PetCommand> commandQueue;
    private bool isExecutingCommand = false;
    private Coroutine commandExecutionCoroutine;
    
    // Movement readiness tracking
    private bool isReadyForMovement = false;
    
    [Header("XR Follow Mode Configuration")]
    [SerializeField] private float followDistance = 0.5f;
    [SerializeField] private float followUpdateInterval = 0.1f;
    
    // Following state management
    private bool isFollowingCamera = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // LLM Command integration for focus management
    private LLMCommandExecutor llmCommandExecutor;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animationController = GetComponent<PetAnimationController>();
        stateMachine = GetComponent<PetActionStateMachine>();
        
        

        // Initialize command queue
        commandQueue = new Queue<PetCommand>();
        
        // Find LLM command executor for focus management
        llmCommandExecutor = FindObjectOfType<LLMCommandExecutor>();
        
        // Subscribe to LLM command events if available
        if (llmCommandExecutor != null)
        {
            llmCommandExecutor.OnUserTakesControl += OnUserTakesControl;
            llmCommandExecutor.OnLLMTakesControl += OnLLMTakesControl;
        }
        
        // Find main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        if (showDebugLogs)
            Debug.Log("[XR] PetMove initialized for XR environment");
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
            
            Vector3 desiredVelocity = agent.desiredVelocity.normalized;
            Vector3 currentForward = transform.forward;
            float turnVelocity = Vector3.Cross(currentForward, desiredVelocity).y;
            
            animator.SetFloat("turnVelocity", turnVelocity);
        }

        HandleXRInput();
        
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
        
        // Pet is ready for movement when not executing transitions or commands
        isReadyForMovement = !animationController.IsExecutingSequentialTransition() &&
                            !stateMachine.IsTransitioning &&
                            !isExecutingCommand;
    }

    private void HandleXRInput()
    {
        // XR input handling removed - pet only follows via voice commands
        // This prevents conflicts with voice recording system that uses pinch gestures
    }
    
    private void QueueFollowCameraCommand()
    {
        // Force user control mode when touch is detected
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
        
        // Queue the follow command - reusing FollowCameraCommand from PetMoveVR
        commandQueue.Enqueue(new FollowCameraCommand(mainCamera, followUpdateInterval));
        
        if (showDebugLogs)
            Debug.Log("[XR] Camera following mode activated via primary touch");
    }
    
    private void ProcessCommandQueue()
    {
        // Start processing commands if we're not already executing and have commands queued
        if (!isExecutingCommand && commandQueue.Count > 0)
        {
            // Check if ready for next command
            PetCommand nextCommand = commandQueue.Peek();
            if (nextCommand is FollowCameraCommand && !isReadyForMovement)
            {
                // Don't execute follow commands until ready
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
            yield return new WaitForSeconds(dynamicDelay);
        }
        
        isExecutingCommand = false;
        commandExecutionCoroutine = null;
    }
    
    private float GetDynamicCommandDelay()
    {
        if (animationController == null) return 0.1f; // Fallback
        
        // Special case: If we're in Idle state, use minimal delay
        if (stateMachine != null && stateMachine.CurrentState == PetActionState.Idle)
        {
            return 0.1f; // Minimal delay for Idle state
        }
        
        // Use the animation controller's dynamic timing for other states
        float delay = animationController.GetDynamicAnimationWaitTime();
        
        // Ensure minimum delay for system stability
        return Mathf.Max(delay, 0.1f);
    }
    
    // Public execution methods for command objects - adapted from PetMoveVR
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
    }
    
    public IEnumerator ExecuteFollowCameraCommand(Camera targetCamera, float updateInterval)
    {
        if (targetCamera == null || agent == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[XR] Cannot execute follow command - missing camera or agent");
            yield break;
        }
        
        isFollowingCamera = true;
        Vector3 lastTargetPosition = Vector3.zero;
        float pathUpdateThreshold = 0.8f; // Update path when camera moves this much
        NavMeshPath currentPath = new NavMeshPath();
        
        if (showDebugLogs)
            Debug.Log($"[XR] Starting camera following with follow distance {followDistance}m, update interval {updateInterval}s");
        
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
                            Debug.Log($"[XR] Updated path to {groundCameraPosition}, distance: {distanceToCamera:F2}m");
                    }
                    else
                    {
                        // Fallback to SetDestination if path calculation fails
                        agent.SetDestination(groundCameraPosition);
                        lastTargetPosition = groundCameraPosition;
                        
                        if (showDebugLogs)
                            Debug.Log($"[XR] Fallback destination to {groundCameraPosition}, path status: {currentPath.status}");
                    }
                }
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
        
        if (showDebugLogs)
            Debug.Log("[XR] Camera following stopped");
    }
    
    private void StopFollowing()
    {
        if (isFollowingCamera)
        {
            isFollowingCamera = false;
            
            // Stop agent movement
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            
            if (showDebugLogs)
                Debug.Log("[XR] Following stopped");
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
                Debug.Log($"[XR] Forward offset position found: {cameraPosition} -> {hit.position} (distance: {followDistance}m)");
            return hit.position;
        }
        
        // Fallback: try the direct ground position if forward position is blocked
        if (NavMesh.SamplePosition(baseGroundPosition, out hit, 2f, NavMesh.AllAreas))
        {
            if (showDebugLogs)
                Debug.Log($"[XR] Using fallback ground position: {cameraPosition} -> {hit.position}");
            return hit.position;
        }
        
        if (showDebugLogs)
            Debug.Log($"[XR] Using direct target position: {cameraPosition} -> {targetPosition}");
        
        return targetPosition; // Use target anyway - NavMeshAgent will find closest valid point
    }
    
    // Additional interface methods required by IPetController (not used in XR but needed for interface)
    public IEnumerator ExecuteMovementCommand(Vector3 destination)
    {
        if (agent == null) yield break;
        
        // Simple movement implementation for XR (if needed in future)
        agent.SetDestination(destination);
        
        // Wait for movement to complete
        while (!agent.pathPending && agent.remainingDistance > 0.1f)
        {
            yield return new WaitForSeconds(0.2f);
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    public IEnumerator ExecuteLLMMovementCommand(Vector3 destination, float speed)
    {
        // Not implemented for XR version - would be similar to ExecuteMovementCommand with speed control
        yield return ExecuteMovementCommand(destination);
    }
    
    public IEnumerator ExecuteRandomMovementCommand(float speed, float maxDistance)
    {
        // Not implemented for XR version - would find random position and move there
        if (showDebugLogs)
            Debug.Log("[XR] Random movement not implemented");
        yield break;
    }
    
    public IEnumerator ExecuteDurationBasedStateTransition(PetActionState targetState, float duration)
    {
        // Execute state transition and wait for duration
        yield return ExecuteStateTransition(targetState);
        
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
        }
    }
    
    // Voice command processing
    public void ProcessVoiceCommand(string command)
    {
        if (showDebugLogs)
            Debug.Log($"[Voice] Received voice command: '{command}'");

        // Force user control when voice command is received
        if (llmCommandExecutor != null)
        {
            llmCommandExecutor.ForceUserControl();
        }

        // Map voice command to appropriate action
        switch (command.ToLower())
        {
            case "sit":
                QueueStateCommand(PetActionState.Sit);
                break;
            case "lying":
                QueueStateCommand(PetActionState.Lying);
                break;
            case "sleep":
                QueueStateCommand(PetActionState.Sleep);
                break;
            case "idle":
                QueueStateCommand(PetActionState.Idle);
                break;
            case "flat":
                QueueStateCommand(PetActionState.Flat);
                break;
            case "follow":
                QueueFollowCameraCommand();
                break;
            case "stop":
                StopFollowing();
                commandQueue.Clear(); // Clear any pending commands
                if (commandExecutionCoroutine != null)
                {
                    StopCoroutine(commandExecutionCoroutine);
                    commandExecutionCoroutine = null;
                    isExecutingCommand = false;
                }
                break;
            default:
                if (showDebugLogs)
                    Debug.LogWarning($"[Voice] Unknown command: '{command}'");
                break;
        }
    }

    private void QueueStateCommand(PetActionState targetState)
    {
        // Stop any current following when transitioning states
        if (isFollowingCamera)
        {
            StopFollowing();
        }

        // Queue the state transition command
        commandQueue.Enqueue(new StateTransitionCommand(targetState));
        
        if (showDebugLogs)
            Debug.Log($"[Voice] Queued state transition to: {targetState}");
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
        
        // Trigger look-at behavior when user takes control
        if (animationController != null && mainCamera != null)
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            animationController.LookAtUser(cameraPosition);
        }
        
        if (showDebugLogs)
            Debug.Log("[XR] User took control - cleared command queue and triggered look-at");
    }
    
    private void OnLLMTakesControl()
    {
        // Stop following when LLM takes control
        StopFollowing();
        
        if (showDebugLogs)
            Debug.Log("[XR] LLM took control - ready for commands");
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
