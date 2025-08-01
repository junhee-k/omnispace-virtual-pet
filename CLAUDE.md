# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity-based Virtual Pet application for VR/AR platforms, primarily targeting Apple Vision Pro and other XR devices. The project supports multiple interaction paradigms including hand tracking, spatial touch, and traditional VR controllers.

## Build and Development Commands

### Unity Editor Operations
- Open project: Launch Unity Hub and open the `VirtualPet` folder
- Play in Editor: Use Unity's Play button or Cmd+P (Mac) / Ctrl+P (Windows)
- Build for visionOS: Use Build Settings → visionOS platform
- Build for other XR platforms: Switch platform in Build Settings (OpenXR supported)

### Testing and Validation
- NavMesh baking: Window → AI → Navigation → Bake (required for pet movement)
- XR simulation: Use XR Device Simulator in Play mode for testing without headset
- Hand tracking test: Use HandTrackTest scene for hand interaction debugging

## Architecture Overview

### Core Systems

**VR/XR Movement System (PetMoveVR.cs)**
- Advanced NavMesh pathfinding with jump capabilities across disconnected surfaces
- Supports proactive island analysis for complex navigation scenarios
- Handles mouse/touch input for pet movement commands
- Integrates with Unity's NavMesh Agent and Animator systems

**Pet Logic (PetLogic/)**
- `PetMove.cs`: Basic pet movement using spatial touch and pinch gestures for Vision Pro
- `PetSpawner.cs`: Handles pet instantiation and placement
- Uses Unity's Enhanced Touch system for Vision Pro spatial interactions

**Hand Tracking (HandTrack/)**
- `HandVisualizer.cs`: Real-time hand joint visualization for Vision Pro
- `PinchSpawn.cs` & `MoveObjectOnInput.cs`: Gesture-based object interaction
- Platform-specific compilation for visionOS hand tracking features

**AR Navigation (ARLogic/)**
- `ARNavMeshManager.cs` & `ARPlaneNavMesh.cs`: Dynamic NavMesh generation on AR planes
- Integrates with ARFoundation for plane detection and navigation setup

### Key Dependencies

**PolySpatial (Unity visionOS)**
- `com.unity.polyspatial.visionos`: Core visionOS integration
- `com.unity.polyspatial.xr`: Cross-platform XR functionality
- Enables mixed reality experiences with spatial computing

**XR Packages**
- `com.unity.xr.hands`: Hand tracking and gesture recognition
- `com.unity.xr.interaction.toolkit`: XR interaction framework
- `com.unity.xr.arfoundation`: AR plane detection and tracking
- `com.unity.xr.openxr`: OpenXR standard for VR/AR compatibility

**Navigation & AI**
- `com.unity.ai.navigation`: Advanced NavMesh generation and pathfinding

### Scene Structure

**Main Scenes**
- `VRLivingRoom.unity`: Primary VR experience scene with living room environment
- `NavMeshTest.unity`: Testing environment for navigation mechanics
- `HandTrackTest.unity`: Hand tracking and interaction testing scene

**Prefabs**
- `BrittanySpaniel.prefab`: Main pet character with animations and NavMesh Agent
- `JointVisuals.prefab`: Hand joint visualization components
- Various treat and interaction objects for pet gameplay

### Rendering Pipeline
- Uses Universal Render Pipeline (URP) for optimal XR performance
- Configured for both mixed reality (visionOS) and immersive VR experiences
- Supports foveated rendering and other XR-specific optimizations

## Development Guidelines

**XR Platform Considerations**
- Test on visionOS simulator or device for spatial computing features
- Use XR Device Simulator for development without headset
- Ensure NavMesh is properly baked before testing pet movement
- Hand tracking features require visionOS platform or editor compilation

**Code Architecture**
- Pet behaviors extend MonoBehaviour and integrate with Unity's component system
- Platform-specific code uses conditional compilation (#if UNITY_VISIONOS)
- NavMesh agents require baked navigation data for proper pathfinding

**Performance Optimization**
- URP pipeline configured for XR performance
- Hand tracking uses efficient joint caching and update patterns
- NavMesh pathfinding includes optimization for complex multi-island scenarios