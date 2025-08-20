using System;
using UnityEngine;

namespace PetBehavior
{
    [Serializable]
    public class VoiceCommandConfig
    {
        [Header("Pet Configuration")]
        [Tooltip("The pet's name - saying this will trigger follow mode")]
        public string petName = "Buddy";
        
        [Header("Voice Commands for Pet States")]
        [Tooltip("Commands that will make the pet sit")]
        public string[] sitCommands = {"sit", "sit down", "please sit"};
        
        [Tooltip("Commands that will make the pet lie down")]
        public string[] lyingCommands = {"lie down", "lying", "lay down", "down"};
        
        [Tooltip("Commands that will make the pet sleep")]
        public string[] sleepCommands = {"sleep", "go to sleep", "nap"};
        
        [Tooltip("Commands that will make the pet go flat")]
        public string[] flatCommands = {"flat", "play dead", "dead"};
        
        [Tooltip("Commands that will make the pet return to idle state")]
        public string[] idleCommands = {"idle", "stand", "get up", "up"};
        
        [Header("Follow Mode Commands")]
        [Tooltip("Commands that will trigger follow mode")]
        public string[] followCommands = {"come here", "follow me", "follow", "come"};
        
        [Header("Recognition Settings")]
        [Tooltip("Minimum confidence score for command recognition (0.0 to 1.0)")]
        [Range(0.0f, 1.0f)]
        public float confidenceThreshold = 0.7f;
        
        [Tooltip("Maximum time to wait for speech input before timeout (seconds)")]
        public float speechTimeoutSeconds = 3.0f;
        
        [Tooltip("Enable debug logging for voice commands")]
        public bool enableDebugLogging = true;
    }
    
    [CreateAssetMenu(fileName = "VoiceCommandConfig", menuName = "Pet/Voice Command Config")]
    public class VoiceCommandConfigAsset : ScriptableObject
    {
        public VoiceCommandConfig config = new VoiceCommandConfig();
    }
}