# Simplified LLM Commands Integration Guide

This guide shows how to integrate your LLM system with the Virtual Pet using the new **simplified** LLMCommandExecutor.

## What's New and Simplified

✅ **Single Component**: LLMCommandExecutor replaces both LLMCommandProcessor + FocusManager  
✅ **JSON Commands**: Simple JSON format instead of complex regex parsing  
✅ **Hybrid Support**: Both file-based and direct C# API calls  
✅ **Better Error Handling**: Clear success/failure feedback  
✅ **Backwards Compatible**: Old regex format still works  

## Quick Setup (Unity)

### 1. Add Component
Add `LLMCommandExecutor` component to your pet GameObject (replaces old LLMCommandProcessor + FocusManager).

### 2. Configure Inspector
```
Command File Name: "LLMCommands.txt"
User Timeout Seconds: 10
Focus Indicator Text: [Assign UI Text component]
Show Debug Logs: ✓ (for testing)
```

That's it! No complex multi-component setup.

## Integration Methods

### Method 1: File-Based (Easiest for most LLMs)

Your LLM writes JSON commands to `LLMCommands.txt`:

```json
{"action": "sit", "duration": 5.0}
{"action": "move", "target": "floor", "speed": "walk"}
{"action": "sleep"}
```

**Example Python Integration:**
```python
import json
import os

def send_pet_command(action, duration=0, target="", speed=""):
    command = {
        "action": action,
        "duration": duration,
        "target": target,
        "speed": speed
    }
    
    # Write to Unity's command file
    file_path = "/path/to/Unity/Assets/Code/Scripts/LLMCommands.txt"
    with open(file_path, "w") as f:
        json.dump(command, f)
    
    print(f"Sent command: {action}")

# Usage
send_pet_command("sit", duration=3.0)
send_pet_command("move", target="floor", speed="run")
send_pet_command("sleep")
```

### Method 2: Direct C# API (For Unity-integrated LLMs)

If your LLM runs in Unity or can call Unity methods directly:

```csharp
// Get reference to command executor
LLMCommandExecutor executor = FindObjectOfType<LLMCommandExecutor>();

// Execute commands directly
CommandResult result = executor.ExecuteCommand("sit", duration: 5.0f);
if (result.success) {
    Debug.Log("Command executed: " + result.message);
} else {
    Debug.LogError("Command failed: " + result.error);
}

// Or using LLMCommand object
var command = new LLMCommand {
    action = "move",
    target = "floor", 
    speed = "walk"
};
CommandResult result2 = executor.ExecuteCommand(command);
```

### Method 3: HTTP API (Future Extension)

The architecture is ready for HTTP integration. You can easily extend LLMCommandExecutor:

```csharp
// Add this method to LLMCommandExecutor for HTTP support
[HttpPost("/pet/command")]
public CommandResult HandleHTTPCommand(LLMCommand command) {
    return ExecuteCommand(command);
}
```

## Command Reference

### State Commands (No Movement)
```json
{"action": "idle"}                    // Return to idle state
{"action": "sit"}                     // Sit indefinitely  
{"action": "sit", "duration": 10}     // Sit for 10 seconds
{"action": "lying"}                   // Lie down
{"action": "lying", "duration": 5}    // Lie down for 5 seconds
{"action": "sleep"}                   // Go to sleep
{"action": "flat"}                    // Flat lying position
```

### Movement Commands
```json
{"action": "move", "target": "floor", "speed": "walk"}   // Walk randomly
{"action": "move", "target": "floor", "speed": "run"}    // Run randomly
```

## Focus System (User vs LLM Control)

The system automatically manages who controls the pet:

- **LLM Control** (default): Your commands are executed
- **User Control**: When user presses keys 1-6 or clicks mouse
- **Auto-timeout**: Returns to LLM control after 10 seconds of no user input

**LLM Integration Notes:**
- Commands are **ignored** while user is in control
- You get `CommandResult.success = false` with error message
- Can check current state: `executor.IsLLMControlled`
- Can force control: `executor.ForceLLMControl()` (use carefully)

## Error Handling

