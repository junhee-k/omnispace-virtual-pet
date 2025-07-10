using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;

public class MoveObjectOnInput : MonoBehaviour
{
    private GameObject selectedObject;
    private Vector3 lastPosition;

    void onEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            foreach (Touch touch in Touch.activeTouches)
            {
                SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);

                if (touchData.targetObject != null && touchData.Kind != SpatialPointerKind.Touch)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        selectedObject = touchData.targetObject;
                        lastPosition = touchData.interactionPosition;
                    }
                    else if (touch.phase == TouchPhase.Moved && selectedObject != null)
                    {
                        Vector3 currentPosition = touchData.interactionPosition;
                        Vector3 delta = currentPosition - lastPosition;
                        selectedObject.transform.position += delta;
                        lastPosition = currentPosition;
                    }
                }
            }
        }
        if (Touch.activeTouches.Count == 0)
        {
            selectedObject = null;
        }
    }
}
