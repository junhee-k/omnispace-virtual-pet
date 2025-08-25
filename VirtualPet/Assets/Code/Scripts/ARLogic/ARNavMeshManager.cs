using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARPlaneManager))]
public class ARNavMeshManager : MonoBehaviour
{
    [Header("NavMesh Settings")]
    [SerializeField] private float bakeInterval = 5f;
    [SerializeField] private int targetAgentTypeID = 0; // Default to Humanoid, set to your "New Agent" type ID
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private ARPlaneManager arPlaneManager;
    private float lastBakeTime;
    private bool hasPendingChanges = false;
    private Coroutine bakingCoroutine;

    private void Awake()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
        arPlaneManager.planesChanged += OnPlanesChanged;
        lastBakeTime = Time.time;
    }

    private void OnDisable()
    {
        arPlaneManager.planesChanged -= OnPlanesChanged;
        if (bakingCoroutine != null)
        {
            StopCoroutine(bakingCoroutine);
            bakingCoroutine = null;
        }
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs eventArgs)
    {
        bool hasFloorPlaneChanges = false;
        
        // Check added planes for floor planes
        foreach (ARPlane plane in eventArgs.added)
        {
            if (IsFloorPlane(plane))
            {
                hasFloorPlaneChanges = true;
                if (showDebugLogs)
                    Debug.Log($"[ARNavMeshManager] Floor plane added: {plane.name}");
                break;
            }
        }
        
        // Check updated planes for floor planes
        if (!hasFloorPlaneChanges)
        {
            foreach (ARPlane plane in eventArgs.updated)
            {
                if (IsFloorPlane(plane))
                {
                    hasFloorPlaneChanges = true;
                    if (showDebugLogs)
                        Debug.Log($"[ARNavMeshManager] Floor plane updated: {plane.name}");
                    break;
                }
            }
        }
        
        if (hasFloorPlaneChanges)
        {
            hasPendingChanges = true;
            
            // Start baking coroutine if not already running
            if (bakingCoroutine == null)
            {
                bakingCoroutine = StartCoroutine(DelayedBaking());
            }
        }
    }

    private bool IsFloorPlane(ARPlane plane)
    {
        // Check if plane is classified as floor or if it's horizontal (for platforms without classification)
        return plane.classification == PlaneClassification.Floor || 
               (plane.classification == PlaneClassification.None && 
                Vector3.Dot(plane.normal, Vector3.up) > 0.7f); // Horizontal threshold
    }
    
    private IEnumerator DelayedBaking()
    {
        while (true)
        {
            float timeSinceLastBake = Time.time - lastBakeTime;
            
            if (hasPendingChanges && timeSinceLastBake >= bakeInterval)
            {
                UpdateFloorNavMeshes();
                hasPendingChanges = false;
                lastBakeTime = Time.time;
                
                if (showDebugLogs)
                    Debug.Log($"[ARNavMeshManager] NavMesh baking completed at {Time.time}");
            }
            
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }
    
    private void UpdateFloorNavMeshes()
    {
        int bakedCount = 0;
        
        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            if (plane.gameObject.activeInHierarchy && IsFloorPlane(plane))
            {
                NavMeshSurface navMeshSurface = plane.GetComponent<NavMeshSurface>();
                if (navMeshSurface != null)
                {
                    // Configure the NavMesh for the target agent type
                    navMeshSurface.agentTypeID = targetAgentTypeID;
                    navMeshSurface.BuildNavMesh();
                    bakedCount++;
                    
                    if (showDebugLogs)
                        Debug.Log($"[ARNavMeshManager] Baked NavMesh for floor plane: {plane.name} with agentTypeID: {targetAgentTypeID}");
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[ARNavMeshManager] Total floor NavMeshes baked: {bakedCount}");
    }
}