using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PetBehavior;

[System.Serializable]
public class StateAnimationData
{
    public PetActionState state;
    [Tooltip("Animation trigger name in the Animator")]
    public string animationTrigger;
    [Tooltip("Duration to wait for this animation to complete")]
    public float animationDuration = 1.0f;
    [Tooltip("Should this animation loop while in this state?")]
    public bool isLooping = true;
}

[System.Serializable]
public class TransitionAnimationData
{
    public PetActionState fromState;
    public PetActionState toState;
    [Tooltip("Custom transition animation trigger (optional)")]
    public string transitionTrigger;
    [Tooltip("Duration of transition animation")]
    public float transitionDuration = 0.5f;
    [Tooltip("Should use custom transition animation instead of direct state change")]
    public bool useCustomTransition = false;
}

[RequireComponent(typeof(Animator))]
public class PetAnimationController : MonoBehaviour
{
    [Header("Animation Configuration")]
    [SerializeField] private StateAnimationData[] stateAnimations;
    [SerializeField] private TransitionAnimationData[] transitionAnimations;
    
    [Header("Animator Parameters")]
    [SerializeField] private string currentStateParameter = "currentState";
    [SerializeField] private string moveSpeedParameter = "moveSpeed";
    [SerializeField] private string turnVelocityParameter = "turnVelocity";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private Animator animator;
    private PetActionStateMachine stateMachine;
    private Dictionary<PetActionState, StateAnimationData> stateAnimationMap;
    private Dictionary<(PetActionState, PetActionState), TransitionAnimationData> transitionAnimationMap;
    
    // Sequential transition management
    private List<PetActionState> currentTransitionPath;
    private int currentPathIndex;
    private bool isExecutingSequentialTransition = false;
    private Coroutine currentAnimationCoroutine;
    
    public System.Action<PetActionState> OnAnimationStarted;
    public System.Action<PetActionState> OnAnimationCompleted;
    public System.Action<PetActionState, PetActionState> OnTransitionAnimationStarted;
    public System.Action<PetActionState, PetActionState> OnTransitionAnimationCompleted;

    void Awake()
    {
        animator = GetComponent<Animator>();
        stateMachine = GetComponent<PetActionStateMachine>();
        
        InitializeAnimationMaps();
    }

    void Start()
    {
        if (stateMachine != null)
        {
            // Subscribe to state machine events
            stateMachine.OnStateChanged += HandleStateChanged;
            stateMachine.OnTransitionPathCalculated += HandleTransitionPathCalculated;
            
            // Set initial animation state
            PlayStateAnimation(stateMachine.CurrentState);
        }
        else
        {
            Debug.LogError("PetAnimationController requires PetActionStateMachine component!");
        }
    }

