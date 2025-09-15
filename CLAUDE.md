# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity-based Virtual Pet application for VR/AR platforms, primarily targeting Apple Vision Pro and other XR devices. The project supports multiple interaction paradigms including hand tracking, spatial touch, and traditional VR controllers with sophisticated pet behavior simulation.

## Build and Development Commands

### Unity Editor Operations
- **Open project**: Launch Unity Hub and open the `VirtualPet` folder
- **Play in Editor**: Use Unity's Play button or Cmd+P (Mac) / Ctrl+P (Windows)
- **Build for visionOS**: File → Build Settings → visionOS platform → Build
- **Build for other XR platforms**: File → Build Settings → Switch platform (OpenXR supported)
- **Unity version**: 2022.3.46f1 (verify in ProjectSettings/ProjectVersion.txt)

### Essential Pre-Development Setup
- **NavMesh baking**: Window → AI → Navigation → Bake (CRITICAL for pet movement)
- **Async NavMesh baking**: Use SimpleNavMeshBaker component for non-blocking NavMesh generation
- **XR simulation**: Use XR Device Simulator in Play mode for testing without headset
- **Scene selection**: VRLivingRoom.unity for main testing, BakingTest.unity for NavMesh testing, HandTrackTest.unity for hand interaction debugging

### Testing and Debug Controls
- **Pet behavior testing**: Number keys 1-5 in Play mode to trigger different behaviors (Idle, Sit, Lying, Flat, Sleep)
- **Movement testing**: Left-click in scene view to make pet move to clicked location
- **Camera following**: C key to make pet follow camera with 0.5m forward offset distance and automatic look-at behavior
- **Look-at behavior**: Pet automatically looks at user when they take control (triggered by mouse input or C key)
- **LLM command testing**: Write JSON commands to `Assets/Code/Scripts/LLMCommands.json` for AI-driven behavior
- **Debug GUI**: Enable `showDebugLogs` in PetAnimationController, PetActionStateMachine, and LLMCommandExecutor for detailed logging

## Architecture Overview

### Advanced Pet Behavior System

**State Machine Architecture (PetBehavior namespace)**
- `PetActionStateMachine.cs`: BFS-based pathfinding between behavior states with graph representation
- `PetAnimationController.cs`: Sequential transition system with dynamic timing, animation integration, and look-at behavior
- Uses graph theory for valid state transitions: Idle ↔ Sit ↔ Lying ↔ Sleep/Flat
- Walk behavior integrated into Idle state via blend tree (no separate Walk state)
- Look-at system uses animation-driven turning with priority management to prevent movement conflicts

**Movement and Command System (PetLogic/)**
- `PetMoveVR.cs`: Advanced command queue system with LLM integration, NavMesh pathfinding, and camera following
- `PetMove.cs`: Enhanced touch input support for XR environments with camera following triggers
- `LLMCommandExecutor.cs`: File-based AI command processing with focus management (User vs LLM control)
- `LLMCommandLogger.cs`: Comprehensive logging system for debugging AI interactions
- `LLMCommandDebugger.cs`: Inspector-based debugging tools for command testing
- Automatic movement state detection and behavior synchronization
- Mouse/touch input handling with user-controlled behavior override
- Camera following with ground projection for VR headset compatibility and forward offset positioning
- User control triggers automatic look-at behavior for natural interaction

**Hand Tracking System (HandTrack/)**
- `HandVisualizer.cs`: Real-time hand joint visualization for Vision Pro
- `PinchSpawn.cs` & `MoveObjectOnInput.cs`: Gesture-based object interaction
- Platform-specific compilation with `#if UNITY_VISIONOS` directives
- Assembly definition: `Hands.asmdef` for isolated compilation

**AR Integration (ARLogic/)**
- `ARNavMeshManager.cs` & `ARPlaneNavMesh.cs`: Dynamic NavMesh generation on detected AR planes
- `SimpleNavMeshBaker.cs`: Async NavMesh baking system with debug visualization for development
- Integrates with ARFoundation for real-time plane detection and navigation setup

### Key Package Dependencies

**PolySpatial (Unity visionOS Integration)**
- `com.unity.polyspatial.visionos@1.3.13`: Core visionOS spatial computing
- `com.unity.polyspatial.xr@1.3.13`: Cross-platform XR functionality
- Enables mixed reality experiences with spatial computing capabilities

