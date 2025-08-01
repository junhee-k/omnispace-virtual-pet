using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PetBehavior;

[System.Serializable]
public class BehaviorRule
{
    [Tooltip("Condition that triggers this behavior")]
    public string conditionName;
    [Tooltip("Target state to transition to")]
    public PetActionState targetState;
    [Tooltip("Priority of this behavior (higher = more important)")]
    public int priority = 1;
    [Tooltip("Cooldown time before this rule can trigger again")]
    public float cooldownTime = 5.0f;
    [Tooltip("Is this behavior currently enabled?")]
    public bool isEnabled = true;
    
    [HideInInspector]
    public float lastTriggeredTime = -1f;
}

[RequireComponent(typeof(PetActionStateMachine), typeof(PetAnimationController))]
public class PetBehaviorManager : MonoBehaviour
{
    [Header("Behavior Configuration")]
    [SerializeField] private BehaviorRule[] behaviorRules;
    [SerializeField] private float idleTimeout = 10.0f;
    [SerializeField] private float walkTimeout = 5.0f;
    
    [Header("Movement Integration")]
    [SerializeField] private bool integrateWithMovement = true;
    [SerializeField] private float movementThreshold = 0.1f;
    
    [Header("Auto Behaviors")]
    [SerializeField] private bool enableAutoBehaviors = true;
    [SerializeField] private float behaviorCheckInterval = 2.0f;
    [SerializeField] private float randomBehaviorChance = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showDebugGUI = true;
    
    // Component references
    private PetActionStateMachine stateMachine;
    private PetAnimationController animationController;
    private NavMeshAgent navMeshAgent;
    
    // Behavior tracking
    private Dictionary<string, BehaviorRule> behaviorRuleMap;
    private Queue<PetActionState> behaviorQueue;
    private Coroutine autoBehaviorCoroutine;
    private float lastStateChangeTime;
    private float lastMovementTime;
    
    // State tracking
    private bool isUserControlled = false;
    private PetActionState previousState;
    
    // Events
    public System.Action<PetActionState> OnBehaviorRequested;
    public System.Action<PetActionState> OnBehaviorExecuted;
    public System.Action<BehaviorRule> OnBehaviorRuleTriggered;

    public PetActionState CurrentState => stateMachine?.CurrentState ?? PetActionState.Idle;
    public bool IsTransitioning => stateMachine?.IsTransitioning ?? false;
    public bool IsUserControlled => isUserControlled;

    void Awake()
    {
        stateMachine = GetComponent<PetActionStateMachine>();
        animationController = GetComponent<PetAnimationController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        
        InitializeBehaviorSystem();
    }

    void Start()
    {
        SubscribeToEvents();
        
        if (enableAutoBehaviors)
        {
            StartAutoBehaviors();
        }
        
        lastStateChangeTime = Time.time;
        previousState = CurrentState;
        
        if (showDebugLogs)
            Debug.Log($"Pet Behavior Manager initialized. Current state: {CurrentState}");
    }

    void Update()
    {
        UpdateMovementTracking();
        CheckTimeoutBehaviors();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
        StopAutoBehaviors();
    }

