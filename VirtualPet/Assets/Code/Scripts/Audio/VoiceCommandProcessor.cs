using System.Linq;
using UnityEngine;

namespace PetBehavior
{
    [RequireComponent(typeof(SpeechRecognitionManager))]
    public class VoiceCommandProcessor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VoiceCommandConfigAsset configAsset;
        [SerializeField] private VoiceCommandConfig config = new VoiceCommandConfig();
        
        [Header("Component References")]
        [SerializeField] private PetMoveVR petMoveVR;
        [SerializeField] private LLMCommandExecutor llmCommandExecutor;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Internal references
        private SpeechRecognitionManager speechRecognition;
        
        // Events for debugging/monitoring
        public System.Action<string, PetActionState> OnStateCommandRecognized;
        public System.Action<string> OnFollowCommandRecognized;
        public System.Action<string> OnPetNameRecognized;
        public System.Action<string> OnUnrecognizedCommand;

        void Start()
        {
            InitializeComponents();
            InitializeConfiguration();
            SubscribeToEvents();
        }
        
        private void InitializeComponents()
        {
            // Get speech recognition manager
            speechRecognition = GetComponent<SpeechRecognitionManager>();
            if (speechRecognition == null)
            {
                Debug.LogError("[Voice] SpeechRecognitionManager not found!");
                enabled = false;
                return;
            }
            
            // Find PetMoveVR if not assigned
            if (petMoveVR == null)
            {
                petMoveVR = FindObjectOfType<PetMoveVR>();
                if (petMoveVR == null)
                {
                    Debug.LogError("[Voice] PetMoveVR not found!");
                    enabled = false;
                    return;
                }
            }
            
            // Find LLMCommandExecutor if not assigned
            if (llmCommandExecutor == null)
            {
                llmCommandExecutor = FindObjectOfType<LLMCommandExecutor>();
                if (llmCommandExecutor == null)
                {
                    Debug.LogError("[Voice] LLMCommandExecutor not found!");
                    enabled = false;
                    return;
                }
            }
            
            if (showDebugLogs)
                Debug.Log("[Voice] VoiceCommandProcessor initialized successfully");
        }
        
