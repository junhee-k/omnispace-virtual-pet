using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    // Update is called once per frame
    void Update()
    {
        if (mainCamera == null)
        {
            Debug.LogError("No camera tagged 'MainCamera' found in the scene. Please tag your main camera as 'MainCamera'.");
            return;
        }
    }
}
