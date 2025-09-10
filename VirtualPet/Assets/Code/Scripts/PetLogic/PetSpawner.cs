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
    
    // Simple two-pinch detection
    private bool hasSpawnedThisSession = false;

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

        if (Touch.activeTouches.Count > 0 && spawnedPet == null && !hasSpawnedThisSession)
        {
            // Count active pinches
            int activePinchCount = 0;
            SpatialPointerState firstPinchData = new SpatialPointerState();
            
            foreach (Touch touch in Touch.activeTouches)
            {
                SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);
                if (touchData.Kind == SpatialPointerKind.IndirectPinch || touchData.Kind == SpatialPointerKind.DirectPinch)
                {
                    if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
                    {
                        activePinchCount++;
                        if (activePinchCount == 1)
                        {
                            firstPinchData = touchData; // Use first pinch for raycast
                        }
                    }
                }
            }
            
            Debug.Log($"[PetSpawner] Active pinches: {activePinchCount}");
            
            // Spawn pet if exactly 2 pinches are active
            if (activePinchCount == 2)
            {
                Debug.Log("[PetSpawner] Two pinches detected! Spawning pet...");
                SpawnPetWithData(firstPinchData);
                hasSpawnedThisSession = true;
            }
        }
    }
    
    /// <summary>
    /// Spawns the pet using pinch data from one of the detected pinches
    /// </summary>
    private void SpawnPetWithData(SpatialPointerState pinchData)
    {
        Ray ray = new Ray(pinchData.startInteractionRayOrigin, pinchData.startInteractionRayDirection);
        
        Debug.Log("[PetSpawner] Spawning with ray from: " + ray.origin + " direction: " + ray.direction);

        // Visualize the ray in-game
        if (linePrefab != null)
        {
            float lineLength = 10f;
            GameObject line = Instantiate(linePrefab);
            line.transform.position = ray.origin + (ray.direction * lineLength / 2f);
            line.transform.rotation = Quaternion.LookRotation(ray.direction);
            line.transform.localScale = new Vector3(0.005f, 0.005f, lineLength);
            Destroy(line, 2.0f); // Keep line visible longer for two-pinch spawn
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("[PetSpawner] Two-pinch spawn raycast hit: " + hit.collider.name);
            spawnedPet = Instantiate(petPrefab, hit.point, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[PetSpawner] Two pinches detected but no surface found for spawning");
        }
    }
    
    /// <summary>
    /// Reset spawning state to allow spawning again
    /// </summary>
    public void ResetSpawnSession()
    {
        hasSpawnedThisSession = false;
        if (spawnedPet != null)
        {
            Destroy(spawnedPet);
            spawnedPet = null;
        }
    }
}