        private void InitializeConfiguration()
        {
            // Use ScriptableObject config if available, otherwise use serialized config
            if (configAsset != null)
            {
                config = configAsset.config;
                if (showDebugLogs)
                    Debug.Log("[Voice] Using ScriptableObject configuration");
            }
            else if (showDebugLogs)
            {
                Debug.Log("[Voice] Using serialized configuration");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (speechRecognition != null)
            {
                speechRecognition.OnSpeechRecognized += OnSpeechRecognized;
                speechRecognition.OnError += OnSpeechError;
                
                if (showDebugLogs)
                {
                    speechRecognition.OnRecordingStarted += () => Debug.Log("[Voice] Recording started");
                    speechRecognition.OnRecordingStopped += () => Debug.Log("[Voice] Recording stopped");
                }
            }
        }
        
        private void OnSpeechRecognized(string recognizedText, float confidence)
        {
            if (string.IsNullOrWhiteSpace(recognizedText))
                return;
                
            // Check confidence threshold
            if (confidence < config.confidenceThreshold)
            {
                if (showDebugLogs)
                    Debug.Log($"[Voice] Ignored low confidence: '{recognizedText}' ({confidence:F2})");
                return;
            }
            
            string lowerText = recognizedText.ToLower().Trim();
            
            if (showDebugLogs)
                Debug.Log($"[Voice] Processing: '{recognizedText}' (confidence: {confidence:F2})");
            
            // Check for pet name (highest priority - triggers follow mode)
            if (ContainsWord(lowerText, config.petName.ToLower()))
            {
                ProcessPetNameCommand(recognizedText);
                return;
            }
            
            // Check for explicit follow commands
            if (ContainsAnyCommand(lowerText, config.followCommands))
            {
                ProcessFollowCommand(recognizedText);
                return;
            }
            
            // Check for behavior state commands
            PetActionState? recognizedState = RecognizeBehaviorCommand(lowerText);
            if (recognizedState.HasValue)
            {
                ProcessStateCommand(recognizedText, recognizedState.Value);
                return;
            }
            
            // No command recognized
            if (showDebugLogs)
                Debug.Log($"[Voice] Unrecognized command: '{recognizedText}'");
            OnUnrecognizedCommand?.Invoke(recognizedText);
        }
        
        private PetActionState? RecognizeBehaviorCommand(string lowerText)
        {
            if (ContainsAnyCommand(lowerText, config.sitCommands))
                return PetActionState.Sit;
                
            if (ContainsAnyCommand(lowerText, config.lyingCommands))
                return PetActionState.Lying;
                
            if (ContainsAnyCommand(lowerText, config.sleepCommands))
                return PetActionState.Sleep;
                
            if (ContainsAnyCommand(lowerText, config.flatCommands))
                return PetActionState.Flat;
                
            if (ContainsAnyCommand(lowerText, config.idleCommands))
                return PetActionState.Idle;
                
            return null;
        }
        
        private bool ContainsAnyCommand(string text, string[] commands)
        {
            return commands.Any(command => ContainsWord(text, command.ToLower()));
        }
        
        private bool ContainsWord(string text, string word)
        {
            // Simple word boundary check - can be improved with regex if needed
            return text.Contains(word) && (
                text == word || 
                text.StartsWith(word + " ") || 
                text.EndsWith(" " + word) || 
                text.Contains(" " + word + " ")
            );
        }
        
        private void ProcessPetNameCommand(string originalText)
        {
            if (showDebugLogs)
                Debug.Log($"[Voice] Pet name '{config.petName}' recognized, activating follow mode");
            
            // Force user control and activate follow mode
            ForceUserControlAndExecute(() => {
                if (petMoveVR != null)
                {
                    // Use reflection to call the private QueueFollowCameraCommand method
                    var method = petMoveVR.GetType().GetMethod("QueueFollowCameraCommand", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(petMoveVR, new object[] {});
                    
                    if (showDebugLogs)
                        Debug.Log("[Voice] Follow mode activated via pet name");
                }
            });
            
            OnPetNameRecognized?.Invoke(originalText);
        }
        
        private void ProcessFollowCommand(string originalText)
        {
            if (showDebugLogs)
                Debug.Log($"[Voice] Follow command recognized: '{originalText}'");
            
            // Force user control and activate follow mode
            ForceUserControlAndExecute(() => {
                if (petMoveVR != null)
                {
                    // Use reflection to call the private QueueFollowCameraCommand method
                    var method = petMoveVR.GetType().GetMethod("QueueFollowCameraCommand", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(petMoveVR, new object[] {});
                    
                    if (showDebugLogs)
                        Debug.Log("[Voice] Follow mode activated via command");
                }
            });
            
            OnFollowCommandRecognized?.Invoke(originalText);
        }
        
        private void ProcessStateCommand(string originalText, PetActionState targetState)
        {
            if (showDebugLogs)
                Debug.Log($"[Voice] State command recognized: '{originalText}' → {targetState}");
            
            // Force user control and execute state command
            ForceUserControlAndExecute(() => {
                if (petMoveVR != null)
                {
                    petMoveVR.QueueStateCommand(targetState);
                    
                    if (showDebugLogs)
                        Debug.Log($"[Voice] Queued state command: {targetState}");
                }
            });
            
            OnStateCommandRecognized?.Invoke(originalText, targetState);
        }
        
        private void ForceUserControlAndExecute(System.Action action)
        {
            // Switch to user control mode
            if (llmCommandExecutor != null)
            {
                llmCommandExecutor.ForceUserControl();
                
                if (showDebugLogs)
                    Debug.Log("[Voice] Switched to user control mode");
            }
            
            // Execute the action
            action?.Invoke();
        }
        
        private void OnSpeechError(string error)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[Voice] Speech recognition error: {error}");
        }
        
        // Public methods for external control
        public void EnableVoiceRecognition()
        {
            if (speechRecognition != null)
            {
                speechRecognition.StartContinuousRecording();
                if (showDebugLogs)
                    Debug.Log("[Voice] Voice recognition enabled");
            }
        }
        
        public void DisableVoiceRecognition()
        {
            if (speechRecognition != null)
            {
                speechRecognition.StopContinuousRecording();
                if (showDebugLogs)
                    Debug.Log("[Voice] Voice recognition disabled");
            }
        }
        
        public void SetPetName(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                config.petName = newName.Trim();
                if (showDebugLogs)
                    Debug.Log($"[Voice] Pet name changed to: '{config.petName}'");
            }
        }
        
        public void SetConfidenceThreshold(float threshold)
        {
            config.confidenceThreshold = Mathf.Clamp01(threshold);
            if (showDebugLogs)
                Debug.Log($"[Voice] Confidence threshold set to: {config.confidenceThreshold:F2}");
        }
        
        // Debug and testing methods
        [ContextMenu("Test Pet Name Command")]
        public void TestPetNameCommand()
        {
            OnSpeechRecognized(config.petName, 1.0f);
        }
        
        [ContextMenu("Test Sit Command")]
        public void TestSitCommand()
        {
            OnSpeechRecognized("sit", 1.0f);
        }
        
        [ContextMenu("Test Follow Command")]
        public void TestFollowCommand()
        {
            OnSpeechRecognized("come here", 1.0f);
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (speechRecognition != null)
            {
                speechRecognition.OnSpeechRecognized -= OnSpeechRecognized;
                speechRecognition.OnError -= OnSpeechError;
            }
        }
        
        // Debug method to show current configuration
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogConfiguration()
        {
            if (config != null)
            {
                Debug.Log($"[Voice] Configuration - Pet Name: '{config.petName}', Confidence: {config.confidenceThreshold:F2}");
                Debug.Log($"[Voice] Sit Commands: [{string.Join(", ", config.sitCommands)}]");
                Debug.Log($"[Voice] Follow Commands: [{string.Join(", ", config.followCommands)}]");
            }
        }
    }
}