**XR Foundation Packages**
- `com.unity.xr.hands@1.5.1`: Hand tracking and gesture recognition
- `com.unity.xr.interaction.toolkit@2.6.4`: XR interaction framework
- `com.unity.xr.arfoundation@5.1.6`: AR plane detection and tracking
- `com.unity.xr.openxr@1.12.1`: OpenXR standard compliance
- `com.unity.xr.visionos@1.3.13`: Apple Vision Pro integration

**Core Unity Systems**
- `com.unity.ai.navigation@1.1.7`: Advanced NavMesh generation and pathfinding
- `com.unity.render-pipelines.universal@14.0.11`: URP for XR performance optimization

### Scene and Asset Structure

**Primary Scenes**
- `VRLivingRoom.unity`: Main VR experience with living room environment
- `NavMeshTest.unity`: Navigation system testing and debugging
- `BakingTest.unity`: Async NavMesh baking testing with debug visualization
- `HandTrackTest.unity`: Hand tracking interaction testing
- `MixedReality.unity`: Primary build scene (configured in EditorBuildSettings)

**Key Prefabs**
- `BrittanySpaniel.prefab`: Main pet with NavMesh Agent, behavior components, and animations
- `JointVisuals.prefab`: Hand joint visualization components
- Pet interaction objects in `/Levels/Prefabs/` (treats, toys, etc.)

### Code Architecture Patterns

**Component Composition**
- Pet entities use composition of PetActionStateMachine + PetAnimationController + PetMoveVR
- LLM integration via LLMCommandExecutor with automatic focus management
- Command pattern implementation for queued behavior execution
- Event-driven architecture with Action delegates for loose coupling

**Platform Abstraction**
- Conditional compilation (`#if UNITY_VISIONOS`) for platform-specific features
- Separate assembly definitions for modular compilation (HandTrack assembly)
- Universal input handling supporting mouse, touch, and XR controllers

**State Management**
- Graph-based state transitions with BFS pathfinding for complex behavior chains
- Command queue system for behavior sequencing and priority handling
- LLM-driven behavior with automatic User/AI focus switching
- Dynamic animation timing system for responsive state transitions

## Development Guidelines

**Critical Development Requirements**
- Always bake NavMesh before testing pet movement (Window → AI → Navigation → Bake)
- Test behavior transitions using number keys 1-5 in Play mode
- Test camera following using C key (exits LLM control, follows camera with 0.5m forward offset and triggers look-at)
- Test look-at behavior: pet should automatically turn toward user when they take control (mouse input or C key)
- Use XR Device Simulator for development without physical headset
- Enable debug logging in PetAnimationController, PetActionStateMachine, and LLMCommandExecutor
- Ensure LLMCommandExecutor is present in scene for AI command processing
- Verify Animator Controller has `turnVelocity` parameter properly configured for look-at behavior

**Code Integration Patterns**
- Extend behavior system by adding states to PetActionState enum and updating state graph in PetActionStateMachine
- Use command pattern for new behaviors: create classes inheriting from PetCommand
- Integrate with existing event system (OnStateChanged, OnTransitionStarted, etc.)
- Follow existing namespace structure (PetBehavior for state machine components)
- LLM commands use JSON format: `{"action":"sit","duration":5.0,"target":"floor","speed":"walk"}`

**Platform-Specific Development**
- Hand tracking features require visionOS platform targeting or editor simulation
- Use conditional compilation for platform-specific code (`#if UNITY_VISIONOS`)
- Test mixed reality features on MixedReality.unity scene
- Verify XR settings in ProjectSettings for target platform compatibility

**Performance Considerations**
- Command queue system prevents overlapping behaviors and ensures smooth execution
- Dynamic animation timing system optimizes transition delays for responsiveness
- NavMesh pathfinding optimized for real-time navigation with smooth path updates to prevent jittering
- LLM command processing includes comprehensive completion tracking to prevent blocking
- Look-at system uses existing animation blend tree for minimal performance impact
- Movement priority system prevents turn velocity conflicts between movement and look-at
- URP pipeline configured for XR performance requirements
- Hand tracking uses efficient joint caching and update patterns

## LLM Integration System

### Command Processing Architecture
- **File-based Communication**: Monitor `Assets/Code/Scripts/LLMCommands.json` for external AI commands
- **Focus Management**: Automatic switching between User Control (manual input) and LLM Control (AI-driven)
- **Command Queue**: Asynchronous processing with completion tracking using reflection
- **Timeout System**: Returns control to LLM after 10 seconds of user inactivity

