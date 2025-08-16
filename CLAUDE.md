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
- **XR simulation**: Use XR Device Simulator in Play mode for testing without headset
- **Scene selection**: VRLivingRoom.unity for main testing, HandTrackTest.unity for hand interaction debugging

### Testing and Debug Controls
- **Pet behavior testing**: Number keys 1-5 in Play mode to trigger different behaviors (Idle, Sit, Lying, Flat, Sleep)
- **Movement testing**: Left-click in scene view to make pet move to clicked location
- **Camera following**: C key to make pet follow camera with automatic distance management (0.1m minimum)
- **LLM command testing**: Write JSON commands to `Assets/Code/Scripts/LLMCommands.json` for AI-driven behavior
- **Debug GUI**: Enable `showDebugLogs` in PetAnimationController, PetActionStateMachine, and LLMCommandExecutor for detailed logging

## Architecture Overview

### Advanced Pet Behavior System

**State Machine Architecture (PetBehavior namespace)**
- `PetActionStateMachine.cs`: BFS-based pathfinding between behavior states with graph representation
- `PetAnimationController.cs`: Sequential transition system with dynamic timing and animation integration
- Uses graph theory for valid state transitions: Idle ↔ Sit ↔ Lying ↔ Sleep/Flat
- Walk behavior integrated into Idle state via blend tree (no separate Walk state)

**Movement and Command System (PetLogic/)**
- `PetMoveVR.cs`: Advanced command queue system with LLM integration and NavMesh pathfinding
- `LLMCommandExecutor.cs`: File-based AI command processing with focus management (User vs LLM control)
- `LLMCommandLogger.cs`: Comprehensive logging system for debugging AI interactions
- `LLMCommandDebugger.cs`: Inspector-based debugging tools for command testing
- Automatic movement state detection and behavior synchronization
- Mouse/touch input handling with user-controlled behavior override

**Hand Tracking System (HandTrack/)**
- `HandVisualizer.cs`: Real-time hand joint visualization for Vision Pro
- `PinchSpawn.cs` & `MoveObjectOnInput.cs`: Gesture-based object interaction
- Platform-specific compilation with `#if UNITY_VISIONOS` directives
- Assembly definition: `Hands.asmdef` for isolated compilation

**AR Integration (ARLogic/)**
- `ARNavMeshManager.cs` & `ARPlaneNavMesh.cs`: Dynamic NavMesh generation on detected AR planes
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
- Test camera following using C key (exits LLM control, follows camera with distance management)
- Use XR Device Simulator for development without physical headset
- Enable debug logging in PetAnimationController, PetActionStateMachine, and LLMCommandExecutor
- Ensure LLMCommandExecutor is present in scene for AI command processing

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
- NavMesh pathfinding optimized for real-time navigation
- LLM command processing includes comprehensive completion tracking to prevent blocking
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