    void OnDestroy()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= HandleStateChanged;
            stateMachine.OnTransitionPathCalculated -= HandleTransitionPathCalculated;
        }
        
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
    }

    private void InitializeAnimationMaps()
    {
        // Initialize state animation map
        stateAnimationMap = new Dictionary<PetActionState, StateAnimationData>();
        if (stateAnimations != null)
        {
            foreach (var stateAnim in stateAnimations)
            {
                if (!stateAnimationMap.ContainsKey(stateAnim.state))
                {
                    stateAnimationMap[stateAnim.state] = stateAnim;
                }
            }
        }
        
        // Initialize transition animation map
        transitionAnimationMap = new Dictionary<(PetActionState, PetActionState), TransitionAnimationData>();
        if (transitionAnimations != null)
        {
            foreach (var transAnim in transitionAnimations)
            {
                var key = (transAnim.fromState, transAnim.toState);
                if (!transitionAnimationMap.ContainsKey(key))
                {
                    transitionAnimationMap[key] = transAnim;
                }
            }
        }
        
        // Ensure all states have animation data (create defaults if missing)
        foreach (PetActionState state in System.Enum.GetValues(typeof(PetActionState)))
        {
            if (!stateAnimationMap.ContainsKey(state))
            {
                var defaultData = new StateAnimationData
                {
                    state = state,
                    animationTrigger = state.ToString().ToLower(),
                    animationDuration = 1.0f,
                    isLooping = state != PetActionState.Walk // Walk might not loop, others typically do
                };
                stateAnimationMap[state] = defaultData;
                
                if (showDebugLogs)
                    Debug.LogWarning($"Created default animation data for state: {state}");
            }
        }
    }

    private void HandleStateChanged(PetActionState fromState, PetActionState toState)
    {
        if (showDebugLogs)
            Debug.Log($"Animation Controller: State changed from {fromState} to {toState}");
        
        // Only play animation if we're not in the middle of a sequential transition
        // (sequential transitions handle their own animation playing)
        if (!isExecutingSequentialTransition)
        {
            PlayStateAnimation(toState);
        }
    }

    private void HandleTransitionPathCalculated(List<PetActionState> transitionPath)
    {
        if (showDebugLogs)
        {
            string pathString = string.Join(" → ", transitionPath);
            Debug.Log($"Animation Controller: Transition path received: {pathString}");
        }
        
        // Start sequential transition execution
        if (transitionPath.Count > 1) // Only if there's actually a path to follow
        {
            StartSequentialTransition(transitionPath);
        }
    }

    private void PlayStateAnimation(PetActionState state)
    {
        if (animator == null) return;
        
        if (stateAnimationMap.TryGetValue(state, out StateAnimationData animData))
        {
            // Set animator parameters
            if (!string.IsNullOrEmpty(currentStateParameter))
            {
                // You can use an integer parameter mapping states to numbers
                animator.SetInteger(currentStateParameter, (int)state);
            }
            
            // Trigger the specific state animation
            if (!string.IsNullOrEmpty(animData.animationTrigger))
            {
                // Only reset this specific trigger to prevent conflicts, but preserve others for multi-step transitions
                animator.ResetTrigger(animData.animationTrigger);
                animator.SetTrigger(animData.animationTrigger);
                OnAnimationStarted?.Invoke(state);
                
                if (showDebugLogs)
                    Debug.Log($"Playing animation trigger: {animData.animationTrigger} for state: {state}");
            }
        }
        else
        {
            Debug.LogWarning($"No animation data found for state: {state}");
        }
    }
    
    private void ResetAllStateTriggers()
    {
        // Reset all state triggers to prevent conflicts
        foreach (var stateData in stateAnimationMap.Values)
        {
            if (!string.IsNullOrEmpty(stateData.animationTrigger))
            {
                animator.ResetTrigger(stateData.animationTrigger);
            }
        }
        
        // Also reset any custom transition triggers that might be active
        foreach (var transitionData in transitionAnimationMap.Values)
        {
            if (transitionData.useCustomTransition && !string.IsNullOrEmpty(transitionData.transitionTrigger))
            {
                animator.ResetTrigger(transitionData.transitionTrigger);
            }
        }
    }

    private void StartSequentialTransition(List<PetActionState> path)
    {
        // Stop any current animation
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
        
        // Reset all triggers at the start of a new sequential transition to clean slate
        ResetAllStateTriggers();
        
        // Setup sequential transition
        currentTransitionPath = new List<PetActionState>(path);
        currentPathIndex = 0;
        isExecutingSequentialTransition = true;
        
        if (showDebugLogs)
        {
            string pathString = string.Join(" → ", path);
            Debug.Log($"Starting sequential transition: {pathString}");
        }
        
        // Start with the first transition step
        ExecuteNextTransitionStep();
    }
    
    private void ExecuteNextTransitionStep()
    {
        if (currentTransitionPath == null || currentPathIndex >= currentTransitionPath.Count - 1)
        {
            // Transition complete
            CompleteSequentialTransition();
            return;
        }
        
        PetActionState fromState = currentTransitionPath[currentPathIndex];
        PetActionState toState = currentTransitionPath[currentPathIndex + 1];
        
        if (showDebugLogs)
            Debug.Log($"Executing transition step: {fromState} → {toState}");
        
        // Play the animation for this step
        PlayStepAnimation(fromState, toState);
    }
    
    private void PlayStepAnimation(PetActionState fromState, PetActionState toState)
    {
        // Check if there's a custom transition animation
        var transitionKey = (fromState, toState);
        if (transitionAnimationMap.TryGetValue(transitionKey, out TransitionAnimationData transitionData) 
            && transitionData.useCustomTransition)
        {
            // Play custom transition animation
            if (!string.IsNullOrEmpty(transitionData.transitionTrigger))
            {
                // Only reset this specific trigger, not all triggers
                animator.ResetTrigger(transitionData.transitionTrigger);
                animator.SetTrigger(transitionData.transitionTrigger);
                OnTransitionAnimationStarted?.Invoke(fromState, toState);
                
                if (showDebugLogs)
                    Debug.Log($"Playing custom transition: {transitionData.transitionTrigger} ({fromState} → {toState})");
            }
            
            // Wait for custom transition to complete
            currentAnimationCoroutine = StartCoroutine(WaitForAnimationStep(transitionData.transitionDuration));
        }
        else
        {
            // Play the target state animation
            PlayStateAnimation(toState);
            
            // Get duration from state animation data
            float duration = 0.5f; // Default
            if (stateAnimationMap.TryGetValue(toState, out StateAnimationData stateData))
            {
                duration = stateData.animationDuration;
            }
            
            // Wait for state animation to complete
            currentAnimationCoroutine = StartCoroutine(WaitForAnimationStep(duration));
        }
    }
    
    private IEnumerator WaitForAnimationStep(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // Animation step completed, advance to next state
        AdvanceToNextState();
    }
    
    private void AdvanceToNextState()
    {
        if (currentTransitionPath == null || !isExecutingSequentialTransition)
            return;
            
        currentPathIndex++;
        
        // Force the state machine to advance to the next state in the path
        if (currentPathIndex < currentTransitionPath.Count)
        {
            PetActionState nextState = currentTransitionPath[currentPathIndex];
            
            if (showDebugLogs)
                Debug.Log($"Advancing state machine to: {nextState}");
            
            // Force set the state in the state machine
            stateMachine.ForceSetState(nextState);
        }
        
        // Continue with next step
        ExecuteNextTransitionStep();
    }
    
    private void CompleteSequentialTransition()
    {
        if (showDebugLogs)
            Debug.Log("Sequential transition completed");
        
        isExecutingSequentialTransition = false;
        currentTransitionPath = null;
        currentPathIndex = 0;
        
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
    }

    // Public methods for external control
    public void SetMoveSpeed(float speed)
    {
        if (animator != null && !string.IsNullOrEmpty(moveSpeedParameter))
        {
            animator.SetFloat(moveSpeedParameter, speed);
        }
    }

    public void SetTurnVelocity(float velocity)
    {
        if (animator != null && !string.IsNullOrEmpty(turnVelocityParameter))
        {
            animator.SetFloat(turnVelocityParameter, velocity);
        }
    }

    public void TriggerCustomAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            // Only reset this specific trigger to prevent conflicts
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
            
            if (showDebugLogs)
                Debug.Log($"Triggered custom animation: {triggerName}");
        }
    }

    public bool IsPlaying(string stateName)
    {
        if (animator == null) return false;
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    public float GetCurrentAnimationTime()
    {
        if (animator == null) return 0f;
        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }
    
    public bool IsExecutingSequentialTransition()
    {
        return isExecutingSequentialTransition;
    }
    
    public List<PetActionState> GetCurrentTransitionPath()
    {
        return currentTransitionPath != null ? new List<PetActionState>(currentTransitionPath) : null;
    }
    
    public void StopCurrentTransition()
    {
        if (isExecutingSequentialTransition)
        {
            CompleteSequentialTransition();
            
            if (showDebugLogs)
                Debug.Log("Sequential transition stopped manually");
        }
    }

    // Debug method to show current animation info
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogCurrentAnimationInfo()
    {
        if (animator != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"Current Animation: {stateInfo.shortNameHash}, Time: {stateInfo.normalizedTime:F2}, Length: {stateInfo.length:F2}");
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            InitializeAnimationMaps();
        }
    }
}