using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Whisper.Utils;
using PetBehavior;


namespace Whisper.Samples
{
    public class StreamingSampleMic : MonoBehaviour
    {
        [Header("Whisper Setup")]
        [SerializeField] private WhisperManager whisper;
        [SerializeField] private MicrophoneRecord microphoneRecord;

        [Header("UI")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private TextMeshProUGUI commandText;
        [SerializeField] private WhisperStream _stream;
        
        [Header("Pet Control")]
        [SerializeField] private PetMove petMove;

        private readonly HashSet<string> sitCommandWords = new HashSet<string>
        {
            "sit",
            "stand",
            "go",
            "hello"
        };

        private async void Start()
        {
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated += OnResult;
            _stream.OnSegmentFinished += OnSegmentFinished;
            microphoneRecord.OnRecordStop += OnRecordStop;
            button.onClick.AddListener(OnButtonPressed);

            // Find PetMove if not assigned
            if (petMove == null)
            {
                petMove = FindObjectOfType<PetMove>();
            }

            foreach (string device in Microphone.devices)
            {
                Debug.Log("Device Name: " + device);
            }
        }

        private void OnButtonPressed()
        {
            if (!microphoneRecord.IsRecording)
            {
                _stream.StartStream();
                microphoneRecord.StartRecord();
            }

            else
            {
                microphoneRecord.StopRecord();
            }
            buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Start";
        }

        private void OnRecordStop(AudioChunk recordedAudio) => buttonText.text = "Start";

        private void OnResult(string result) => text.text = result;

        private void OnSegmentFinished(WhisperResult segment)
        {
            string segmentText = segment.Result;
            commandText.text = segmentText;

            // Process voice command if pet is available
            if (petMove != null && !string.IsNullOrWhiteSpace(segmentText))
            {
                ProcessVoiceCommand(segmentText.ToLower().Trim());
            }
        }

        private void ProcessVoiceCommand(string command)
        {
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