### Supported LLM Commands
```json
{"action":"idle"}                                    // Transition to idle state
{"action":"sit","duration":5.0}                     // Sit for 5 seconds
{"action":"lying"}                                   // Transition to lying state
{"action":"flat"}                                    // Transition to flat state
{"action":"sleep","duration":10.0}                  // Sleep for 10 seconds
{"action":"walk","target":"floor","speed":"walk"}   // Walk to random floor location
{"action":"run","target":"floor","speed":"run"}     // Run to random floor location
```

### LLM Development Workflow
1. Write commands to `LLMCommands.json` file (automatically monitored every 0.5s)
2. Commands are parsed and executed if LLM has control focus
3. Comprehensive logging available in `Assets/Code/Scripts/Logs/LLMCommandLog.json`
4. Use LLMCommandDebugger component for Inspector-based testing and debugging
5. Monitor console logs with `[LLM]` prefix for detailed execution tracking

## Camera Following and Look-At System

### Camera Following Features
- **Forward Offset Following**: Pet maintains 0.5m distance in front of camera for comfortable viewing
- **Ground Projection**: Camera X/Z coordinates projected to NavMesh surface for VR headset compatibility
- **Smooth Path Updates**: Uses `NavMesh.CalculatePath()` and `SetPath()` to prevent animation jittering
- **Automatic Look-At**: Pet automatically looks at user when they take control (mouse input or C key)

### Look-At Behavior Implementation
- **Animation-Driven**: Uses existing `turnVelocity` parameter and blend tree for natural turning
- **Priority System**: Movement turn velocity only applies when not looking at user
- **Smooth Deceleration**: Pet slows rotation as it approaches target direction
- **Ground-Plane Calculation**: Reliable direction finding using Unity Quaternion.LookRotation()
- **Duration-Based**: Configurable look-at duration with automatic return to normal behavior

### Animator Setup Requirements
For look-at behavior to work properly, ensure your Animator Controller has:
- `turnVelocity` (float): Controls body rotation in blend tree (-1 = left, 0 = idle, 1 = right)
- `moveSpeed` (float): Controls movement animations
- `currentState` (int): Current behavior state
- Blend tree configured for turn left/idle/turn right animations at -1/0/1 thresholds

### Key Configuration Files
- `Assets/Code/Scripts/PetLogic/AnimatorSetup_LookAt.md`: Complete setup guide for Unity Animator
- `PetAnimationController.cs`: Look-at behavior configuration (lookAtSpeed, lookAtDuration, maxTurnVelocity)
- `PetMoveVR.cs`: Camera following and movement priority system

## Async NavMesh Baking System

### SimpleNavMeshBaker Component
- **Async Baking**: Non-blocking NavMesh generation using coroutines and Unity's async NavMesh APIs
- **Debug Visualization**: Transparent cyan overlay in Game View showing walkable NavMesh areas
- **Smart Build Logic**: Automatically detects first-time build vs update scenarios
- **UI Integration**: Button-driven interface for easy testing and development

### Key Features
- **First-time Building**: Uses `BuildNavMesh()` for initial NavMesh creation
- **Incremental Updates**: Uses `UpdateNavMesh()` for existing NavMesh modifications
- **Visual Debugging**: Creates "NavMesh_Debug_Global" GameObject with transparent cyan material
- **Performance**: UI remains responsive during baking operations

### Usage Instructions
1. **Setup**: Add `SimpleNavMeshBaker` component to any GameObject in scene
2. **Assign**: Reference a `NavMeshSurface` component in the inspector
3. **Configure**: Enable "Show NavMesh In Game View" for debug visualization
4. **Bake**: Call `BakeNavMesh()` method (UI button or code)
5. **Clear**: Use `ClearNavMesh()` to reset for testing
6. **Status**: Check `GetStatusInfo()` for current baking state

### Inspector Settings
- `showDebugLogs`: Enable detailed console logging for debugging
- `showNavMeshInGameView`: Toggle transparent cyan NavMesh visualization
- `navMeshSurface`: Reference to NavMeshSurface component for baking

### Test Scene
- **BakingTest.unity**: Complete test scene with UI button integration
- NavMeshBaker GameObject with configured SimpleNavMeshBaker component
- NavMesh Surface GameObject with appropriate settings for general use
- UI Canvas with "Bake NavMesh" button connected to BakeNavMesh() method

### Technical Implementation
- **Coroutine-based**: Uses `StartCoroutine()` for async behavior without blocking main thread
- **AsyncOperation handling**: Properly waits for Unity's async NavMesh operations
- **Error handling**: Comprehensive null checks and error reporting
- **Memory management**: Automatic cleanup of debug visualization objects
- **Unity 2022.3 compatible**: Uses modern Unity NavMesh APIs