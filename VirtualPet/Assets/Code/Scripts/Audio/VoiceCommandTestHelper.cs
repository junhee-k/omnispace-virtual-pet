using UnityEngine;
using UnityEngine.UI;

namespace PetBehavior
{
    /// <summary>
    /// Helper script for testing voice commands without requiring actual speech input.
    /// Useful for debugging and development when microphone access is limited.
    /// </summary>
    public class VoiceCommandTestHelper : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private VoiceCommandProcessor voiceProcessor;
        [SerializeField] private SpeechRecognitionManager speechManager;
        
        [Header("Test Configuration")]
        [SerializeField] private string testPetName = "Buddy";
        [SerializeField] private bool showDebugUI = true;
        
        [Header("UI References (Optional)")]
        [SerializeField] private Text statusText;
        [SerializeField] private Button testSitButton;
        [SerializeField] private Button testFollowButton;
        [SerializeField] private Button testPetNameButton;
        
        void Start()
        {
            InitializeComponents();
            SetupTestButtons();
            UpdateStatusDisplay();
        }
        
        void Update()
        {
            // Keyboard shortcuts for testing
            if (Input.GetKeyDown(KeyCode.F1))
                TestSitCommand();
            if (Input.GetKeyDown(KeyCode.F2))
                TestLyingCommand();
            if (Input.GetKeyDown(KeyCode.F3))
                TestSleepCommand();
            if (Input.GetKeyDown(KeyCode.F4))
                TestFollowCommand();
            if (Input.GetKeyDown(KeyCode.F5))
                TestPetNameCommand();
            
            UpdateStatusDisplay();
        }
        
        private void InitializeComponents()
        {
            if (voiceProcessor == null)
                voiceProcessor = FindObjectOfType<VoiceCommandProcessor>();
            
            if (speechManager == null)
                speechManager = FindObjectOfType<SpeechRecognitionManager>();
        }
        
        private void SetupTestButtons()
        {
            if (testSitButton != null)
                testSitButton.onClick.AddListener(TestSitCommand);
            
            if (testFollowButton != null)
                testFollowButton.onClick.AddListener(TestFollowCommand);
            
            if (testPetNameButton != null)
                testPetNameButton.onClick.AddListener(TestPetNameCommand);
        }
        
        private void UpdateStatusDisplay()
        {
            if (statusText != null)
            {
                string status = "Voice Command Status:\n";
                status += $"Voice Processor: {(voiceProcessor != null ? "✓" : "✗")}\n";
                status += $"Speech Manager: {(speechManager != null ? "✓" : "✗")}\n";
                
                if (speechManager != null)
                {
                    status += $"Initialized: {(speechManager.IsInitialized ? "✓" : "✗")}\n";
                    status += $"Recording: {(speechManager.IsRecording ? "✓" : "✗")}\n";
                    status += $"Processing: {(speechManager.IsProcessing ? "✓" : "✗")}\n";
                }
                
                status += "\nTest Controls:\n";
                status += "F1: Test 'sit' command\n";
                status += "F2: Test 'lie down' command\n";
                status += "F3: Test 'sleep' command\n";
                status += "F4: Test 'follow' command\n";
                status += "F5: Test pet name command";
                
                statusText.text = status;
            }
        }
        
        // Test methods that simulate voice input
        [ContextMenu("Test Sit Command")]
        public void TestSitCommand()
        {
            SimulateVoiceInput("sit");
        }
        
        [ContextMenu("Test Lying Command")]
        public void TestLyingCommand()
        {
            SimulateVoiceInput("lie down");
        }
        
        [ContextMenu("Test Sleep Command")]
        public void TestSleepCommand()
        {
            SimulateVoiceInput("sleep");
        }
        
        [ContextMenu("Test Flat Command")]
        public void TestFlatCommand()
        {
            SimulateVoiceInput("play dead");
        }
        
        [ContextMenu("Test Idle Command")]
        public void TestIdleCommand()
        {
            SimulateVoiceInput("stand up");
        }
        
        [ContextMenu("Test Follow Command")]
        public void TestFollowCommand()
        {
            SimulateVoiceInput("follow me");
        }
        
