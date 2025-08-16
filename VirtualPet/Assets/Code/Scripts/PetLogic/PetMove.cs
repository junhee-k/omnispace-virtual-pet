using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;
using Unity.PolySpatial.InputDevices;

public class PetMove : MonoBehaviour
{
    NavMeshAgent agent;
    private Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("moveSpeed", agent.velocity.magnitude);
        if (Touch.activeTouches.Count > 0)
        {
            foreach (Touch touch in Touch.activeTouches)
            {
                SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);
                if (touchData.Kind == SpatialPointerKind.IndirectPinch || touchData.Kind == SpatialPointerKind.DirectPinch)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        Ray ray = new Ray(touchData.startInteractionRayOrigin, touchData.startInteractionRayDirection);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit))
                        {
                            agent.SetDestination(hit.point);
                        }
                    }
                }
            }
        }
    }
}
