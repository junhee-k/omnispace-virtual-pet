using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class ProactiveJumpPathfinder : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    [SerializeField] private Camera mainCamera;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
        if (animator != null)
        {
            animator.SetFloat("moveSpeed", agent.velocity.magnitude);
            
            // Simple turn calculation
            Vector3 desiredVelocity = agent.desiredVelocity.normalized;
            Vector3 currentForward = transform.forward;
            float turnVelocity = Vector3.Cross(currentForward, desiredVelocity).y;
            
            animator.SetFloat("turnVelocity", turnVelocity);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.SetDestination(hit.point);
            }
        }
    }
}