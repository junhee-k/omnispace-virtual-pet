using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

/// <summary>
/// A simple and versatile camera controller script for Unity using the new Input System.
/// - Use W, A, S, D keys to move the camera forward, left, back, and right.
/// - Hold the right mouse button and move the mouse to look around.
/// - Use the mouse scroll wheel to zoom in and out.
/// Attach this script to your main camera GameObject.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The speed at which the camera moves.")]
    public float moveSpeed = 5.0f;

    [Header("Look Settings")]
    [Tooltip("The sensitivity of the mouse look.")]
    public float mouseSensitivity = 100.0f;

    [Header("Zoom Settings")]
    [Tooltip("The speed at which the camera zooms.")]
    public float zoomSpeed = 2.0f;
    [Tooltip("A multiplier to make scroll wheel zoom less sensitive.")]
    public float zoomSensitivityFactor = 0.1f;

    // Private variables to store the camera's rotation
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    void Start()
    {
        // Initialize rotation values from the camera's starting orientation
        Vector3 startRotation = transform.eulerAngles;
        rotationX = startRotation.y;
        rotationY = startRotation.x;
    }

    void Update()
    {
        // Ensure we have valid Keyboard and Mouse devices before proceeding
        if (Keyboard.current == null || Mouse.current == null)
        {
            return; // Exit if no keyboard or mouse is connected
        }

        // --- Camera Movement (WASD) ---
        Vector3 moveInput = Vector3.zero;
        if (Keyboard.current.wKey.isPressed)
        {
            moveInput.z += 1;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            moveInput.z -= 1;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            moveInput.x -= 1;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            moveInput.x += 1;
        }

        Vector3 moveDirection = transform.forward * moveInput.z + transform.right * moveInput.x;
        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;


        // --- Camera Look (Mouse) ---
        // Only look around if the right mouse button is held down
        if (Mouse.current.rightButton.isPressed)
        {
            // Get mouse movement input
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

            // Calculate rotation
            rotationX += mouseX;
            rotationY -= mouseY;

            // Clamp the vertical rotation to prevent flipping
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            // Apply rotation to the camera
            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }


        // --- Camera Zoom (Scroll Wheel) ---
        float scrollValue = Mouse.current.scroll.ReadValue().y;
        // The scroll value is often very large, so we multiply by a small factor
        Vector3 zoomDirection = transform.forward * scrollValue * zoomSpeed * zoomSensitivityFactor * Time.deltaTime;
        transform.position += zoomDirection;
    }
}
