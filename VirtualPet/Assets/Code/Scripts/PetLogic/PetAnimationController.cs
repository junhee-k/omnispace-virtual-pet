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
                    isLooping = true // All states now loop since walk is handled as blend tree within idle
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
            
            // Get duration from animator-based timing
            float duration = GetAnimationDurationForState(toState);
            
            if (showDebugLogs)
                Debug.Log($"Using dynamic duration {duration:F2}s for {toState} state animation");
            
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

    public float GetCurrentAnimationDuration()
    {
        if (animator == null) return 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length;
    }
    
    public float GetAnimationDurationForState(PetActionState state)
    {
        if (animator == null) return 1.0f; // Default fallback
        
        // Special handling for Idle state with blend tree (contains walk animations)
        if (state == PetActionState.Idle)
        {
            // For Idle state (which contains looping idle + walk blend tree), 
            // don't wait for the loop - allow immediate transitions
            return 0.1f; // Minimal wait time for immediate responsiveness
        }
        
        // Priority 1: Try to get from Unity Animator if the state is currently playing
        if (stateMachine != null && stateMachine.CurrentState == state)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // Handle blend trees and complex states
            if (stateInfo.loop)
            {
                // For looping states, use a short reasonable wait time instead of full loop
                return 0.3f;
            }
            else
            {
                // For non-looping states, use a reasonable portion of the clip length
                // Instead of waiting for the full animation, use a shorter settle time
                float clipLength = stateInfo.length;
                return Mathf.Min(clipLength * 0.3f, 1.0f); // Max 1 second, or 30% of clip length
            }
        }
        
        // Priority 2: Try to get from configured animation data
        if (stateAnimationMap.TryGetValue(state, out StateAnimationData stateData))
        {
            // Use reasonable durations instead of full animation lengths
            if (!stateData.isLooping && stateData.animationDuration > 0)
            {
                // Cap configured durations to reasonable values for responsiveness
                return Mathf.Min(stateData.animationDuration * 0.3f, 1.0f);
            }
        }
        
        // Fallback: reasonable default
        return 0.5f;
    }
    
    public float GetTransitionDuration(PetActionState fromState, PetActionState toState)
    {
        if (animator == null) return 0.5f; // Default fallback
        
        // Check if there's a custom transition animation configured
        var transitionKey = (fromState, toState);
        if (transitionAnimationMap.TryGetValue(transitionKey, out TransitionAnimationData transitionData))
        {
            if (transitionData.useCustomTransition)
            {
                return transitionData.transitionDuration;
            }
        }
        
        // If no custom transition, check if animator is currently in a transition
        if (animator.IsInTransition(0))
        {
            AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(0);
            return transitionInfo.duration;
        }
        
        // Fallback: use the duration of the target state animation
        return GetAnimationDurationForState(toState);
    }
    
    public bool IsInTransition()
    {
        if (animator == null) return false;
        return animator.IsInTransition(0);
    }
    
    public float GetRemainingAnimationTime()
    {
        if (animator == null) return 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = stateInfo.normalizedTime % 1.0f; // Handle looping
        return stateInfo.length * (1.0f - normalizedTime);
    }
    
    public float GetSequentialTransitionDuration(List<PetActionState> transitionPath)
    {
        if (transitionPath == null || transitionPath.Count <= 1) return 0f;
        
        float totalDuration = 0f;
        
        for (int i = 0; i < transitionPath.Count - 1; i++)
        {
            PetActionState fromState = transitionPath[i];
            PetActionState toState = transitionPath[i + 1];
            
            totalDuration += GetTransitionDuration(fromState, toState);
        }
        
        if (showDebugLogs)
        {
            string pathString = string.Join(" → ", transitionPath);
            Debug.Log($"Sequential transition duration calculated: {totalDuration:F2}s for path {pathString}");
        }
        
        return totalDuration;
    }
    
    public float GetDynamicAnimationWaitTime()
    {
        if (animator == null) return 0.1f; // Default fallback
        
        // Special case: If we're in Idle state, don't wait for the loop
        if (stateMachine != null && stateMachine.CurrentState == PetActionState.Idle)
        {
            return 0.1f; // Immediate responsiveness for Idle state
        }
        
        // If we're in a transition, wait for it to complete (but cap it)
        if (IsInTransition())
        {
            AnimatorTransitionInfo transitionInfo = animator.GetAnimatorTransitionInfo(0);
            float remainingTransitionTime = transitionInfo.duration * (1.0f - transitionInfo.normalizedTime);
            return Mathf.Clamp(remainingTransitionTime, 0.1f, 0.8f); // Cap at 0.8s max
        }
        
        // For any other states, use reasonable wait times for responsiveness
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.loop)
        {
            // For non-looping animations, use a portion of remaining time, not the full duration
            float remainingTime = GetRemainingAnimationTime();
            float cappedTime = Mathf.Min(remainingTime * 0.3f, 0.8f); // 30% of remaining time, max 0.8s
            return Mathf.Max(cappedTime, 0.1f); // Minimum 0.1s
        }
        
        // For looping animations, use a short wait time
        return 0.2f;
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
            
            if (IsInTransition())
            {
                var transitionInfo = animator.GetAnimatorTransitionInfo(0);
                Debug.Log($"In Transition: Duration: {transitionInfo.duration:F2}s, Progress: {transitionInfo.normalizedTime:F2}");
            }
        }
    }
    
    // Debug method to test dynamic duration system
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogDynamicTimingInfo()
    {
        if (showDebugLogs)
        {
            Debug.Log("=== Dynamic Timing System Info ===");
            Debug.Log($"Current Animation Duration: {GetCurrentAnimationDuration():F2}s");
            Debug.Log($"Dynamic Wait Time: {GetDynamicAnimationWaitTime():F2}s");
            Debug.Log($"Is In Transition: {IsInTransition()}");
            
            if (stateMachine != null)
            {
                PetActionState currentState = stateMachine.CurrentState;
                Debug.Log($"Current State: {currentState}");
                Debug.Log($"Duration for {currentState}: {GetAnimationDurationForState(currentState):F2}s");
            }
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