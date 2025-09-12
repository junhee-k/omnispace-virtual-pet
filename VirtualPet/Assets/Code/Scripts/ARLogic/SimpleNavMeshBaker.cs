using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class SimpleNavMeshBaker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showNavMeshInGameView = true;
    
    [Header("References")]
    [SerializeField] private NavMeshSurface navMeshSurface;
    
    private void Awake()
    {
        // Auto-find NavMeshSurface if not assigned
        if (navMeshSurface == null)
        {
            navMeshSurface = FindObjectOfType<NavMeshSurface>();
        }
    }
    
    /// <summary>
    /// Simple method to bake NavMesh asynchronously. Can be called from UI button.
    /// </summary>
    public void BakeNavMesh()
    {
        if (showDebugLogs)
        {
            Debug.Log("[SimpleNavMeshBaker] BakeNavMesh() called.");
        }
        StartCoroutine(BakeNavMeshCoroutine());
    }
    
    /// <summary>
    /// Coroutine for async NavMesh baking without blocking the UI
    /// </summary>
    private IEnumerator BakeNavMeshCoroutine()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("[SimpleNavMeshBaker] NavMeshSurface not found! Cannot bake NavMesh.");
            yield break;
        }
        
        // Determine if this is first-time build or update
        bool isFirstTimeBuild = navMeshSurface.navMeshData == null;
        
        if (showDebugLogs)
        {
            Debug.Log($"[SimpleNavMeshBaker] Starting async NavMesh {(isFirstTimeBuild ? "build" : "update")}...");
        }
        
        // Handle async baking operation
        if (isFirstTimeBuild)
        {
            // First time - build from scratch (synchronous but spread across frames)
            yield return StartCoroutine(BuildNavMeshAsync());
        }
        else
        {
            // Update existing NavMeshData (async)
            var asyncOp = navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
            
            // Wait for completion without blocking
            while (!asyncOp.isDone)
            {
                yield return null;
            }
        }
        
        // Create debug visualization if enabled
        if (showNavMeshInGameView)
        {
            CreateDebugVisualization();
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[SimpleNavMeshBaker] Async NavMesh baking completed!");
        }
    }
    
    /// <summary>
    /// Helper coroutine for first-time NavMesh building
    /// </summary>
    private IEnumerator BuildNavMeshAsync()
    {
        // Use synchronous build but yield every frame to prevent blocking
        yield return null; // Yield before starting
        
        navMeshSurface.BuildNavMesh();
        
        yield return null; // Yield after completion
    }
    
    /// <summary>
    /// Create simple debug visualization for NavMesh in Game View
    /// </summary>
    private void CreateDebugVisualization()
    {
        // Remove existing debug visualization
        GameObject existingDebug = GameObject.Find("NavMesh_Debug_Global");
        if (existingDebug != null)
        {
            DestroyImmediate(existingDebug);
        }
        
        // Get global NavMesh triangulation
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices.Length == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[SimpleNavMeshBaker] No NavMesh triangulation data found for visualization.");
            }
            return;
        }
        
        // Create debug GameObject
        GameObject debugVis = new("NavMesh_Debug_Global");
        
        // Add mesh components
        MeshFilter meshFilter = debugVis.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = debugVis.AddComponent<MeshRenderer>();
        
        // Create mesh from triangulation
        Mesh debugMesh = new()
        {
            vertices = triangulation.vertices,
            triangles = triangulation.indices
        };
        debugMesh.RecalculateNormals();
        meshFilter.mesh = debugMesh;
        
        // Create transparent cyan material
        Material debugMaterial = new(Shader.Find("Standard"))
        {
            color = new Color(0, 1, 1, 0.3f) // Transparent cyan
        };
        debugMaterial.SetFloat("_Mode", 3); // Transparent mode
        debugMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        debugMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        debugMaterial.SetInt("_ZWrite", 0);
        debugMaterial.DisableKeyword("_ALPHATEST_ON");
        debugMaterial.EnableKeyword("_ALPHABLEND_ON");
        debugMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        debugMaterial.renderQueue = 3000;
        
        meshRenderer.material = debugMaterial;
        
        if (showDebugLogs)
        {
            Debug.Log($"[SimpleNavMeshBaker] Created global NavMesh debug visualization with {triangulation.vertices.Length} vertices.");
        }
    }
    
    /// <summary>
    /// Clear NavMesh data. Useful for testing.
    /// </summary>
    public void ClearNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("[SimpleNavMeshBaker] NavMeshSurface not found! Cannot clear NavMesh.");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[SimpleNavMeshBaker] Clearing NavMesh...");
        }
        
        // Clear NavMesh data
        navMeshSurface.RemoveData();
        
        // Remove debug visualization
        GameObject debugVis = GameObject.Find("NavMesh_Debug_Global");
        if (debugVis != null)
        {
            DestroyImmediate(debugVis);
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[SimpleNavMeshBaker] NavMesh clearing completed!");
        }
    }
    
    /// <summary>
    /// Get current status for UI display
    /// </summary>
    public string GetStatusInfo()
    {
        if (navMeshSurface == null)
        {
            return "NavMeshSurface not found";
        }
        
        bool isNavMeshBaked = navMeshSurface.navMeshData != null;
        
        return $"NavMesh Status: {(isNavMeshBaked ? "Baked" : "Not Baked")}";
    }
}