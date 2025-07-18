using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PetMoveVR : MonoBehaviour
{
    UnityEngine.AI.NavMeshAgent agent;
    [SerializeField] private Camera mainCamera;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("moveSpeed", agent.velocity.magnitude);
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log(hit.point.x + " " + hit.point.y + " " + hit.point.z);
                agent.SetDestination(hit.point);
            }
        }
    }
}
