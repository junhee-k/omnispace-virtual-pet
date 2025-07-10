using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;

public class ColorChange : MonoBehaviour
{
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
                if (touch.phase == TouchPhase.Began)
                {
                    SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);
                    if (touchData.targetObject != null)
                    {
                        ChangeObjectColor(touchData.targetObject);
                        break;
                    }
                }
            }
        }
    }

    void ChangeObjectColor(GameObject targetObject)
    {
        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(Random.value, Random.value, Random.value);
        }
    }
}
