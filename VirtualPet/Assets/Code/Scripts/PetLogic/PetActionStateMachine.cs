using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PetBehavior
{
    public enum PetActionState
    {
        Idle,
        Sit,
        Lying,
        Flat,
        Sleep
    }

    [Serializable]
    public class StateTransition
    {
        public PetActionState fromState;
        public PetActionState toState;
        public float transitionDuration;

        public StateTransition(PetActionState from, PetActionState to, float duration = 0.5f)
        {
            fromState = from;
            toState = to;
            transitionDuration = duration;
        }
    }

    public class PetActionStateMachine : MonoBehaviour
    {
        [Header("State Configuration")]
        [SerializeField] private PetActionState currentState = PetActionState.Idle;
        [SerializeField] private bool isTransitioning = false;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Graph representation using adjacency list
        private Dictionary<PetActionState, List<PetActionState>> stateGraph;
        private Queue<PetActionState> transitionQueue;
        
        // Events
        public System.Action<PetActionState, PetActionState> OnStateChanged;
        public System.Action<PetActionState> OnTransitionStarted;
        public System.Action OnTransitionCompleted;
        public System.Action<List<PetActionState>> OnTransitionPathCalculated;

        public PetActionState CurrentState => currentState;
        public bool IsTransitioning => isTransitioning;

        void Awake()
        {
            InitializeStateGraph();
            transitionQueue = new Queue<PetActionState>();
        }

        void Start()
        {
            // State machine initialized
        }

        private void InitializeStateGraph()
        {
            stateGraph = new Dictionary<PetActionState, List<PetActionState>>();
            
            // Initialize all states with empty lists
            foreach (PetActionState state in System.Enum.GetValues(typeof(PetActionState)))
            {
                stateGraph[state] = new List<PetActionState>();
            }
            
            // Define connections based on your requirements
            // Idle is connected to sit, flat, sleep
            stateGraph[PetActionState.Idle].AddRange(new[] { 
                PetActionState.Sit, PetActionState.Flat, PetActionState.Sleep 
            });
            
            // Sit is connected to idle, lying
            stateGraph[PetActionState.Sit].AddRange(new[] { 
                PetActionState.Idle, PetActionState.Lying 
            });
            
            // Lying is connected to sit, idle, sleep, flat
            stateGraph[PetActionState.Lying].AddRange(new[] { 
                PetActionState.Sit, PetActionState.Idle, PetActionState.Sleep, PetActionState.Flat 
            });
            
            // Flat is connected to lying
            stateGraph[PetActionState.Flat].AddRange(new[] { 
                PetActionState.Lying 
            });
            
            // Sleep is connected to idle, flat
            stateGraph[PetActionState.Sleep].AddRange(new[] { 
                PetActionState.Idle, PetActionState.Flat 
            });
        }

        public bool CanTransitionTo(PetActionState targetState)
        {
            if (isTransitioning) return false;
            return FindPath(currentState, targetState) != null;
        }

        public bool RequestStateChange(PetActionState targetState)
        {
            if (currentState == targetState)
            {
                return true;
            }

            if (isTransitioning)
            {
                return false;
            }

            List<PetActionState> path = FindPath(currentState, targetState);
            if (path == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"No valid path from {currentState} to {targetState}");
                return false;
            }


            OnTransitionPathCalculated?.Invoke(path);
            StartCoroutine(ExecuteTransitionPath(path));
            return true;
        }

        private List<PetActionState> FindPath(PetActionState start, PetActionState target)
        {
            if (start == target) return new List<PetActionState> { start };
            
            // BFS to find shortest path
            Queue<PetActionState> queue = new Queue<PetActionState>();
            Dictionary<PetActionState, PetActionState> parent = new Dictionary<PetActionState, PetActionState>();
            HashSet<PetActionState> visited = new HashSet<PetActionState>();
            
            queue.Enqueue(start);
            visited.Add(start);
            parent[start] = start; // Mark start as its own parent
            
            while (queue.Count > 0)
            {
                PetActionState current = queue.Dequeue();
                
                if (current == target)
                {
                    // Reconstruct path
                    List<PetActionState> path = new List<PetActionState>();
                    PetActionState step = target;
                    
                    while (step != start)
                    {
                        path.Add(step);
                        step = parent[step];
                    }
                    path.Add(start);
                    path.Reverse();
                    
                    return path;
                }
                
                foreach (PetActionState neighbor in stateGraph[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return null; // No path found
        }

        private IEnumerator ExecuteTransitionPath(List<PetActionState> path)
        {
            isTransitioning = true;
            
            for (int i = 1; i < path.Count; i++) // Start from 1 since path[0] is current state
            {
                PetActionState nextState = path[i];
                
                OnTransitionStarted?.Invoke(nextState);
                
                // Wait for animation or transition time
                yield return new WaitForSeconds(0.5f); // Default transition time
                
                PetActionState previousState = currentState;
                currentState = nextState;
                OnStateChanged?.Invoke(previousState, currentState);
            }
            
            isTransitioning = false;
            OnTransitionCompleted?.Invoke();
        }

        public void ForceSetState(PetActionState state)
        {
            if (isTransitioning)
            {
                StopAllCoroutines();
                isTransitioning = false;
            }
            
            PetActionState previousState = currentState;
            currentState = state;
            OnStateChanged?.Invoke(previousState, currentState);
        }

        public List<PetActionState> GetValidTransitions()
        {
            return new List<PetActionState>(stateGraph[currentState]);
        }

        public List<PetActionState> GetAllStates()
        {
            return new List<PetActionState>(stateGraph.Keys);
        }

        // Debug method to visualize the state graph
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogStateGraph()
        {
        }

        void OnValidate()
        {
            if (Application.isPlaying && stateGraph != null)
            {
                // Reinitialize graph if edited in inspector during play
                InitializeStateGraph();
            }
        }
    }
}