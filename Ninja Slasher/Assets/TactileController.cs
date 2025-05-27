using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TactileController : MonoBehaviour
{
    private InputController _tactileController;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        _tactileController = new InputController();

        _tactileController.Tactile.Touch.started += context => StartTouch(context);
        _tactileController.Tactile.Touch.canceled += context => EndTouch(context);
    }

    private void OnEnable()
    {
        _tactileController.Enable();
    }

    private void OnDisable()
    {
        _tactileController.Disable();
    }

    private void StartTouch(InputAction.CallbackContext context) 
    {
        Vector2 startPosition = _tactileController.Tactile.TouchPosition.ReadValue<Vector2>();

        Vector3 startTouch = mainCamera.ScreenToWorldPoint(startPosition);

        Debug.Log("StartPos: " + startTouch.ToString());
    }

    private void EndTouch(InputAction.CallbackContext context) 
    {
        Vector2 endPosition = _tactileController.Tactile.TouchPosition.ReadValue<Vector2>();
        Vector3 endTouch = mainCamera.ScreenToWorldPoint(endPosition);

        Debug.Log("EndPos: " + endTouch.ToString());
    }
}