        [ContextMenu("Test Pet Name Command")]
        public void TestPetNameCommand()
        {
            SimulateVoiceInput(testPetName);
        }
        
        /// <summary>
        /// Simulates voice input by directly calling the speech recognition callback
        /// </summary>
        private void SimulateVoiceInput(string text)
        {
            bool simulationSuccessful = false;
            
            if (speechManager != null)
            {
                try
                {
                    // Use reflection to trigger the OnSpeechRecognized event
                    var onSpeechRecognizedField = speechManager.GetType().GetField("OnSpeechRecognized");
                    if (onSpeechRecognizedField != null)
                    {
                        var speechRecognizedEvent = onSpeechRecognizedField.GetValue(speechManager) as System.Action<string, float>;
                        speechRecognizedEvent?.Invoke(text, 1.0f);
                        simulationSuccessful = true;
                        Debug.Log($"[VoiceTest] Simulated voice input via speech manager: '{text}'");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[VoiceTest] Could not simulate via speech manager: {e.Message}");
                }
            }
            
            if (!simulationSuccessful && voiceProcessor != null)
            {
                try
                {
                    // Fallback: Try to call voice processor directly
                    var method = voiceProcessor.GetType().GetMethod("OnSpeechRecognized", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(voiceProcessor, new object[] { text, 1.0f });
                        simulationSuccessful = true;
                        Debug.Log($"[VoiceTest] Simulated voice input via processor: '{text}'");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[VoiceTest] Could not simulate via voice processor: {e.Message}");
                }
            }
            
            if (!simulationSuccessful)
            {
                Debug.LogWarning("[VoiceTest] No voice system components found for testing");
            }
        }
        
        /// <summary>
        /// Enable or disable voice recognition
        /// </summary>
        public void SetVoiceRecognitionEnabled(bool enabled)
        {
            if (voiceProcessor != null)
            {
                if (enabled)
                    voiceProcessor.EnableVoiceRecognition();
                else
                    voiceProcessor.DisableVoiceRecognition();
                
                Debug.Log($"[VoiceTest] Voice recognition {(enabled ? "enabled" : "disabled")}");
            }
        }
        
        /// <summary>
        /// Change the pet name for testing
        /// </summary>
        public void SetTestPetName(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                testPetName = newName;
                if (voiceProcessor != null)
                {
                    voiceProcessor.SetPetName(newName);
                    Debug.Log($"[VoiceTest] Pet name changed to: '{newName}'");
                }
            }
        }
        
        void OnGUI()
        {
            if (!showDebugUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label("Voice Command Test Helper", "Box");
            
            if (GUILayout.Button("Test 'sit' Command"))
                TestSitCommand();
            
            if (GUILayout.Button("Test 'lie down' Command"))
                TestLyingCommand();
                
            if (GUILayout.Button("Test 'sleep' Command"))
                TestSleepCommand();
                
            if (GUILayout.Button("Test 'follow me' Command"))
                TestFollowCommand();
                
            if (GUILayout.Button($"Test Pet Name '{testPetName}'"))
                TestPetNameCommand();
            
            GUILayout.Space(10);
            
            GUILayout.Label("Pet Name:");
            string newPetName = GUILayout.TextField(testPetName);
            if (newPetName != testPetName)
                SetTestPetName(newPetName);
            
            GUILayout.Space(10);
            
            if (speechManager != null)
            {
                GUILayout.Label($"Speech Status: {(speechManager.IsInitialized ? "Ready" : "Initializing")}");
                GUILayout.Label($"Recording: {speechManager.IsRecording}");
                GUILayout.Label($"Processing: {speechManager.IsProcessing}");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        void OnDestroy()
        {
            // Clean up button listeners
            if (testSitButton != null)
                testSitButton.onClick.RemoveListener(TestSitCommand);
            
            if (testFollowButton != null)
                testFollowButton.onClick.RemoveListener(TestFollowCommand);
            
            if (testPetNameButton != null)
                testPetNameButton.onClick.RemoveListener(TestPetNameCommand);
        }
    }
}