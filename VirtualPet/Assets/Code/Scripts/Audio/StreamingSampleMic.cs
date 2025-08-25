using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Whisper.Utils;


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
        [SerializeField] private WhisperStream _stream;
        private async void Start()
        {
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated += OnResult;
            microphoneRecord.OnRecordStop += OnRecordStop;
            button.onClick.AddListener(OnButtonPressed);

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
    }
}