    private void InitializeBehaviorSystem()
    {
        behaviorQueue = new Queue<PetActionState>();
        behaviorRuleMap = new Dictionary<string, BehaviorRule>();
        
        if (behaviorRules != null)
        {
            foreach (var rule in behaviorRules)
            {
                if (!string.IsNullOrEmpty(rule.conditionName))
                {
                    behaviorRuleMap[rule.conditionName] = rule;
                }
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += HandleStateChanged;
            stateMachine.OnTransitionCompleted += HandleTransitionCompleted;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= HandleStateChanged;
            stateMachine.OnTransitionCompleted -= HandleTransitionCompleted;
        }
    }

    private void HandleStateChanged(PetActionState fromState, PetActionState toState)
    {
        lastStateChangeTime = Time.time;
        previousState = fromState;
        
        OnBehaviorExecuted?.Invoke(toState);
        
        if (showDebugLogs)
            Debug.Log($"Behavior Manager: State changed to {toState}");
    }

    private void HandleTransitionCompleted()
    {
        // Process next behavior in queue if available
        ProcessBehaviorQueue();
    }

    private void UpdateMovementTracking()
    {
        if (integrateWithMovement && navMeshAgent != null)
        {
            bool isMoving = navMeshAgent.velocity.magnitude > movementThreshold;
            
            if (isMoving)
            {
                lastMovementTime = Time.time;
                
                // Auto-transition to walk state if moving but not in walk state
                if (CurrentState != PetActionState.Walk && !IsTransitioning)
                {
                    RequestBehavior(PetActionState.Walk, "movement_detected");
                }
            }
            else if (CurrentState == PetActionState.Walk && !IsTransitioning)
            {
                // Transition back to idle when movement stops
                RequestBehavior(PetActionState.Idle, "movement_stopped");
            }
        }
    }

    private void CheckTimeoutBehaviors()
    {
        if (IsTransitioning) return;
        
        float timeSinceStateChange = Time.time - lastStateChangeTime;
        
        // Check for idle timeout
        if (CurrentState == PetActionState.Idle && timeSinceStateChange > idleTimeout)
        {
            TriggerRandomBehavior("idle_timeout");
        }
        
        // Check for walk timeout
        if (CurrentState == PetActionState.Walk && timeSinceStateChange > walkTimeout)
        {
            RequestBehavior(PetActionState.Idle, "walk_timeout");
        }
    }

    // Public API methods
    public bool RequestBehavior(PetActionState targetState, string reason = "manual")
    {
        OnBehaviorRequested?.Invoke(targetState);
        
        if (showDebugLogs)
            Debug.Log($"Behavior requested: {targetState} (reason: {reason})");
        
        if (stateMachine.RequestStateChange(targetState))
        {
            isUserControlled = reason == "manual" || reason == "user_input";
            return true;
        }
        
        return false;
    }

    public bool QueueBehavior(PetActionState targetState)
    {
        if (!behaviorQueue.Contains(targetState))
        {
            behaviorQueue.Enqueue(targetState);
            
            if (showDebugLogs)
                Debug.Log($"Behavior queued: {targetState}");
            
            return true;
        }
        
        return false;
    }

    public void ClearBehaviorQueue()
    {
        behaviorQueue.Clear();
        
        if (showDebugLogs)
            Debug.Log("Behavior queue cleared");
    }

    public bool TriggerBehaviorRule(string ruleName)
    {
        if (behaviorRuleMap.TryGetValue(ruleName, out BehaviorRule rule))
        {
            if (!rule.isEnabled) return false;
            
            // Check cooldown
            if (rule.lastTriggeredTime >= 0 && Time.time - rule.lastTriggeredTime < rule.cooldownTime)
            {
                return false;
            }
            
            rule.lastTriggeredTime = Time.time;
            OnBehaviorRuleTriggered?.Invoke(rule);
            
            return RequestBehavior(rule.targetState, ruleName);
        }
        
        return false;
    }

    public List<PetActionState> GetValidTransitions()
    {
        return stateMachine?.GetValidTransitions() ?? new List<PetActionState>();
    }

    public bool CanTransitionTo(PetActionState targetState)
    {
        return stateMachine?.CanTransitionTo(targetState) ?? false;
    }

    private void ProcessBehaviorQueue()
    {
        if (behaviorQueue.Count > 0 && !IsTransitioning)
        {
            PetActionState nextBehavior = behaviorQueue.Dequeue();
            RequestBehavior(nextBehavior, "queued");
        }
    }

    private void TriggerRandomBehavior(string reason)
    {
        var validTransitions = GetValidTransitions();
        if (validTransitions.Count > 0)
        {
            int randomIndex = Random.Range(0, validTransitions.Count);
            PetActionState randomState = validTransitions[randomIndex];
            RequestBehavior(randomState, reason);
        }
    }

    private void StartAutoBehaviors()
    {
        StopAutoBehaviors();
        autoBehaviorCoroutine = StartCoroutine(AutoBehaviorCoroutine());
    }

    private void StopAutoBehaviors()
    {
        if (autoBehaviorCoroutine != null)
        {
            StopCoroutine(autoBehaviorCoroutine);
            autoBehaviorCoroutine = null;
        }
    }

    private IEnumerator AutoBehaviorCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(behaviorCheckInterval);
            
            if (!isUserControlled && !IsTransitioning && Random.value < randomBehaviorChance)
            {
                TriggerRandomBehavior("auto_behavior");
            }
        }
    }

    // Force methods for external control
    public void ForceState(PetActionState state)
    {
        stateMachine?.ForceSetState(state);
        isUserControlled = true;
        
        if (showDebugLogs)
            Debug.Log($"Force set state: {state}");
    }

    public void SetUserControlled(bool controlled)
    {
        isUserControlled = controlled;
        
        if (showDebugLogs)
            Debug.Log($"User controlled: {controlled}");
    }

    public void EnableBehaviorRule(string ruleName, bool enabled)
    {
        if (behaviorRuleMap.TryGetValue(ruleName, out BehaviorRule rule))
        {
            rule.isEnabled = enabled;
        }
    }

    // Debug GUI
    void OnGUI()
    {
        if (!showDebugGUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"Pet Behavior Manager", GUI.skin.label);
        GUILayout.Label($"Current State: {CurrentState}");
        GUILayout.Label($"Is Transitioning: {IsTransitioning}");
        GUILayout.Label($"User Controlled: {IsUserControlled}");
        GUILayout.Label($"Queue Count: {behaviorQueue.Count}");
        
        GUILayout.Space(10);
        GUILayout.Label("Quick Actions:");
        
        var validTransitions = GetValidTransitions();
        foreach (var state in validTransitions)
        {
            if (GUILayout.Button($"Go to {state}"))
            {
                RequestBehavior(state, "debug_gui");
            }
        }
        
        GUILayout.Space(10);
        if (GUILayout.Button("Clear Queue"))
        {
            ClearBehaviorQueue();
        }
        
        if (GUILayout.Button(enableAutoBehaviors ? "Disable Auto" : "Enable Auto"))
        {
            enableAutoBehaviors = !enableAutoBehaviors;
            if (enableAutoBehaviors)
                StartAutoBehaviors();
            else
                StopAutoBehaviors();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}