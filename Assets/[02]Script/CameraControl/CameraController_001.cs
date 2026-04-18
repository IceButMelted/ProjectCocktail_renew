using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mouse-driven camera controller that smoothly transitions between different view angles
/// when the mouse hovers near screen edges. Supports left/right/down camera angles with
/// configurable transitions and optional camera translation.
///
/// Assign a CameraControllerSettings asset to the 'settings' field to configure all
/// parameters. If no asset is assigned, built-in defaults are used as a fallback.
/// </summary>
public class CameraController : MonoBehaviour
{
    #region Serialized Fields

    [Tooltip("ScriptableObject asset containing all camera controller settings. " +
             "Create one via: Assets > Create > Camera > Camera Controller Settings")]
    [SerializeField] private CameraControllerSettings settings;

    #endregion

    #region Settings Accessors (read from SO, fall back to defaults)

    private bool CanRotateSideways => settings != null ? settings.canRotateSideways : false;
    private bool CanMoveCamera => settings != null ? settings.canMoveCamera : false;
    private float SideTrigger => settings != null ? settings.sideViewTriggerThreshold : 30f;
    private float SideReturn => settings != null ? settings.sideViewReturnThreshold : 40f;
    private float DownTrigger => settings != null ? settings.downViewTriggerThreshold : 20f;
    private float DownReturn => settings != null ? settings.downViewReturnThreshold : 30f;
    private Vector3 ForwardAngle => settings != null ? settings.forwardAngle : Vector3.zero;
    private Vector3 LeftSideAngle => settings != null ? settings.leftSideAngle : new Vector3(0, -90, 0);
    private Vector3 RightSideAngle => settings != null ? settings.rightSideAngle : new Vector3(0, 90, 0);
    private Vector3 DownAngle => settings != null ? settings.downAngle : new Vector3(45, 0, 0);
    private float RotationDuration => settings != null ? settings.rotationDuration : 0.5f;
    private float HoverDelayDuration => settings != null ? settings.hoverDelayDuration : 0.6f;
    private float MovementDuration => settings != null ? settings.movementDuration : 0.6f;
    private float MoveDownDistance => settings != null ? settings.moveDownDistance : 1f;

    #endregion

    #region Private Fields

    // References
    private Camera mainCamera;

    // Position tracking
    private Vector3 initialPosition;
    private Vector3 downPosition;

    // Rotation state
    private bool isRotating = false;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float rotationProgress = 0f;

    // Movement state
    private bool isMoving = false;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float movementProgress = 0f;

    // Hover delay system
    private bool isHovering = false;
    private float hoverTimer = 0f;
    private ViewDirection pendingDirection = ViewDirection.Forward;

    // Current state
    private ViewDirection currentDirection = ViewDirection.Forward;

    #endregion

    #region Enums

    public enum ViewDirection
    {
        Forward,
        Left,
        Right,
        Down
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the current viewing direction of the camera.
    /// </summary>
    public ViewDirection CurrentDirection => currentDirection;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("CameraController: No main camera found in scene!");
            enabled = false;
            return;
        }

        if (settings == null)
            Debug.LogWarning("CameraController: No CameraControllerSettings asset assigned — using built-in defaults.");

