# LLM Command Integration for Virtual Pet

This system allows the virtual pet to receive real-time commands from Large Language Models (LLMs) via file-based communication, while maintaining hybrid control with user input.

## Features

- **File-based LLM Commands**: Monitor and execute commands from `LLMCommands.txt`
- **Hybrid Control System**: Seamless switching between user and LLM control
- **Focus Management**: User input automatically takes priority over LLM commands
- **Command Queue System**: Sequential execution of multiple commands
- **Visual Feedback**: Real-time indicator showing current control state
- **Movement Controls**: Walk/Run speeds with random floor movement
- **Duration-based Actions**: Timed behavior states (Sit, Lying, Sleep, etc.)

## Setup Instructions

### 1. Add Components to Scene

In Unity, add these components:

**To your pet GameObject:**
1. **PetMoveVR** - Already exists, now extended with LLM integration

**To any GameObject in scene (recommend creating an empty "LLMManager" GameObject):**
1. **LLMCommandExecutor** - Handles file watching, command parsing, and focus management
2. **LLMCommandLogger** - Tracks and logs all command execution (optional but recommended)

### 2. Configure Inspector Settings

**LLMCommandExecutor:**
- `Command File Name`: "LLMCommands.json" (default)
- `File Check Interval`: 0.5 seconds (default)
- `User Timeout Seconds`: 10 (time before switching back to LLM control)
- `Focus Indicator Text`: Assign a UI Text component for visual feedback
- `Show Debug Logs`: Enable for testing

**LLMCommandLogger (Optional):**
- `Enable Logging`: True (to track commands)
- `Log To File`: True (saves to JSON file)
- `Log To Console`: True (Unity console output)
- `Max Log Entries`: 1000 (prevents file bloat)
- `Show In Inspector`: True (displays recent logs in Inspector)

**PetMoveVR:**
- `Walk Speed`: 0.5 (default)
- `Run Speed`: 4.0 (default) 
- `Max Random Movement Distance`: 5.0 (default)

### 3. UI Setup (Optional)

Create a UI Text element to display the focus state:
1. Right-click in Hierarchy → UI → Text
2. Position it in a corner of the screen
3. Assign this Text component to the LLMCommandExecutor's `Focus Indicator Text` field

## Unity Editor Setup Guide

### Step 1: Prepare Your Scene

**Ensure NavMesh is Baked (CRITICAL for movement commands):**
1. Select your floor/ground objects in the scene
2. In Inspector → Navigation → **check "Navigation Static"**
3. Go to **Window → AI → Navigation**
4. In Navigation window → **Bake tab → Click "Bake"**
5. You should see blue overlay on walkable areas

### Step 2: Add Components to Pet GameObject

**Find your pet GameObject** (the one with PetMoveVR component):
1. Select pet in Hierarchy
2. Verify it has these components:
   - ✅ **PetMoveVR** (should already exist)
   - ✅ **NavMeshAgent** (should already exist)  
   - ✅ **PetActionStateMachine** (should already exist)
   - ✅ **PetAnimationController** (should already exist)

**Configure PetMoveVR LLM settings:**
- **Walk Speed**: 0.5
- **Run Speed**: 4.0  
- **Max Random Movement Distance**: 5.0
- **Show Debug Logs**: ✅ Enable for testing

### Step 3: Create LLM Manager GameObject

**Create new GameObject for LLM system:**
1. Right-click in Hierarchy → **Create Empty**
2. Rename to **"LLMManager"**
3. **Add Component** → Search "LLMCommandExecutor"
4. **Add Component** → Search "LLMCommandLogger" (optional but recommended)

### Step 4: Configure LLMCommandExecutor

**In LLMManager GameObject → LLMCommandExecutor component:**

**File Configuration:**
- **Command File Name**: "LLMCommands.json" 
- **File Check Interval**: 0.5

**Focus Configuration:**
- **User Timeout Seconds**: 10.0
- **Focus Indicator Text**: Drag UI Text here (if created)

