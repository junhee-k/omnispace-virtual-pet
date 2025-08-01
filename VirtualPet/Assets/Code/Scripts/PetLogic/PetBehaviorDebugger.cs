using UnityEngine;
using System.Collections.Generic;
using PetBehavior;

public class PetBehaviorDebugger : MonoBehaviour
{
    [Header("Debug Configuration")]
    [SerializeField] private bool enableOnScreenDebug = true;
    [SerializeField] private bool logStateChanges = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    
    [Header("Visual Debug")]
    [SerializeField] private bool showStateGraph = true;
    [SerializeField] private bool showTransitionPath = true;
    [SerializeField] private float pathVisualizationTime = 2.0f;
    
    private PetActionStateMachine stateMachine;
    private PetBehaviorManager behaviorManager;
    private PetAnimationController animationController;
    
    // Debug visualization
    private List<PetActionState> currentTransitionPath;
    private float pathVisualizationStartTime;
    private bool isDebugVisible = true;
    
    // GUI style
    private GUIStyle debugStyle;
    
    void Awake()
    {
        stateMachine = GetComponent<PetActionStateMachine>();
        behaviorManager = GetComponent<PetBehaviorManager>();
        animationController = GetComponent<PetAnimationController>();
    }
    
    void Start()
    {
        SubscribeToEvents();
        InitializeGUIStyle();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isDebugVisible = !isDebugVisible;
        }
        
        // Clear transition path visualization after time
        if (currentTransitionPath != null && 
            Time.time - pathVisualizationStartTime > pathVisualizationTime)
        {
            currentTransitionPath = null;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += OnStateChanged;
            stateMachine.OnTransitionPathCalculated += OnTransitionPathCalculated;
        }
        
        if (behaviorManager != null)
        {
            behaviorManager.OnBehaviorRequested += OnBehaviorRequested;
            behaviorManager.OnBehaviorExecuted += OnBehaviorExecuted;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= OnStateChanged;
            stateMachine.OnTransitionPathCalculated -= OnTransitionPathCalculated;
        }
        
