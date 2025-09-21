using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Whisper.Utils;
using PetBehavior;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;


namespace Whisper.Samples
{
    public class StreamingSampleMic : MonoBehaviour
    {
        [Header("Whisper Setup")]
        [SerializeField] private WhisperManager whisper;
        [SerializeField] private MicrophoneRecord microphoneRecord;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private TextMeshProUGUI debugText;
        [SerializeField] private WhisperStream _stream;

        [Header("Spatial Input")]
        [SerializeField] private InputActionReference m_Touch;

        [Header("Pet Control")]
        [SerializeField] private PetMove petMove;

        private async void Start()
        {
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated += OnResult;
            microphoneRecord.OnRecordStop += OnRecordStop;

            foreach (string device in Microphone.devices)
            {
                Debug.Log("Device Name: " + device);
            }
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            if (m_Touch != null)
            {
                m_Touch.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (m_Touch != null)
            {
                m_Touch.action.Disable();
            }
        }

        private void Update()
        {
            if (m_Touch == null) return;

            var activeTouches = Touch.activeTouches;
            if (activeTouches.Count > 0)
            {
                var primaryTouchPhase = activeTouches[0].phase;

                if (primaryTouchPhase == TouchPhase.Began && !microphoneRecord.IsRecording)
                {
                    // Start recording (pinch detected)
                    Debug.Log("[Voice] Pinch detected - Starting recording");
                    debugText.text = "[Voice] Pinch detected - Starting recording";
                    _stream.StartStream();
                    microphoneRecord.StartRecord();
                }
                else if ((primaryTouchPhase == TouchPhase.Ended || primaryTouchPhase == TouchPhase.Canceled) && microphoneRecord.IsRecording)
                {
                    // Stop recording (pinch released or canceled)
                    Debug.Log("[Voice] Pinch released - Stopping recording");
                    debugText.text = "[Voice] Pinch released - Stopping recording";
                    microphoneRecord.StopRecord();
                }
            }
            else if (activeTouches.Count == 0 && microphoneRecord.IsRecording)
            {
                // No active touches but still recording - force stop
                Debug.Log("[Voice] No active touches - Force stopping recording");
                debugText.text = "[Voice] No active touches - Force stopping recording";
                microphoneRecord.StopRecord();
            }
        }

        private void OnRecordStop(AudioChunk recordedAudio)
        {
            Debug.Log("[Voice] Recording stopped");
            debugText.text = "[Voice] Recording stopped";
        }

        private void OnResult(string result)
        {
            text.text = result;

            // Process voice command if pet is available and result is not empty
            if (petMove != null && !string.IsNullOrWhiteSpace(result))
            {
                ProcessVoiceCommand(result.ToLower().Trim());
            }
        }

        private void ProcessVoiceCommand(string command)
        {
            // Find PetMove dynamically if not assigned
            if (petMove == null)
            {
                petMove = FindObjectOfType<PetMove>();
            }

            if (petMove == null)
            {
                Debug.LogWarning("[Voice] No pet found to process voice command");
                return;
            }

            Debug.Log($"[Voice] Processing command: '{command}'");

            // State transition commands
            if (command.Contains("sit"))
            {
                petMove.ProcessVoiceCommand("sit");
            }
            else if (command.Contains("lie down") || command.Contains("lying") || command.Contains("lay down"))
            {
                petMove.ProcessVoiceCommand("lying");
            }
            else if (command.Contains("sleep"))
            {
                petMove.ProcessVoiceCommand("sleep");
            }
            else if (command.Contains("stand") || command.Contains("idle") || command.Contains("get up"))
            {
                petMove.ProcessVoiceCommand("idle");
            }
            else if (command.Contains("flat"))
            {
                petMove.ProcessVoiceCommand("flat");
            }
            // Movement commands
            else if (command.Contains("come here") || command.Contains("follow me") || command.Contains("come"))
            {
                petMove.ProcessVoiceCommand("follow");
            }
            else if (command.Contains("stop") || command.Contains("stay"))
            {
                petMove.ProcessVoiceCommand("stop");
            }
            else
            {
                Debug.Log($"[Voice] Unknown command: '{command}'");
            }
        }
    }
}

