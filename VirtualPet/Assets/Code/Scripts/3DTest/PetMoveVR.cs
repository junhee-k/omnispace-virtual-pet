using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PetBehavior;

// Command system for queued actions
public abstract class PetCommand
{
    public abstract IEnumerator Execute(PetMoveVR controller);
}

public class StateTransitionCommand : PetCommand
{
    public PetActionState targetState;
    
    public StateTransitionCommand(PetActionState state)
    {
        targetState = state;
    }
    
    public override IEnumerator Execute(PetMoveVR controller)
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
    
    public override IEnumerator Execute(PetMoveVR controller)
    {
        yield return controller.ExecuteMovementCommand(destination);
    }
}

[RequireComponent(typeof(NavMeshAgent))]
public class PetMoveVR : MonoBehaviour
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
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animationController = GetComponent<PetAnimationController>();
        stateMachine = GetComponent<PetActionStateMachine>();
        
        // Initialize command queue
        commandQueue = new Queue<PetCommand>();
        
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
            
            // Simple turn calculation
            Vector3 desiredVelocity = agent.desiredVelocity.normalized;
            Vector3 currentForward = transform.forward;
            float turnVelocity = Vector3.Cross(currentForward, desiredVelocity).y;
            
            animationController.SetTurnVelocity(turnVelocity);
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

        // Check if movement completed and transition back to Idle
        CheckMovementCompletion();
        HandleInput();
        
        // Update movement readiness
        UpdateMovementReadiness();
        
        // Process command queue
        ProcessCommandQueue();
    }
    
    private void CheckMovementCompletion()
    {
        // Movement completion is now handled entirely through animation blend tree
        // No state transitions needed since walk is part of idle state
    }
    
    private void UpdateMovementReadiness()
    {
        if (stateMachine == null || animationController == null)
        {
            isReadyForMovement = false;
            return;
        }
        
        bool wasReady = isReadyForMovement;
        
        // Simplified: Pet is ready for movement when:
        // 1. State machine is in Idle state
        // 2. Animation controller is not executing sequential transitions
        // 3. Not currently executing any commands (to avoid conflicts)
        isReadyForMovement = (stateMachine.CurrentState == PetActionState.Idle) &&
                            !animationController.IsExecutingSequentialTransition() &&
                            !stateMachine.IsTransitioning &&
                            !isExecutingCommand;
        
        // Log readiness changes for debugging
        if (wasReady != isReadyForMovement && showDebugLogs)
        {
            Debug.Log($"Movement readiness changed: {wasReady} → {isReadyForMovement} " +
                     $"(State: {stateMachine.CurrentState}, " +
                     $"SeqTrans: {animationController.IsExecutingSequentialTransition()}, " +
                     $"StateTrans: {stateMachine.IsTransitioning}, " +
                     $"ExecCmd: {isExecutingCommand})");
        }
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
        
        PetActionState currentState = stateMachine.CurrentState;
        
        if (showDebugLogs)
            Debug.Log($"Queuing movement command. Current state: {currentState}");
        
        // Queue transition to Idle first if not already in Idle
        if (currentState != PetActionState.Idle)
        {
            commandQueue.Enqueue(new StateTransitionCommand(PetActionState.Idle));
            if (showDebugLogs)
                Debug.Log("Queued: Transition to Idle");
        }
        
        // Queue the movement command (movement now handled in Idle state via blend tree)
        commandQueue.Enqueue(new MovementCommand(destination));
        if (showDebugLogs)
            Debug.Log("Queued: Movement command");
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
                if (showDebugLogs)
                    Debug.Log("Movement command queued but pet not ready for movement yet");
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
            
            if (showDebugLogs)
                Debug.Log($"Executing command: {command.GetType().Name}");
            
            yield return command.Execute(this);
            
            // Dynamic delay based on animation controller's timing
            float dynamicDelay = GetDynamicCommandDelay();
            if (showDebugLogs)
                Debug.Log($"Waiting {dynamicDelay:F2}s between commands for proper transitions");
            
            yield return new WaitForSeconds(dynamicDelay);
        }
        
        isExecutingCommand = false;
        commandExecutionCoroutine = null;
        
        if (showDebugLogs)
            Debug.Log("All commands in queue executed");
    }
    
    private float GetDynamicCommandDelay()
    {
        if (animationController == null) return 0.1f; // Fallback
        
        // Special case: If we're in Idle state, use minimal delay for immediate responsiveness
        if (stateMachine != null && stateMachine.CurrentState == PetActionState.Idle)
        {
            return 0.1f; // Minimal delay for Idle state
        }
        
        // Use the animation controller's dynamic timing for other states
        float delay = animationController.GetDynamicAnimationWaitTime();
        
        // Ensure minimum delay for system stability
        return Mathf.Max(delay, 0.1f);
    }
    
    // Public methods for command execution (called by command objects)
    public IEnumerator ExecuteStateTransition(PetActionState targetState)
    {
        if (stateMachine == null) yield break;
        
        if (showDebugLogs)
            Debug.Log($"Executing state transition to {targetState}");
        
        // Reset movement readiness during any state transition
        isReadyForMovement = false;
        
        bool transitionStarted = stateMachine.RequestStateChange(targetState);
        
        if (!transitionStarted)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Failed to start transition to {targetState}");
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
            if (showDebugLogs)
                Debug.Log($"Waiting {settleTime:F2}s for {targetState} animation to settle");
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
        
        // Verify we reached the target state
        if (stateMachine.CurrentState != targetState)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Expected to reach {targetState} but ended up in {stateMachine.CurrentState}");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"Successfully transitioned to {targetState}");
        }
    }
    
    public IEnumerator ExecuteMovementCommand(Vector3 destination)
    {
        if (stateMachine == null || agent == null) yield break;
        
        // Only allow movement from Idle state
        if (stateMachine.CurrentState != PetActionState.Idle)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Cannot execute movement from {stateMachine.CurrentState} state. Movement only allowed from Idle state.");
            yield break;
        }
        
        if (showDebugLogs)
            Debug.Log($"Executing movement to {destination}");
        
        // Set NavMesh destination - movement handled via blend tree in Idle state
        agent.SetDestination(destination);
        
        // Wait for movement to complete - stay in Idle state, animation blend tree handles walk
        while (!agent.pathPending && agent.remainingDistance > 0.1f)
        {
            yield return new WaitForSeconds(0.2f);
        }
        
        // Wait a bit more for agent to fully stop
        yield return new WaitForSeconds(0.5f);
        
        if (showDebugLogs)
            Debug.Log("Movement command completed");
    }

    private void HandleBehaviorInput()
    {
        if (stateMachine == null) return;

        // Number keys for quick behavior testing - now using queue system
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            QueueStateCommand(PetActionState.Idle);
        
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            QueueStateCommand(PetActionState.Sit);
        
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            QueueStateCommand(PetActionState.Lying);
        
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            QueueStateCommand(PetActionState.Flat);
        
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            QueueStateCommand(PetActionState.Sleep);
        
        // Digit 6 key removed - Walk state no longer exists (handled via blend tree in Idle)
    }
    
    private void QueueStateCommand(PetActionState targetState)
    {
        commandQueue.Enqueue(new StateTransitionCommand(targetState));
        if (showDebugLogs)
            Debug.Log($"Queued: State transition to {targetState}");
    }
}