        if (behaviorManager != null)
        {
            behaviorManager.OnBehaviorRequested -= OnBehaviorRequested;
            behaviorManager.OnBehaviorExecuted -= OnBehaviorExecuted;
        }
    }
    
    private void InitializeGUIStyle()
    {
        debugStyle = new GUIStyle();
        debugStyle.fontSize = 12;
        debugStyle.normal.textColor = Color.white;
    }
    
    private void OnStateChanged(PetActionState fromState, PetActionState toState)
    {
        if (logStateChanges)
        {
            Debug.Log($"[Pet Behavior Debug] State changed: {fromState} → {toState}", this);
        }
    }
    
    private void OnTransitionPathCalculated(List<PetActionState> path)
    {
        currentTransitionPath = new List<PetActionState>(path);
        pathVisualizationStartTime = Time.time;
        
        if (logStateChanges)
        {
            string pathString = string.Join(" → ", path);
            Debug.Log($"[Pet Behavior Debug] Transition path: {pathString}", this);
        }
    }
    
    private void OnBehaviorRequested(PetActionState targetState)
    {
        if (logStateChanges)
        {
            Debug.Log($"[Pet Behavior Debug] Behavior requested: {targetState}", this);
        }
    }
    
    private void OnBehaviorExecuted(PetActionState state)
    {
        if (logStateChanges)
        {
            Debug.Log($"[Pet Behavior Debug] Behavior executed: {state}", this);
        }
    }
    
    void OnGUI()
    {
        if (!enableOnScreenDebug || !isDebugVisible) return;
        
        DrawDebugPanel();
    }
    
    private void DrawDebugPanel()
    {
        float panelWidth = 350f;
        float panelHeight = 500f;
        Rect panelRect = new Rect(Screen.width - panelWidth - 10, 10, panelWidth, panelHeight);
        
        GUI.Box(panelRect, "");
        GUILayout.BeginArea(panelRect);
        GUILayout.BeginVertical();
        
        // Title
        GUILayout.Label("Pet Behavior Debugger", GUI.skin.label);
        GUILayout.Label($"Press {toggleKey} to toggle", debugStyle);
        GUILayout.Space(10);
        
        // Current state info
        if (stateMachine != null)
        {
            GUILayout.Label($"Current State: {stateMachine.CurrentState}", debugStyle);
            GUILayout.Label($"Is Transitioning: {stateMachine.IsTransitioning}", debugStyle);
        }
        
        if (behaviorManager != null)
        {
            GUILayout.Label($"User Controlled: {behaviorManager.IsUserControlled}", debugStyle);
        }
        
        GUILayout.Space(10);
        
        // Transition path visualization
        if (showTransitionPath && currentTransitionPath != null && currentTransitionPath.Count > 1)
        {
            GUILayout.Label("Current Transition Path:", debugStyle);
            string pathString = string.Join(" → ", currentTransitionPath);
            GUILayout.Label(pathString, debugStyle);
            
            float progress = (Time.time - pathVisualizationStartTime) / pathVisualizationTime;
            GUI.HorizontalSlider(GUILayoutUtility.GetRect(200, 20), progress, 0f, 1f);
            GUILayout.Space(10);
        }
        
        // State graph visualization
        if (showStateGraph && stateMachine != null)
        {
            GUILayout.Label("State Graph:", debugStyle);
            DrawStateGraph();
        }
        
        // Quick actions
        GUILayout.Space(10);
        GUILayout.Label("Quick Actions:", debugStyle);
        DrawQuickActionButtons();
        
        // Keyboard shortcuts
        GUILayout.Space(10);
        GUILayout.Label("Keyboard Shortcuts:", debugStyle);
        GUILayout.Label("1-6: Change to states", debugStyle);
        GUILayout.Label("Click: Move pet", debugStyle);
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private void DrawStateGraph()
    {
        if (stateMachine == null) return;
        
        var allStates = stateMachine.GetAllStates();
        var currentState = stateMachine.CurrentState;
        
        foreach (var state in allStates)
        {
            Color textColor = (state == currentState) ? Color.green : Color.white;
            
            var oldColor = GUI.color;
            GUI.color = textColor;
            
            var validTransitions = stateMachine.GetValidTransitions();
            string transitionsText = "";
            if (state == currentState && validTransitions.Count > 0)
            {
                transitionsText = $" → [{string.Join(", ", validTransitions)}]";
            }
            
            GUILayout.Label($"{state}{transitionsText}", debugStyle);
            GUI.color = oldColor;
        }
    }
    
    private void DrawQuickActionButtons()
    {
        if (behaviorManager == null) return;
        
        var validTransitions = behaviorManager.GetValidTransitions();
        
        foreach (var state in validTransitions)
        {
            if (GUILayout.Button($"→ {state}"))
            {
                behaviorManager.RequestBehavior(state, "debug_panel");
            }
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Clear Queue"))
        {
            behaviorManager.ClearBehaviorQueue();
        }
        
        if (GUILayout.Button("Log State Graph"))
        {
            stateMachine?.LogStateGraph();
        }
    }
    
    // Scene view debug drawing
    void OnDrawGizmos()
    {
        if (!showTransitionPath || currentTransitionPath == null || currentTransitionPath.Count <= 1)
            return;
        
        // Draw transition path as connected lines above the pet
        Vector3 basePosition = transform.position + Vector3.up * 2f;
        
        for (int i = 0; i < currentTransitionPath.Count - 1; i++)
        {
            Vector3 startPos = basePosition + Vector3.right * (i * 0.5f);
            Vector3 endPos = basePosition + Vector3.right * ((i + 1) * 0.5f);
            
            // Use different colors for different transition types
            Gizmos.color = GetStateColor(currentTransitionPath[i + 1]);
            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawSphere(endPos, 0.1f);
            
            // Draw state labels in scene view
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(endPos + Vector3.up * 0.2f, currentTransitionPath[i + 1].ToString());
            #endif
        }
    }
    
    private Color GetStateColor(PetActionState state)
    {
        switch (state)
        {
            case PetActionState.Idle: return Color.white;
            case PetActionState.Sit: return Color.blue;
            case PetActionState.Lying: return Color.cyan;
            case PetActionState.Flat: return Color.magenta;
            case PetActionState.Sleep: return Color.gray;
            case PetActionState.Walk: return Color.green;
            default: return Color.yellow;
        }
    }
    
    // Console commands for runtime debugging
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugForceState(string stateName)
    {
        if (System.Enum.TryParse<PetActionState>(stateName, true, out PetActionState state))
        {
            behaviorManager?.ForceState(state);
            Debug.Log($"[Debug] Forced state to: {state}");
        }
        else
        {
            Debug.LogError($"[Debug] Invalid state name: {stateName}");
        }
    }
    
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugRequestBehavior(string stateName)
    {
        if (System.Enum.TryParse<PetActionState>(stateName, true, out PetActionState state))
        {
            bool success = behaviorManager?.RequestBehavior(state, "debug_command") ?? false;
            Debug.Log($"[Debug] Requested behavior {state}: {(success ? "Success" : "Failed")}");
        }
        else
        {
            Debug.LogError($"[Debug] Invalid state name: {stateName}");
        }
    }
}