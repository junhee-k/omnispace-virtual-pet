using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Touch Related
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;
using Unity.PolySpatial.InputDevices;

/// <summary>
/// This script manages finding a suitable plane, showing a placement indicator,
/// and spawning a virtual pet when a UI button is pressed.
/// It ensures only one pet can be spawned at a time.
/// Designed to work with input systems like Apple Vision Pro's gestures or standard UI buttons.
/// </summary>
[RequireComponent(typeof(ARPlaneManager))]
public class PetSpawner : MonoBehaviour
{
    [Tooltip("The prefab of the pet to be spawned.")]
    public GameObject petPrefab;

    [Tooltip("The visual indicator for where the pet will be placed.")]
    public GameObject placementIndicator;

    [Tooltip("A LineRenderer prefab to visualize the raycast in-game.")]
    public GameObject linePrefab;

    // This will hold the pet instance once it's spawned.
    private GameObject spawnedPet;

    // Reference to the AR Plane Manager component.
    private ARPlaneManager arPlaneManager;

    void onEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void Awake()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();
        
        if (placementIndicator != null)
        {
            placementIndicator.SetActive(false);
        }
    }

    void Update()
    {
        // Log the number of planes being tracked to help with debugging.
        Debug.Log($"Planes detected: {arPlaneManager.trackables.count}");

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
                        Debug.Log("Pinch began at " + ray.origin + " " + ray.direction);

                        // Visualize the ray in-game by instantiating and scaling a cube prefab
                        if (linePrefab != null)
                        {
                            float lineLength = 10f;
                            GameObject line = Instantiate(linePrefab);
                            line.transform.position = ray.origin + (ray.direction * lineLength / 2f);
                            line.transform.rotation = Quaternion.LookRotation(ray.direction);
                            line.transform.localScale = new Vector3(0.005f, 0.005f, lineLength);
                            Destroy(line, 1.0f); // Destroy the line after 1 second
                        }

                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit))
                        {
                            Debug.Log("Physics Raycast hit a collider: " + hit.collider.name);
                            if(spawnedPet == null)
                            {
                                spawnedPet = Instantiate(petPrefab, hit.point, Quaternion.identity);
                            }
                        }
                    }
                }
            }
        }
    }
}