**Debug:**
- **Show Debug Logs**: ✅ Enable

### Step 5: Configure LLMCommandLogger (Optional)

**In LLMManager GameObject → LLMCommandLogger component:**

**Logging Configuration:**
- **Enable Logging**: ✅ True
- **Log To File**: ✅ True  
- **Log To Console**: ✅ True
- **Max Log Entries**: 1000

**Log Display:**
- **Show In Inspector**: ✅ True (to see logs in Inspector)

### Step 6: Create Focus State UI (Optional)

**Add visual indicator for focus state:**
1. Right-click Hierarchy → **UI → Text**
2. Rename to **"FocusIndicator"**
3. Position in corner of screen (e.g., top-left)
4. **Set Text properties:**
   - Text: "🤖 LLM Control"
   - Font Size: 24
   - Color: Cyan
5. **Link to LLMCommandExecutor:**
   - Select LLMManager
   - Drag FocusIndicator Text → **Focus Indicator Text** field

### Step 7: Test the Setup

**Verify everything works:**
1. **Enter Play Mode**
2. **Check Console** for initialization messages:
   ```
   LLMCommandExecutor initialized. File: [path]/LLMCommands.json
   LLMCommandLogger connected to LLMCommandExecutor
   ```
3. **Test user control**: Press keys 1-6 or click mouse
   - Focus indicator should turn green: "🎮 User Control"
   - After 10 seconds, should return to: "🤖 LLM Control"

### Step 8: Test LLM Commands

**Create test command file:**
1. Navigate to: `Assets/Code/Scripts/LLMCommands.json`
2. Edit with text editor, add:
   ```
   ACTION: Move, TARGET: {Floor}, TYPE: {Walk}
   ACTION: Sit, DURATION: 3
   ```
3. **Save file**
4. **Watch Unity Console** for command processing logs
5. **Pet should move randomly then sit for 3 seconds**

### Step 9: Monitor with Logger

**View logging information:**
1. **Select LLMManager** in Hierarchy
2. **LLMCommandLogger component** shows recent commands
3. **Right-click component** → "Show Log Summary" for statistics
4. **Log files saved** to: `Assets/Code/Scripts/Logs/`

## Troubleshooting Setup

### Pet Not Moving
- ✅ **NavMesh baked?** (blue areas visible in Scene view)
- ✅ **NavMeshAgent attached** to pet?
- ✅ **LLMCommandExecutor** showing "🤖 LLM Control"?

### Commands Not Processing  
- ✅ **File exists?** `Assets/Code/Scripts/LLMCommands.json`
- ✅ **Console showing** "Processing file commands"?
- ✅ **Debug logs enabled** on LLMCommandExecutor?

### Components Missing
- ✅ **LLMCommandExecutor** added to scene?
- ✅ **PetMoveVR** on pet GameObject?
- ✅ **All required scripts** in project?

## Command Format

### Movement Commands
```
ACTION: Move, TARGET: {Floor}, TYPE: {Walk}
ACTION: Move, TARGET: {Floor}, TYPE: {Run}
```

### Behavior Commands (with optional duration)
```
ACTION: Sit, DURATION: 5
ACTION: Lying, DURATION: 3
ACTION: Sleep
ACTION: Flat, DURATION: 2
ACTION: Idle
```

### Sample Command File
```
ACTION: Move, TARGET: {Floor}, TYPE: {Walk}
ACTION: Sit, DURATION: 3
ACTION: Move, TARGET: {Floor}, TYPE: {Run}
ACTION: Lying, DURATION: 5
ACTION: Sleep
```

## How It Works

### File Monitoring
- The system watches `VirtualPet/Assets/Code/Scripts/LLMCommands.json`
- File changes trigger automatic command processing
- File is cleared after successful processing

### Focus Management
- **LLM Control**: Default state, processes commands from file
- **User Control**: Triggered by keyboard (1-6 keys) or mouse clicks
- **Timeout**: Returns to LLM control after 10 seconds of no user input

