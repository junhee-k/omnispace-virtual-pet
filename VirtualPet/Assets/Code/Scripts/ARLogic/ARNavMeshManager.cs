using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlaneManager))]
public class ARNavMeshManager : MonoBehaviour
{
    private ARPlaneManager arPlaneManager;

    private void Awake()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
        arPlaneManager.planesChanged += OnPlanesChanged;
    }

    private void OnDisable()
    {
        arPlaneManager.planesChanged -= OnPlanesChanged;
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs eventArgs)
    {
        if (eventArgs.added.Count > 0 || eventArgs.updated.Count > 0)
        {
            UpdateAllNavMeshes();
        }
    }

    private void UpdateAllNavMeshes()
    {
        foreach (ARPlane plane in arPlaneManager.trackables)
        {
            if (plane.gameObject.activeInHierarchy)
            {
                NavMeshSurface navMeshSurface = plane.GetComponent<NavMeshSurface>();
                if (navMeshSurface != null)
                {
                    navMeshSurface.BuildNavMesh();
                }
            }
        }
    }
}