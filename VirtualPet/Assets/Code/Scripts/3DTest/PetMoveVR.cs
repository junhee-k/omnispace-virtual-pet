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
    
    // Behavior system integration
    private PetBehaviorManager behaviorManager;
    private PetAnimationController animationController;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        behaviorManager = GetComponent<PetBehaviorManager>();
        animationController = GetComponent<PetAnimationController>();
        
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

        HandleInput();
    }

    private void HandleInput()
    {
        // Mouse click for movement
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.SetDestination(hit.point);
                
                // Notify behavior manager that user initiated movement
                if (behaviorManager != null)
                {
                    behaviorManager.SetUserControlled(true);
                }
            }
        }

        // Keyboard shortcuts for testing different behaviors
        HandleBehaviorInput();
    }

    private void HandleBehaviorInput()
    {
        if (behaviorManager == null) return;

        // Number keys for quick behavior testing
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            behaviorManager.RequestBehavior(PetActionState.Idle, "user_input");
        
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            behaviorManager.RequestBehavior(PetActionState.Sit, "user_input");
        
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            behaviorManager.RequestBehavior(PetActionState.Lying, "user_input");
        
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            behaviorManager.RequestBehavior(PetActionState.Flat, "user_input");
        
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            behaviorManager.RequestBehavior(PetActionState.Sleep, "user_input");
        
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            behaviorManager.RequestBehavior(PetActionState.Walk, "user_input");
    }
}