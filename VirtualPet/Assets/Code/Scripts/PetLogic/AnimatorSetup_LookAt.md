# Pet Look-At Feature: Animator Setup Guide

This guide provides step-by-step instructions for configuring the Unity Animator to support the pet's "look at user" behavior when entering focus mode.

## Overview

The look-at feature uses the existing `turnVelocity` parameter to smoothly rotate the pet's body toward the user. This approach leverages your current animation blend tree system without requiring complex IK setup.

## Prerequisites

- Existing pet Animator Controller with `turnVelocity` parameter
- Current animation blend tree for movement/turning
- Pet model with proper pivot point at base/feet

## Required Animator Parameters

### Existing Parameters (Already Configured)
- `turnVelocity` (float): Controls body rotation speed and direction
- `moveSpeed` (float): Controls movement blend tree
- `currentState` (int): Current behavior state

### New Parameters to Add
1. **lookAtSpeed** (float)
   - **Range**: 0.0 to 5.0
   - **Default**: 2.0
   - **Purpose**: Controls how fast the pet rotates toward the user

2. **lookAtDuration** (float) - *Optional*
   - **Range**: 0.0 to 10.0  
   - **Default**: 3.0
   - **Purpose**: How long to maintain gaze before returning to normal behavior

## Animator Controller Configuration

### Step 1: Add New Parameters

1. Open your pet's **Animator Controller**
2. In the **Parameters** tab, click the **+** button
3. Add the following parameters:

```
Name: lookAtSpeed
Type: Float
Default Value: 2.0
```

```
Name: lookAtDuration (Optional)
Type: Float  
Default Value: 3.0
```

### Step 2: Modify Existing Blend Tree (If Needed)

Your current setup likely has a blend tree for Idle/Movement states that uses `turnVelocity`. This should work as-is, but verify:

1. **Locate your Idle/Movement Blend Tree**
   - Usually in the "Idle" or "Locomotion" state
   - Should have `turnVelocity` as the blend parameter

2. **Verify Blend Tree Setup**:
   - **Turn Left Animation**: turnVelocity = -1.0
   - **Idle Animation**: turnVelocity = 0.0  
   - **Turn Right Animation**: turnVelocity = 1.0

3. **Test the Blend Tree**:
   - Select the pet in Scene view
   - In Animator window, adjust `turnVelocity` from -1 to 1
   - Pet should smoothly turn left/right

### Step 3: Animation Constraints (Optional Enhancement)

If you want to limit rotation speed for more realistic movement:

1. **Add Animation Curve Constraints**:
   - In blend tree, select turn animations
   - Adjust **Speed** multiplier (0.5-1.5 range)
   - This controls how fast the actual animation plays

2. **Damping Settings**:
   - In the main Animator Controller settings
   - Increase **Update Mode** to "Normal" 
   - Adjust **Culling Mode** to "Always Animate"

## Animation Setup Verification

### Test 1: Manual Parameter Control
1. Enter Play mode
2. Select pet GameObject
3. In Animator window, manually adjust `turnVelocity`:
   - Set to 1.0 → Pet should turn right
   - Set to -1.0 → Pet should turn left  
   - Set to 0.0 → Pet should stop turning

### Test 2: Code Integration Test
After implementing the code changes:
1. Enter VR mode or use Camera in Scene view
2. Press 'C' key to enter focus mode
3. Pet should smoothly rotate to face the camera
4. Verify smooth blending with existing animations

## Common Issues and Solutions

### Issue 1: Pet Turns Too Fast/Slow
**Solution**: Adjust `lookAtSpeed` parameter or animation speed multipliers in blend tree

### Issue 2: Pet Stutters When Turning  
**Solution**: 
- Check blend tree thresholds are properly spaced (-1, 0, 1)
- Ensure turn animations loop properly
- Verify `turnVelocity` parameter is being smoothly updated in code

### Issue 3: Pet Doesn't Return to Idle
**Solution**: Ensure `turnVelocity` gets reset to 0 after look-at duration expires

### Issue 4: Look-At Conflicts with Movement
**Solution**: The code should handle this automatically by only applying look-at when pet is not actively moving

## Animation Timing Guidelines

### Recommended Settings:
- **Look-At Rotation Speed**: 1.5-2.5 (natural but noticeable)
- **Look-At Duration**: 2-4 seconds (long enough to establish connection)
- **Fade-Out Time**: 0.5-1.0 seconds (smooth return to normal behavior)

### State-Specific Behavior:
- **Idle State**: Full rotation allowed (360°)
- **Sit State**: Limited rotation (±120°)  
- **Lying State**: Minimal rotation (±60°)
- **Moving State**: Look-at disabled (focus on movement)

## Advanced Configuration (Optional)

### Multiple Look-At Intensities
Add additional parameters for context-aware behavior:

```
lookAtIntensity (float, 0-1): Overall look-at strength
distanceMultiplier (float): Adjust behavior based on user distance  
```

### State-Dependent Look-At
Modify the blend tree to have different turn speeds based on current state:
- Add sub-blend trees for each major state
- Use `currentState` parameter to blend between them

## Code Integration Points

The following methods in `PetAnimationController.cs` will use these parameters:

```csharp
// Sets the target look direction and intensity
public void LookAtUser(Vector3 cameraPosition)

// Controls the duration of look-at behavior  
public void SetLookAtDuration(float duration)

// Resets to normal behavior
public void StopLookingAtUser()
```

## Troubleshooting Checklist

- [ ] `turnVelocity` parameter exists and is properly connected
- [ ] Blend tree has turn left/right animations at -1/+1 thresholds
- [ ] Pet model pivot point is at base/feet for proper rotation
- [ ] No conflicting animation layers or states
- [ ] Look-at parameters added with correct names and ranges
- [ ] Test manual parameter adjustment works before code integration

## Performance Notes

This simple approach has minimal performance impact:
- Uses existing animation system
- No additional bone calculations
- No IK solver overhead
- Smooth integration with current behavior states

Once configured, the pet will naturally turn toward the user when focus mode is entered, creating engaging eye contact for VR interactions!