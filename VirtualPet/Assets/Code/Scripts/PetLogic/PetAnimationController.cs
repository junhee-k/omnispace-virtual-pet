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
    private Coroutine currentTransitionCoroutine;
    
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
            stateMachine.OnTransitionStarted += HandleTransitionStarted;
            stateMachine.OnTransitionCompleted += HandleTransitionCompleted;
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
            stateMachine.OnTransitionStarted -= HandleTransitionStarted;
            stateMachine.OnTransitionCompleted -= HandleTransitionCompleted;
            stateMachine.OnTransitionPathCalculated -= HandleTransitionPathCalculated;
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
        
        PlayStateAnimation(toState);
    }

    private void HandleTransitionStarted(PetActionState targetState)
    {
        if (showDebugLogs)
            Debug.Log($"Animation Controller: Transition started to {targetState}");
    }

    private void HandleTransitionCompleted()
    {
        if (showDebugLogs)
            Debug.Log($"Animation Controller: Transition completed");
    }

    private void HandleTransitionPathCalculated(List<PetActionState> transitionPath)
    {
        if (showDebugLogs)
        {
            string pathString = string.Join(" → ", transitionPath);
            Debug.Log($"Animation Controller: Transition path received: {pathString}");
        }
        
        // Start playing transition animations for the path
        if (currentTransitionCoroutine != null)
        {
            StopCoroutine(currentTransitionCoroutine);
        }
        currentTransitionCoroutine = StartCoroutine(PlayTransitionPath(transitionPath));
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

    private IEnumerator PlayTransitionPath(List<PetActionState> path)
    {
        for (int i = 1; i < path.Count; i++)
        {
            PetActionState fromState = path[i - 1];
            PetActionState toState = path[i];
            
            // Check if there's a custom transition animation
            var transitionKey = (fromState, toState);
            if (transitionAnimationMap.TryGetValue(transitionKey, out TransitionAnimationData transitionData) 
                && transitionData.useCustomTransition)
            {
                // Play custom transition animation
                if (!string.IsNullOrEmpty(transitionData.transitionTrigger))
                {
                    animator.SetTrigger(transitionData.transitionTrigger);
                    OnTransitionAnimationStarted?.Invoke(fromState, toState);
                    
                    if (showDebugLogs)
                        Debug.Log($"Playing custom transition: {transitionData.transitionTrigger} ({fromState} → {toState})");
                }
                
                yield return new WaitForSeconds(transitionData.transitionDuration);
                OnTransitionAnimationCompleted?.Invoke(fromState, toState);
            }
            else
            {
                // Use default state animation timing
                if (stateAnimationMap.TryGetValue(toState, out StateAnimationData stateData))
                {
                    yield return new WaitForSeconds(stateData.animationDuration);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f); // Default timing
                }
            }
        }
        
        currentTransitionCoroutine = null;
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