### File-Based Method
```python
import json
import time

def send_command_with_retry(action, **kwargs):
    command = {"action": action, **kwargs}
    
    try:
        with open(command_file, "w") as f:
            json.dump(command, f)
        print(f"✓ Sent: {action}")
        return True
    except Exception as e:
        print(f"✗ Failed: {e}")
        return False

# Retry logic
for attempt in range(3):
    if send_command_with_retry("sit", duration=5.0):
        break
    time.sleep(0.5)
```

### Direct API Method
```csharp
CommandResult result = executor.ExecuteCommand("sit", 5.0f);

if (!result.success) {
    if (result.error.Contains("User is currently in control")) {
        // Wait for user to finish, then retry
        StartCoroutine(RetryWhenLLMControlled());
    } else {
        Debug.LogError("Command error: " + result.error);
    }
}
```

## Testing Your Integration

### 1. Inspector Testing
- Use `Test JSON Command` context menu on LLMCommandExecutor
- Use `Test Legacy Command` for backwards compatibility testing

### 2. Debug Logging
Enable "Show Debug Logs" to see:
- Command parsing results
- Execution success/failure
- Focus state changes
- User input detection

### 3. Visual Feedback
The focus indicator shows:
- 🤖 **LLM Control** (cyan) - Your commands work
- 🎮 **User Control** (green) - Your commands ignored  
- ⏳ **Transitioning** (yellow) - Brief transition state

## Migration from Old System

If you have existing regex-based commands, they still work:

**Old Format (still supported):**
```
ACTION: sit, DURATION: 5.0
ACTION: move, TARGET: {floor}, TYPE: {walk}
```

**New Format (recommended):**
```json
{"action": "sit", "duration": 5.0}
{"action": "move", "target": "floor", "speed": "walk"}
```

## Common LLM Integration Patterns

### OpenAI/ChatGPT Function Calling
```python
def pet_command_function(action: str, duration: float = 0, target: str = "", speed: str = ""):
    """Send command to virtual pet"""
    command = {
        "action": action,
        "duration": duration,
        "target": target,
        "speed": speed
    }
    
    with open(COMMAND_FILE, "w") as f:
        json.dump(command, f)
    
    return f"Pet command sent: {action}"

# Register with OpenAI
functions = [{
    "name": "pet_command_function",
    "description": "Control virtual pet behavior",
    "parameters": {
        "type": "object",
        "properties": {
            "action": {"type": "string", "enum": ["sit", "sleep", "lying", "move", "idle"]},
            "duration": {"type": "number", "description": "Duration in seconds"},
            "target": {"type": "string", "enum": ["floor"]},
            "speed": {"type": "string", "enum": ["walk", "run"]}
        },
        "required": ["action"]
    }
}]
```

### Local LLM (Ollama/LlamaCpp)
```python
import ollama
import json

def control_pet_with_llm(user_input):
    prompt = f"""
    User says: "{user_input}"
    
    Control the virtual pet by responding with a JSON command:
    - State commands: {{"action": "sit"/"sleep"/"lying"/"idle", "duration": seconds}}
    - Movement: {{"action": "move", "target": "floor", "speed": "walk"/"run"}}
    
    Respond with only the JSON command:
    """
    
    response = ollama.chat(model='llama2', messages=[{
        'role': 'user',
        'content': prompt
    }])
    
    try:
        command = json.loads(response['message']['content'])
        with open(COMMAND_FILE, "w") as f:
            json.dump(command, f)
        return f"Pet will {command['action']}"
    except:
        return "Failed to parse LLM response"

# Usage
control_pet_with_llm("Make the pet sit down for a while")
```

## Benefits for Your Teammate

1. **60% Less Complexity**: Single component vs multiple components
2. **Standard JSON**: No regex parsing headaches  
3. **Immediate Feedback**: Know if commands succeed/fail
4. **Multiple Options**: Choose file-based, API, or future HTTP
5. **Easy Testing**: Built-in testing methods and debug logs
6. **Backwards Compatible**: Existing integrations keep working

Your teammate can start with the simple file-based JSON approach and upgrade to direct API calls when needed.