# LLM Command Integration Implementation Plan

## Overview
Extend the existing command queue system to support LLM-driven pet actions via file watching, with user focus management and interruption capabilities.

## Core Components to Create

### 1. **LLMCommandProcessor Component**
- **File watching**: Monitor `Assets/Code/Scripts/LLMCommands.txt` using FileSystemWatcher
- **Command parsing**: Parse multi-line format (ACTION: X, DURATION: Y / TARGET: Z, TYPE: W)
- **Queue integration**: Convert parsed commands to existing PetCommand objects
- **File management**: Clear file after successful processing

### 2. **FocusManager Component** 
- **Focus state tracking**: Enum (UserControlled, LLMControlled, Transitioning)
- **Input detection**: Monitor for keyboard (1-6 keys) + mouse clicks
- **Timeout system**: Configurable timeout (default 10 seconds) to return to LLM control
- **Visual indicator**: Simple UI text showing current focus state

### 3. **New Command Classes**
- **LLMMovementCommand**: Extends MovementCommand with speed control (Walk: 0.5, Run: 4.0)
- **DurationBasedStateCommand**: Extends StateTransitionCommand with duration tracking
- **RandomMovementCommand**: Generates random NavMesh position within 5 units

### 4. **PetMoveVR Extensions**
- **Speed configuration**: Inspector fields for walk/run speeds
- **Focus integration**: Clear queue when user takes control  
- **Random position generation**: NavMesh sampling within radius
- **LLM command processing**: Integration point for LLM commands

## Integration Strategy
- **Hybrid control**: LLM commands processed when user not focused
- **Immediate interruption**: Clear command queue when user input detected
- **Seamless transition**: Existing keyboard/mouse controls unchanged
- **File-based communication**: Simple text file interface for LLM

## Implementation Order
1. Create FocusManager with basic user input detection
2. Add LLMCommandProcessor with file watching
3. Implement new command types and parsing
4. Integrate with existing PetMoveVR command queue
5. Add visual feedback and testing

## Technical Notes
- Leverages existing command queue architecture
- Maintains state machine and animation system compatibility
- FileSystemWatcher runs on separate thread, queues updates for main thread
- All parsing and validation happens before commands enter the queue