        initialPosition = transform.position;
        downPosition = initialPosition - new Vector3(0, MoveDownDistance, 0);
    }

    private void Start()
    {
        mainCamera.transform.localRotation = Quaternion.Euler(ForwardAngle);
    }

    private void Update()
    {
        if (isRotating)
        {
            UpdateRotation();
        }
        else
        {
            UpdateHoverDelay();
            CheckForTransitions();
        }

        if (isMoving)
        {
            UpdateMovement();
        }
    }

    #endregion

    #region Hover Delay System

    private void UpdateHoverDelay()
    {
        if (!isHovering) return;

        hoverTimer += Time.deltaTime;

        if (hoverTimer >= HoverDelayDuration)
        {
            StartTransition(pendingDirection);
            ResetHoverDelay();
        }
    }

    private void StartHoverDelay(ViewDirection direction)
    {
        if (isHovering && pendingDirection != direction)
            ResetHoverDelay();

        if (!isHovering)
        {
            isHovering = true;
            hoverTimer = 0f;
            pendingDirection = direction;
        }
    }

    private void ResetHoverDelay()
    {
        isHovering = false;
        hoverTimer = 0f;
    }

    #endregion

    #region Transition Detection

    private void CheckForTransitions()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        switch (currentDirection)
        {
            case ViewDirection.Forward:
                CheckFromForward(mousePosition);
                break;

            case ViewDirection.Left:
                if (CanRotateSideways) CheckFromLeft(mousePosition);
                break;

            case ViewDirection.Right:
                if (CanRotateSideways) CheckFromRight(mousePosition);
                break;

            case ViewDirection.Down:
                CheckFromDown(mousePosition);
                break;
        }
    }

    private void CheckFromForward(Vector2 mousePos)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float rightTrigger = screenWidth * (100f - SideTrigger) / 100f;
        float leftTrigger = screenWidth * SideTrigger / 100f;
        float downTrigger = screenHeight * DownTrigger / 100f;

        if (CanRotateSideways && mousePos.x > rightTrigger)
            StartHoverDelay(ViewDirection.Right);
        else if (CanRotateSideways && mousePos.x < leftTrigger)
            StartHoverDelay(ViewDirection.Left);
        else if (mousePos.y < downTrigger)
            StartHoverDelay(ViewDirection.Down);
        else
            ResetHoverDelay();
    }

    private void CheckFromLeft(Vector2 mousePos)
    {
        float returnThreshold = Screen.width * SideReturn / 100f;

        if (mousePos.x > returnThreshold) StartHoverDelay(ViewDirection.Forward);
        else ResetHoverDelay();
    }

    private void CheckFromRight(Vector2 mousePos)
    {
        float returnThreshold = Screen.width * (100f - SideReturn) / 100f;

        if (mousePos.x < returnThreshold) StartHoverDelay(ViewDirection.Forward);
        else ResetHoverDelay();
    }

    private void CheckFromDown(Vector2 mousePos)
    {
        float returnThreshold = Screen.height * DownReturn / 100f;

        if (mousePos.y > returnThreshold) StartHoverDelay(ViewDirection.Forward);
        else ResetHoverDelay();
    }

    #endregion

    #region Transition Execution

    private void StartTransition(ViewDirection newDirection)
    {
        currentDirection = newDirection;
        StartRotation(newDirection);
        StartMovement(newDirection);
    }

    private void StartRotation(ViewDirection direction)
    {
        isRotating = true;
        rotationProgress = 0f;
        startRotation = mainCamera.transform.localRotation;

        Vector3 targetEuler = direction switch
        {
            ViewDirection.Forward => ForwardAngle,
            ViewDirection.Left => LeftSideAngle,
            ViewDirection.Right => RightSideAngle,
            ViewDirection.Down => DownAngle,
            _ => ForwardAngle
        };

        targetRotation = Quaternion.Euler(targetEuler);
    }

    private void StartMovement(ViewDirection direction)
    {
        if (!CanMoveCamera) return;
        if (direction != ViewDirection.Forward && direction != ViewDirection.Down) return;

        isMoving = true;
        movementProgress = 0f;
        startPosition = transform.position;
        targetPosition = direction == ViewDirection.Down ? downPosition : initialPosition;
    }

    #endregion

    #region Transition Updates

    private void UpdateRotation()
    {
        rotationProgress += Time.deltaTime / RotationDuration;

        if (rotationProgress >= 1f)
        {
            mainCamera.transform.localRotation = targetRotation;
            isRotating = false;
            rotationProgress = 0f;
        }
        else
        {
            float smoothed = Mathf.SmoothStep(0f, 1f, rotationProgress);
            mainCamera.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, smoothed);
        }
    }

    private void UpdateMovement()
    {
        movementProgress += Time.deltaTime / MovementDuration;

        if (movementProgress >= 1f)
        {
            transform.position = targetPosition;
            isMoving = false;
            movementProgress = 0f;
        }
        else
        {
            float smoothed = Mathf.SmoothStep(0f, 1f, movementProgress);
            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothed);
        }
    }

    #endregion

    #region Debug

    private void OnValidate()
    {
        if (settings == null)
            Debug.LogWarning("CameraController: No CameraControllerSettings asset assigned.");
    }

    #endregion
}