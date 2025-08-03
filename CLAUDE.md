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
- **Pet behavior testing**: Number keys 1-6 in Play mode to trigger different behaviors (Idle, Sit, Lying, Flat, Sleep, Walk)
- **Movement testing**: Left-click in scene view to make pet move to clicked location
- **Debug GUI**: Enable `showDebugGUI` in PetBehaviorManager for runtime state inspection

## Architecture Overview

### Advanced Pet Behavior System

**State Machine Architecture (PetBehavior namespace)**
- `PetActionStateMachine.cs`: BFS-based pathfinding between behavior states with graph representation
- `PetBehaviorManager.cs`: Comprehensive behavior orchestration with rule-based triggers, queuing, and auto-behaviors
- `PetAnimationController.cs`: Animation integration with state transitions
- Uses graph theory for valid state transitions: Idle ↔ Sit ↔ Lying ↔ Sleep/Flat, Walk ↔ Idle

**Movement and Navigation (3DTest/)**
- `PetMoveVR.cs`: Advanced NavMesh pathfinding with seamless behavior system integration
- Automatic movement state detection and behavior synchronization
- Mouse/touch input handling with user-controlled behavior override
- Integrates with Unity's NavMesh Agent and Animator systems

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
- Pet entities use composition of PetBehaviorManager + PetActionStateMachine + PetAnimationController
- Behavior system designed for extensibility with BehaviorRule configurations
- Event-driven architecture with Action delegates for loose coupling

**Platform Abstraction**
- Conditional compilation (`#if UNITY_VISIONOS`) for platform-specific features
- Separate assembly definitions for modular compilation (HandTrack assembly)
- Universal input handling supporting mouse, touch, and XR controllers

**State Management**
- Graph-based state transitions with BFS pathfinding for complex behavior chains
- Queue system for behavior sequencing and priority handling
- Auto-behavior system with configurable intervals and random triggers

## Development Guidelines

**Critical Development Requirements**
- Always bake NavMesh before testing pet movement (Window → AI → Navigation → Bake)
- Test behavior transitions using number keys 1-6 in Play mode
- Use XR Device Simulator for development without physical headset
- Enable debug GUI on PetBehaviorManager for behavior state monitoring

**Code Integration Patterns**
- Extend behavior system by adding states to PetActionState enum and updating state graph in PetActionStateMachine
- Use BehaviorRule system for condition-based behavior triggers
- Integrate with existing event system (OnStateChanged, OnBehaviorRequested, etc.)
- Follow existing namespace structure (PetBehavior for state machine components)

**Platform-Specific Development**
- Hand tracking features require visionOS platform targeting or editor simulation
- Use conditional compilation for platform-specific code (`#if UNITY_VISIONOS`)
- Test mixed reality features on MixedReality.unity scene
- Verify XR settings in ProjectSettings for target platform compatibility

**Performance Considerations**
- Behavior system includes built-in cooldowns and timeout handling
- NavMesh pathfinding optimized for real-time navigation
- URP pipeline configured for XR performance requirements
- Hand tracking uses efficient joint caching and update patterns