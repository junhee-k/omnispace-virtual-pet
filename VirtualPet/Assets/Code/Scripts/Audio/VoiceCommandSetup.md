# Voice Command Setup Guide

This guide explains how to set up and configure the Whisper speech recognition system for voice-controlled pet behaviors.

## Prerequisites

- Unity 2022.3.46f1 or newer
- Whisper Unity package (already added to manifest.json)
- Microphone access permissions
- Existing Virtual Pet project with PetMoveVR and LLMCommandExecutor

## Quick Setup

### 1. Add Voice Recognition Components

1. **Create Voice Recognition GameObject:**
   - In your main scene (VRLivingRoom.unity), create an empty GameObject
   - Name it "VoiceRecognition"
   - Add the following components:
     - `SpeechRecognitionManager`
     - `VoiceCommandProcessor`
     - `VoiceCommandTestHelper` (optional, for testing)

2. **Configure Component References:**
   - The components will automatically find `PetMoveVR` and `LLMCommandExecutor` in the scene
   - No manual assignment required if they exist in the scene

### 2. Create Voice Command Configuration (Optional)

1. **Create Configuration Asset:**
   - Right-click in Project window
   - Create → Pet → Voice Command Config
   - Name it "PetVoiceConfig"

2. **Customize Commands:**
   - Set your pet's name (default: "Buddy")
   - Modify command variations for each behavior
   - Adjust confidence threshold (0.7 recommended)

3. **Assign Configuration:**
   - Select the VoiceCommandProcessor component
   - Drag the config asset to the "Config Asset" field

### 3. Configure Microphone Permissions

#### For VR/AR Development:
- **Vision Pro**: Microphone permissions are handled automatically
- **OpenXR**: Ensure microphone capability is enabled in XR settings
- **Editor Testing**: Grant microphone permission when prompted

#### For Standalone Builds:
- Enable "Microphone Usage Description" in Player Settings
- Add appropriate platform-specific permissions

## Voice Commands Reference

### Pet Behavior Commands

| Voice Input | Pet Action | Variations |
|-------------|------------|------------|
| "sit" | Sit | "sit down", "please sit" |
| "lie down" | Lying | "down", "lay down" |
| "sleep" | Sleep | "go to sleep", "nap" |
| "flat" | Flat | "play dead", "dead" |
| "stand" | Idle | "get up", "up" |

### Follow Mode Commands

| Voice Input | Action | Variations |
|-------------|--------|------------|
| "Buddy" | Follow mode | (pet name - configurable) |
| "come here" | Follow mode | "follow me", "follow" |

## Testing and Debugging

### Using Test Helper (Recommended for Development)

1. **Keyboard Shortcuts:**
   - **F1**: Test "sit" command
   - **F2**: Test "lie down" command  
   - **F3**: Test "sleep" command
   - **F4**: Test "follow" command
   - **F5**: Test pet name command

2. **Debug UI:**
   - Enable "Show Debug UI" in VoiceCommandTestHelper
   - Shows voice recognition status in game view
   - Provides manual test buttons

3. **Context Menu Testing:**
   - Right-click VoiceCommandProcessor in Inspector
   - Use "Test [Command]" options for quick testing

### Console Debugging

Enable debug logging in components for detailed information:
- `SpeechRecognitionManager`: Shows recording and processing status
- `VoiceCommandProcessor`: Shows command recognition results
- `LLMCommandExecutor`: Shows focus switching and command execution

## Configuration Options

### Voice Recognition Settings

```csharp
// In VoiceCommandConfig or component inspector
public string petName = "Buddy";                    // Pet name for follow mode
public float confidenceThreshold = 0.7f;            // Recognition confidence (0.0-1.0)
public float speechTimeoutSeconds = 3.0f;           // Max speech input time
public bool enableDebugLogging = true;              // Console debug output
```

### Speech Recognition Settings

```csharp
// In SpeechRecognitionManager component
public float recordingLength = 5f;                  // Recording duration per cycle
public float silenceThreshold = 0.01f;              // Silence detection sensitivity
public int sampleRate = 16000;                      // Audio sample rate
```

## Integration with Existing Systems

### Focus Management Integration

Voice commands automatically:
- Switch to **User Control** mode when recognized
- Reset the 10-second timeout for returning to LLM control
- Work alongside keyboard/mouse input detection

### Command Queue Integration

Voice commands use the existing PetMoveVR command system:
- **State Commands**: Use `PetMoveVR.QueueStateCommand()`
- **Follow Commands**: Use private `QueueFollowCameraCommand()` method
- **Execution**: Processed through existing command queue system

## Troubleshooting

### Common Issues

1. **No Microphone Detected:**
   - Check microphone permissions in system settings
   - Verify microphone is connected and working
   - Check Unity console for initialization errors

2. **Voice Commands Not Recognized:**
   - Lower confidence threshold in configuration
   - Check microphone volume and background noise
   - Enable debug logging to see recognition attempts
   - Use test helper to verify command processing

3. **Pet Doesn't Respond to Commands:**
   - Verify PetMoveVR and LLMCommandExecutor are in scene
   - Check that components found each other (console logs)
   - Test with keyboard shortcuts (F1-F5) to isolate issue

4. **Performance Issues:**
   - Adjust recording length (shorter = more responsive, less accurate)
   - Increase silence threshold to reduce processing of quiet audio
   - Check that only one SpeechRecognitionManager exists in scene

### Debug Checklist

- [ ] Whisper Unity package imported correctly
- [ ] Microphone permissions granted
- [ ] VoiceRecognition GameObject with required components
- [ ] PetMoveVR and LLMCommandExecutor present in scene
- [ ] Console shows "Voice command processor found and connected"
- [ ] Debug logging enabled for troubleshooting

## Advanced Configuration

### Custom Command Variations

Edit the VoiceCommandConfig to add more command variations:

```csharp
public string[] sitCommands = {
    "sit", "sit down", "please sit", 
    "take a seat", "have a seat"
};
```

### Multi-Language Support

The system supports any language that Whisper can recognize. Simply configure commands in your target language:

```csharp
// Example: Spanish commands
public string[] sitCommands = {"siéntate", "sentado"};
public string petName = "Mascota";
```

### Custom Pet Names

Change the pet name anytime:
```csharp
voiceCommandProcessor.SetPetName("Fluffy");
```

## Performance Notes

- **VR/AR Optimization**: The system is optimized for continuous operation in VR/AR environments
- **Battery Impact**: Continuous microphone recording will impact battery life on mobile VR devices
- **Processing Load**: Whisper processing happens on separate threads to maintain frame rate
- **Memory Usage**: Audio buffers are recycled to minimize garbage collection

## Integration with LLM System

Voice commands work seamlessly with the existing LLM command system:
- Voice input automatically switches to User Control mode
- After 10 seconds of inactivity, control returns to LLM
- Voice commands have same priority as keyboard/mouse input
- All commands go through the existing command queue system

This ensures consistent behavior whether commands come from voice, keyboard, mouse, or LLM sources.