### Visual Indicators
- 🤖 **LLM Control** (Cyan) - Processing LLM commands
- 🎮 **User Control** (Green) - User input detected
- ⏳ **Transitioning** (Yellow) - Switching states

### Command Execution
1. Commands are parsed and converted to internal command objects
2. Added to command queue for sequential execution
3. User input immediately clears the queue and takes priority
4. Commands respect the existing state machine and animation system

## Usage Examples

### For LLM Integration
Write commands to the monitored file:
```python
# Python example
with open("VirtualPet/Assets/Code/Scripts/LLMCommands.json", "w") as f:
    f.write("ACTION: Move, TARGET: {Floor}, TYPE: {Walk}\\n")
    f.write("ACTION: Sit, DURATION: 5\\n")
```

### For Testing
1. **Manual Testing**: Edit `LLMCommands.json` directly and save
2. **User Override**: Press 1-6 keys or click to take control
3. **Timeout Test**: Stop user input and wait for LLM control to resume

## Debug Information

Enable `Show Debug Logs` to see:
- File change detection
- Command parsing results  
- Focus state transitions
- Command execution progress
- Error messages for malformed commands

## Troubleshooting

### Commands Not Executing
- Check that file path exists: `Assets/Code/Scripts/LLMCommands.json`
- Verify focus state is "LLM Control" (cyan indicator)
- Enable debug logs to see parsing results
- Ensure NavMesh is baked for movement commands

### Focus Not Switching
- Check that FocusManager is attached to a GameObject
- Verify timeout settings are reasonable (>1 second)
- Test with keyboard keys 1-6 or mouse clicks

### File Not Detected
- File watcher monitors the exact path: `Assets/Code/Scripts/`
- File must be named exactly: `LLMCommands.json`
- Try restarting Play mode if file monitoring fails

### Movement Issues
- Ensure NavMesh is baked (Window → AI → Navigation → Bake)
- Check that pet has NavMeshAgent component
- Verify walk/run speeds are configured appropriately

## Integration Notes

- **Thread Safety**: File watching runs on background thread, commands processed on main thread
- **Performance**: Minimal overhead, file checked every 0.5 seconds when not processing
- **Compatibility**: Works alongside existing mouse/keyboard controls
- **Extensibility**: Easy to add new command types and behaviors

## Command Logging System

The **LLMCommandLogger** provides comprehensive tracking of all LLM command execution:

### Features
- **File Logging**: Saves detailed logs to `Assets/Code/Scripts/Logs/LLMCommandLog.json`
- **Console Logging**: Real-time output in Unity Console
- **Inspector Display**: Shows recent commands directly in Inspector
- **Performance Tracking**: Execution times and success rates
- **Focus State Tracking**: Logs when control switches between User/LLM

### Log Data Includes
- **Timestamp**: Precise execution time
- **Command Details**: Action, duration, target, speed
- **Execution Result**: Success/failure with error messages
- **Performance Metrics**: Execution time in milliseconds
- **Focus State**: Whether user or LLM was in control

### Usage
```csharp
// Get recent log entries
var recentLogs = logger.GetRecentEntries(20);

// Export to CSV for analysis
logger.ExportLogToCSV();

// Get success rate
float successRate = logger.SuccessRate;
```

### Inspector Context Menu
- **"Show Log Summary"**: Displays statistics in console
- **"Export to CSV"**: Creates spreadsheet-friendly export
- **"Clear All Logs"**: Resets all logging data

### Log File Location
`VirtualPet/Assets/Code/Scripts/Logs/LLMCommandLog.json`

## Future Enhancements

- Network-based command reception (TCP/HTTP)
- Additional movement targets (specific objects/locations)
- Command scheduling and timing
- Behavior interruption and blending
- Enhanced visual feedback and status displays
- Real-time dashboard for command monitoring