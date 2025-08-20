using System;
using System.Collections;
using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace PetBehavior
{
    public class SpeechRecognitionManager : MonoBehaviour
    {
        [Header("Whisper Configuration")]
        [SerializeField] private WhisperManager whisperManager;
        [SerializeField] private MicrophoneRecord microphoneRecord;
        
        [Header("Recording Settings")]
        [SerializeField] private float recordingLength = 5f;
        [SerializeField] private float silenceThreshold = 0.01f;
        [SerializeField] private int sampleRate = 16000;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Events
        public System.Action<string, float> OnSpeechRecognized;
        public System.Action OnRecordingStarted;
        public System.Action OnRecordingStopped;
        public System.Action<string> OnError;
        
        // State management
        private bool isInitialized = false;
        private bool isRecording = false;
        private bool isProcessing = false;
        private Coroutine recordingCoroutine;
        
        // Audio processing
        private string selectedMicrophone;
        private AudioClip lastRecordedClip;
        
        public bool IsInitialized => isInitialized;
        public bool IsRecording => isRecording;
        public bool IsProcessing => isProcessing;
        public bool IsAvailable => isInitialized && whisperManager != null && !isProcessing;

        void Start()
        {
            StartCoroutine(InitializeAsync());
        }
        
        private IEnumerator InitializeAsync()
        {
            if (showDebugLogs)
                Debug.Log("[Speech] Initializing Speech Recognition...");
            
            bool initializationSuccessful = false;
            
            // Initialize Whisper if not already done
            if (whisperManager == null)
            {
                try
                {
                    whisperManager = FindObjectOfType<WhisperManager>();
                    if (whisperManager == null)
                    {
                        GameObject whisperGO = new GameObject("WhisperManager");
                        whisperManager = whisperGO.AddComponent<WhisperManager>();
                        whisperGO.transform.SetParent(transform);
                    }
                }
                catch (Exception e)
                {
                    string error = $"Failed to initialize Whisper: {e.Message}";
                    Debug.LogError($"[Speech] {error}");
                    OnError?.Invoke(error);
                    yield break;
                }
            }
            
            // Initialize microphone recording
            if (microphoneRecord == null)
            {
                try
                {
                    GameObject micGO = new GameObject("MicrophoneRecord");
                    microphoneRecord = micGO.AddComponent<MicrophoneRecord>();
                    micGO.transform.SetParent(transform);
                }
                catch (Exception e)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[Speech] Could not create MicrophoneRecord: {e.Message}");
                    // Continue without MicrophoneRecord - we can use fallback
                }
            }
            
            // Wait for Whisper to initialize (if it has IsLoaded property)
            if (whisperManager != null)
            {
                System.Reflection.PropertyInfo isLoadedProperty = null;
                try
                {
                    // Check if IsLoaded property exists
                    isLoadedProperty = whisperManager.GetType().GetProperty("IsLoaded");
                }
                catch (Exception e)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[Speech] Could not check for IsLoaded property: {e.Message}");
                }
                
                // Handle waiting outside of try-catch blocks to avoid CS1626 error
                if (isLoadedProperty != null)
                {
                    bool waitSuccess = false;
                    try
                    {
                        // Validate we can access the property before yielding
                        bool testAccess = (bool)isLoadedProperty.GetValue(whisperManager);
                        waitSuccess = true;
                    }
                    catch (Exception e)
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"[Speech] Cannot access IsLoaded property: {e.Message}");
                        waitSuccess = false;
                    }
                    
                    if (waitSuccess)
                    {
                        yield return new WaitUntil(() => {
                            try {
                                return (bool)isLoadedProperty.GetValue(whisperManager);
                            } catch {
                                return true; // If access fails, assume loaded
                            }
                        });
                        initializationSuccessful = true;
                    }
                    else
                    {
                        // If we can't access the property, just wait a bit
                        yield return new WaitForSeconds(1.0f);
                        initializationSuccessful = true;
                    }
                }
                else
                {
                    // Wait a bit for initialization if no IsLoaded property
                    yield return new WaitForSeconds(1.0f);
                    initializationSuccessful = true;
                }
            }
            
            // Initialize microphone
            bool microphoneInitialized = false;
            try
            {
                InitializeMicrophone();
                microphoneInitialized = true;
            }
            catch (Exception e)
            {
                string error = $"Failed to initialize microphone: {e.Message}";
                Debug.LogError($"[Speech] {error}");
                OnError?.Invoke(error);
            }
            
            if (!microphoneInitialized)
            {
                yield break;
            }
            
            if (initializationSuccessful)
            {
                isInitialized = true;
                
                if (showDebugLogs)
                    Debug.Log("[Speech] Speech Recognition initialized successfully");
                
                // Start continuous recording
                StartContinuousRecording();
            }
            else
            {
                string error = "Speech recognition initialization failed";
                Debug.LogError($"[Speech] {error}");
                OnError?.Invoke(error);
            }
        }
        
        private void InitializeMicrophone()
        {
            // Check for available microphones
            if (Microphone.devices.Length == 0)
            {
                throw new Exception("No microphones detected");
            }
            
            // Select the first available microphone
            selectedMicrophone = Microphone.devices[0];
            
            if (showDebugLogs)
                Debug.Log($"[Speech] Selected microphone: {selectedMicrophone}");
            
            // Configure microphone record component
            if (microphoneRecord != null)
            {
                try
                {
                    // Use reflection to check if OnRecordStop event exists before subscribing
                    var onRecordStopEvent = microphoneRecord.GetType().GetEvent("OnRecordStop");
                    if (onRecordStopEvent != null)
                    {
                        // Try to get the UnityEvent property
                        var onRecordStopProperty = microphoneRecord.GetType().GetProperty("OnRecordStop");
                        if (onRecordStopProperty != null)
                        {
                            var unityEvent = onRecordStopProperty.GetValue(microphoneRecord);
                            if (unityEvent != null)
                            {
                                // Use reflection to call AddListener
                                var addListenerMethod = unityEvent.GetType().GetMethod("AddListener");
                                if (addListenerMethod != null)
                                {
                                    addListenerMethod.Invoke(unityEvent, new object[] { new UnityEngine.Events.UnityAction<AudioClip>(OnRecordingComplete) });
                                    if (showDebugLogs)
                                        Debug.Log("[Speech] Successfully subscribed to OnRecordStop event");
                                }
                            }
                        }
                    }
                    else if (showDebugLogs)
                    {
                        Debug.Log("[Speech] MicrophoneRecord.OnRecordStop event not found, using fallback method");
                    }
                }
                catch (Exception e)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[Speech] Could not subscribe to MicrophoneRecord events: {e.Message}");
                }
            }
        }
        
        public void StartContinuousRecording()
        {
            if (!isInitialized || isRecording)
                return;
                
            if (recordingCoroutine != null)
                StopCoroutine(recordingCoroutine);
                
            recordingCoroutine = StartCoroutine(ContinuousRecordingLoop());
        }
        
        public void StopContinuousRecording()
        {
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                recordingCoroutine = null;
            }
            
            StopRecording();
        }
        
        private IEnumerator ContinuousRecordingLoop()
        {
            while (isInitialized)
            {
                if (!isProcessing && IsAvailable)
                {
                    yield return StartCoroutine(RecordAndProcess());
                }
                
                yield return new WaitForSeconds(0.1f); // Small delay between recording attempts
            }
        }
        
        private IEnumerator RecordAndProcess()
        {
            isRecording = true;
            OnRecordingStarted?.Invoke();
            
            if (showDebugLogs)
                Debug.Log("[Speech] Starting recording...");
            
            bool recordingSuccess = false;
            AudioClip recordedClip = null;
            
            // Start recording using MicrophoneRecord
            if (microphoneRecord != null)
            {
                try
                {
                    microphoneRecord.StartRecord();
                    recordingSuccess = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Speech] Failed to start recording: {e.Message}");
                    OnError?.Invoke($"Recording start error: {e.Message}");
                }
                
                if (recordingSuccess)
                {
                    // Wait for recording duration or until stopped
                    float elapsed = 0f;
                    while (elapsed < recordingLength && isRecording && microphoneRecord.IsRecording)
                    {
                        yield return new WaitForSeconds(0.1f);
                        elapsed += 0.1f;
                    }
                    
                    // Stop recording
                    try
                    {
                        microphoneRecord.StopRecord();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Speech] Error stopping recording: {e.Message}");
                        OnError?.Invoke($"Recording stop error: {e.Message}");
                    }
                }
            }
            else
            {
                // Fallback: Direct microphone recording
                try
                {
                    lastRecordedClip = Microphone.Start(selectedMicrophone, false, (int)recordingLength, sampleRate);
                    recordingSuccess = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Speech] Failed to start microphone: {e.Message}");
                    OnError?.Invoke($"Microphone start error: {e.Message}");
                }
                
                if (recordingSuccess)
                {
                    yield return new WaitForSeconds(recordingLength);
                    
                    try
                    {
                        Microphone.End(selectedMicrophone);
                        recordedClip = lastRecordedClip;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Speech] Error ending microphone recording: {e.Message}");
                        OnError?.Invoke($"Microphone end error: {e.Message}");
                    }
                }
            }
            
            // Process recorded audio
            if (recordedClip != null)
            {
                yield return StartCoroutine(ProcessAudioClip(recordedClip));
            }
            
            isRecording = false;
            OnRecordingStopped?.Invoke();
        }
        
        private void OnRecordingComplete(AudioClip clip)
        {
            if (clip != null && clip.length > 0.1f) // Only process clips longer than 0.1 seconds
            {
                StartCoroutine(ProcessAudioClip(clip));
            }
        }
        
        private IEnumerator ProcessAudioClip(AudioClip clip)
        {
            if (whisperManager == null || clip == null)
                yield break;
                
            isProcessing = true;
            
            if (showDebugLogs)
                Debug.Log($"[Speech] Processing audio clip: {clip.length}s");
            
            // Convert AudioClip to float array if needed for Whisper
            float[] audioData = null;
            try
            {
                audioData = AudioClipToFloatArray(clip);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Speech] Error converting audio data: {e.Message}");
                OnError?.Invoke($"Audio conversion error: {e.Message}");
                isProcessing = false;
                yield break;
            }
            
            // Check for silence (avoid processing empty audio)
            if (IsAudioSilent(audioData))
            {
                if (showDebugLogs)
                    Debug.Log("[Speech] Audio is silent, skipping processing");
                isProcessing = false;
                yield break;
            }
            
            // Process with Whisper using reflection for compatibility
            object whisperTask = null;
            try
            {
                // Use reflection to call GetTextAsync to avoid type dependency issues
                var getTextAsyncMethod = whisperManager.GetType().GetMethod("GetTextAsync");
                if (getTextAsyncMethod != null)
                {
                    whisperTask = getTextAsyncMethod.Invoke(whisperManager, new object[] { clip });
                }
                else
                {
                    Debug.LogError("[Speech] GetTextAsync method not found on WhisperManager");
                    OnError?.Invoke("Whisper method not available");
                    isProcessing = false;
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Speech] Error starting Whisper processing: {e.Message}");
                OnError?.Invoke($"Whisper start error: {e.Message}");
                isProcessing = false;
                yield break;
            }
            
            // Wait for completion using reflection
            if (whisperTask != null)
            {
                var taskType = whisperTask.GetType();
                var isCompletedProperty = taskType.GetProperty("IsCompleted");
                
                if (isCompletedProperty != null)
                {
                    yield return new WaitUntil(() => (bool)isCompletedProperty.GetValue(whisperTask));
                    
                    // Process result using reflection
                    try
                    {
                        var isCompletedSuccessfullyProperty = taskType.GetProperty("IsCompletedSuccessfully");
                        var resultProperty = taskType.GetProperty("Result");
                        
                        if (isCompletedSuccessfullyProperty != null && resultProperty != null)
                        {
                            bool isSuccess = (bool)isCompletedSuccessfullyProperty.GetValue(whisperTask);
                            if (isSuccess)
                            {
                                var result = resultProperty.GetValue(whisperTask);
                                if (result != null)
                                {
                                    // Try to get the Result property from the whisper result
                                    var textProperty = result.GetType().GetProperty("Result");
                                    if (textProperty != null)
                                    {
                                        string recognizedText = textProperty.GetValue(result) as string;
                                        if (!string.IsNullOrWhiteSpace(recognizedText))
                                        {
                                            recognizedText = recognizedText.Trim();
                                            
                                            if (showDebugLogs)
                                                Debug.Log($"[Speech] Recognized: '{recognizedText}'");
                                            
                                            // Emit recognition event
                                            OnSpeechRecognized?.Invoke(recognizedText, 1.0f);
                                        }
                                        else if (showDebugLogs)
                                        {
                                            Debug.Log("[Speech] Empty text recognized from audio");
                                        }
                                    }
                                    else if (showDebugLogs)
                                    {
                                        Debug.Log("[Speech] Could not find Result property in Whisper result");
                                    }
                                }
                            }
                            else if (showDebugLogs)
                            {
                                Debug.Log("[Speech] Whisper processing was not successful");
                            }
                        }
                        else if (showDebugLogs)
                        {
                            Debug.Log("[Speech] Could not access task completion properties");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Speech] Error processing Whisper result: {e.Message}");
                        OnError?.Invoke($"Result processing error: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError("[Speech] Could not find IsCompleted property on Whisper task");
                    OnError?.Invoke("Whisper task completion check failed");
                }
            }
            
            isProcessing = false;
        }
        
        private float[] AudioClipToFloatArray(AudioClip clip)
        {
            float[] audioData = new float[clip.samples * clip.channels];
            clip.GetData(audioData, 0);
            return audioData;
        }
        
        private bool IsAudioSilent(float[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
                return true;
                
            float maxAmplitude = 0f;
            for (int i = 0; i < audioData.Length; i++)
            {
                float amplitude = Mathf.Abs(audioData[i]);
                if (amplitude > maxAmplitude)
                    maxAmplitude = amplitude;
            }
            
            return maxAmplitude < silenceThreshold;
        }
        
        public void StopRecording()
        {
            if (isRecording)
            {
                isRecording = false;
                
                if (microphoneRecord != null && microphoneRecord.IsRecording)
                {
                    microphoneRecord.StopRecord();
                }
                
                if (Microphone.IsRecording(selectedMicrophone))
                {
                    Microphone.End(selectedMicrophone);
                }
            }
        }
        
        public void SetRecordingLength(float length)
        {
            recordingLength = Mathf.Clamp(length, 1f, 30f);
        }
        
        public void SetSilenceThreshold(float threshold)
        {
            silenceThreshold = Mathf.Clamp(threshold, 0.001f, 0.1f);
        }
        
        void OnDestroy()
        {
            StopContinuousRecording();
            
            if (microphoneRecord != null)
            {
                try
                {
                    // Use reflection to unsubscribe from OnRecordStop event
                    var onRecordStopProperty = microphoneRecord.GetType().GetProperty("OnRecordStop");
                    if (onRecordStopProperty != null)
                    {
                        var unityEvent = onRecordStopProperty.GetValue(microphoneRecord);
                        if (unityEvent != null)
                        {
                            // Use reflection to call RemoveListener
                            var removeListenerMethod = unityEvent.GetType().GetMethod("RemoveListener");
                            if (removeListenerMethod != null)
                            {
                                removeListenerMethod.Invoke(unityEvent, new object[] { new UnityEngine.Events.UnityAction<AudioClip>(OnRecordingComplete) });
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[Speech] Could not unsubscribe from MicrophoneRecord events: {e.Message}");
                }
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopContinuousRecording();
            }
            else if (isInitialized)
            {
                StartContinuousRecording();
            }
        }
        
        // Debug methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogStatus()
        {
            Debug.Log($"[Speech] Status - Initialized: {isInitialized}, Recording: {isRecording}, Processing: {isProcessing}");
            Debug.Log($"[Speech] Microphone: {selectedMicrophone}, Available devices: {Microphone.devices.Length}");
        }
    }
}