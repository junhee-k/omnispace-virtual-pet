using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PetBehavior;

[RequireComponent(typeof(NavMeshAgent))]
public class PetMoveVR : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    [SerializeField] private Camera mainCamera;
    
    private PetAnimationController animationController;
    private PetActionStateMachine stateMachine;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animationController = GetComponent<PetAnimationController>();
        stateMachine = GetComponent<PetActionStateMachine>();
        
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
    }
    
    private void CheckMovementCompletion()
    {
        if (stateMachine == null || agent == null) return;
        
        // Only check if currently in Walk state
        if (stateMachine.CurrentState == PetActionState.Walk)
        {
            // Check if NavMeshAgent has reached its destination and stopped moving
            bool hasReachedDestination = !agent.pathPending && agent.remainingDistance < 0.1f;
            bool isNotMoving = agent.velocity.magnitude < 0.1f;
            
            if (hasReachedDestination && isNotMoving)
            {
                // Movement completed, return to Idle state
                stateMachine.RequestStateChange(PetActionState.Idle);
            }
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
                // Store the movement destination for later use
                Vector3 targetDestination = hit.point;
                
                if (stateMachine != null)
                {
                    // Check current state
                    PetActionState currentState = stateMachine.CurrentState;
                    
                    // Check if pet needs to transition to Idle first
                    if (currentState != PetActionState.Idle && currentState != PetActionState.Walk)
                    {
                        // Pet is in another state (Sit, Lying, Flat, Sleep, etc.) - transition to Idle first
                        Debug.Log($"Movement input detected. Pet is in {currentState} state. Starting transition to Idle before movement.");
                        StartCoroutine(TransitionToIdleAndMove(targetDestination));
                    }
                    else
                    {
                        // Pet is already in Idle or Walk - move immediately
                        Debug.Log($"Movement input detected. Pet is in {currentState} state. Moving immediately.");
                        ExecuteMovement(targetDestination);
                    }
                }
                else
                {
                    // No state machine, just move
                    agent.SetDestination(targetDestination);
                }
            }
        }

        // Keyboard shortcuts for testing different behaviors
        HandleBehaviorInput();
    }
    
    private System.Collections.IEnumerator TransitionToIdleAndMove(Vector3 destination)
    {
        // Use state machine to handle the transition (which will use animation controller's sequential logic)
        if (stateMachine != null)
        {
            Debug.Log("Requesting transition to Idle state for movement...");
            bool transitionStarted = stateMachine.RequestStateChange(PetActionState.Idle);
            
            if (!transitionStarted)
            {
                Debug.LogWarning("Failed to start transition to Idle state for movement");
                yield break;
            }
            
            Debug.Log("Transition started. Waiting for completion...");
            
            // Wait for the complete transition to finish
            // This includes both state machine transitions and animation controller sequential transitions
            while ((animationController != null && animationController.IsExecutingSequentialTransition()) ||
                   stateMachine.IsTransitioning)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("All transitions completed.");
            
            // Ensure we actually reached Idle state
            PetActionState finalState = stateMachine.CurrentState;
            if (finalState != PetActionState.Idle)
            {
                Debug.LogWarning($"Expected to reach Idle state but ended up in {finalState}");
                yield break;
            }
        }
        else
        {
            Debug.LogWarning("No state machine available for state transition");
            yield break;
        }
        
        // Wait a brief moment to ensure everything is fully settled
        yield return new WaitForSeconds(0.3f);
        
        // Now execute the movement
        Debug.Log("Starting movement execution...");
        ExecuteMovement(destination);
    }
    
    private void ExecuteMovement(Vector3 destination)
    {
        // Set the NavMesh destination
        agent.SetDestination(destination);
        
        // Transition to Walk state if not already walking
        if (stateMachine != null && stateMachine.CurrentState != PetActionState.Walk)
        {
            stateMachine.RequestStateChange(PetActionState.Walk);
        }
    }

    private void HandleBehaviorInput()
    {
        if (stateMachine == null) return;

        // Number keys for quick behavior testing
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            stateMachine.RequestStateChange(PetActionState.Idle);
        
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            stateMachine.RequestStateChange(PetActionState.Sit);
        
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            stateMachine.RequestStateChange(PetActionState.Lying);
        
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            stateMachine.RequestStateChange(PetActionState.Flat);
        
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            stateMachine.RequestStateChange(PetActionState.Sleep);
        
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            stateMachine.RequestStateChange(PetActionState.Walk);
    }
}