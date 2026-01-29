using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mouse-driven camera controller that smoothly transitions between different view angles
/// when the mouse hovers near screen edges. Supports left/right/down camera angles with
/// configurable transitions and optional camera translation.
/// </summary>
public class CameraController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Feature Toggles")]
    [Tooltip("Enable left/right camera rotation when hovering at screen edges")]
    [SerializeField] private bool canRotateSideways = false;

    [Tooltip("Enable camera position movement (vertical) when looking down")]
    [SerializeField] private bool canMoveCamera = false;

    [Header("Edge Detection Thresholds (%)")]
    [Tooltip("Distance from screen edge (%) to trigger side view")]
    [SerializeField][Range(5, 70)] private float sideViewTriggerThreshold = 30f;

    [Tooltip("Distance from screen edge (%) to return from side view")]
    [SerializeField][Range(5, 70)] private float sideViewReturnThreshold = 40f;

    [Tooltip("Distance from bottom (%) to trigger down view")]
    [SerializeField][Range(5, 50)] private float downViewTriggerThreshold = 20f;

    [Tooltip("Distance from bottom (%) to return from down view")]
    [SerializeField][Range(5, 80)] private float downViewReturnThreshold = 30f;

    [Header("Camera Rotation Angles")]
    [SerializeField] private Vector3 forwardAngle = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 sideAngle = new Vector3(0, 90, 0);
    [SerializeField] private Vector3 downAngle = new Vector3(45, 0, 0);

    [Header("Transition Settings")]
    [Tooltip("Time to complete rotation transition")]
    [SerializeField][Range(0.1f, 2f)] private float rotationDuration = 0.5f;

    [Tooltip("Time mouse must hover before triggering transition")]
    [SerializeField][Range(0.1f, 2f)] private float hoverDelayDuration = 0.6f;

    [Tooltip("Time to complete camera position movement")]
    [SerializeField][Range(0.1f, 2f)] private float movementDuration = 0.6f;

    [Header("Camera Translation")]
    [Tooltip("Distance to move camera down when looking down")]
    [SerializeField] private float moveDownDistance = 1f;

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
    /// Gets the current viewing direction of the camera
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

        // Store initial positions
        initialPosition = transform.position;
        downPosition = initialPosition - new Vector3(0, moveDownDistance, 0);
    }

    private void Start()
    {
        // Set initial camera angle
        mainCamera.transform.localRotation = Quaternion.Euler(forwardAngle);
    }

    private void Update()
    {
        // Update transitions
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

        if (hoverTimer >= hoverDelayDuration)
        {
            // Hover duration met - trigger transition
            StartTransition(pendingDirection);
            ResetHoverDelay();
        }
    }

    private void StartHoverDelay(ViewDirection direction)
    {
        // If already hovering toward a different direction, reset
        if (isHovering && pendingDirection != direction)
        {
            ResetHoverDelay();
        }

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
                if (canRotateSideways)
                    CheckFromLeft(mousePosition);
                break;

            case ViewDirection.Right:
                if (canRotateSideways)
                    CheckFromRight(mousePosition);
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

        // Calculate edge trigger positions
        float rightTrigger = screenWidth * (100f - sideViewTriggerThreshold) / 100f;
        float leftTrigger = screenWidth * sideViewTriggerThreshold / 100f;
        float downTrigger = screenHeight * downViewTriggerThreshold / 100f;

        // Check which edge the mouse is near
        if (canRotateSideways && mousePos.x > rightTrigger)
        {
            StartHoverDelay(ViewDirection.Right);
        }
        else if (canRotateSideways && mousePos.x < leftTrigger)
        {
            StartHoverDelay(ViewDirection.Left);
        }
        else if (mousePos.y < downTrigger)
        {
            StartHoverDelay(ViewDirection.Down);
        }
        else
        {
            // Mouse not in any trigger zone
            ResetHoverDelay();
        }
    }

    private void CheckFromLeft(Vector2 mousePos)
    {
        float returnThreshold = Screen.width * sideViewReturnThreshold / 100f;

        if (mousePos.x > returnThreshold)
        {
            StartHoverDelay(ViewDirection.Forward);
        }
        else
        {
            ResetHoverDelay();
        }
    }

    private void CheckFromRight(Vector2 mousePos)
    {
        float returnThreshold = Screen.width * (100f - sideViewReturnThreshold) / 100f;

        if (mousePos.x < returnThreshold)
        {
            StartHoverDelay(ViewDirection.Forward);
        }
        else
        {
            ResetHoverDelay();
        }
    }

    private void CheckFromDown(Vector2 mousePos)
    {
        float returnThreshold = Screen.height * downViewReturnThreshold / 100f;

        if (mousePos.y > returnThreshold)
        {
            StartHoverDelay(ViewDirection.Forward);
        }
        else
        {
            ResetHoverDelay();
        }
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

        // Determine target rotation based on direction
        Vector3 targetEuler = direction switch
        {
            ViewDirection.Forward => forwardAngle,
            ViewDirection.Left => new Vector3(sideAngle.x, -sideAngle.y, sideAngle.z),
            ViewDirection.Right => sideAngle,
            ViewDirection.Down => downAngle,
            _ => forwardAngle
        };

        targetRotation = Quaternion.Euler(targetEuler);
    }

    private void StartMovement(ViewDirection direction)
    {
        // Only move camera if feature is enabled
        if (!canMoveCamera) return;

        // Only move for specific directions
        if (direction != ViewDirection.Forward && direction != ViewDirection.Down)
            return;

        isMoving = true;
        movementProgress = 0f;
        startPosition = transform.position;

        targetPosition = direction == ViewDirection.Down ? downPosition : initialPosition;
    }

    #endregion

    #region Transition Updates

    private void UpdateRotation()
    {
        rotationProgress += Time.deltaTime / rotationDuration;

        if (rotationProgress >= 1f)
        {
            // Rotation complete
            mainCamera.transform.localRotation = targetRotation;
            isRotating = false;
            rotationProgress = 0f;
        }
        else
        {
            // Smooth rotation using spherical interpolation
            float smoothed = Mathf.SmoothStep(0f, 1f, rotationProgress);
            mainCamera.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, smoothed);
        }
    }

    private void UpdateMovement()
    {
        movementProgress += Time.deltaTime / movementDuration;

        if (movementProgress >= 1f)
        {
            // Movement complete
            transform.position = targetPosition;
            isMoving = false;
            movementProgress = 0f;
        }
        else
        {
            // Smooth movement using linear interpolation
            float smoothed = Mathf.SmoothStep(0f, 1f, movementProgress);
            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothed);
        }
    }

    #endregion

    #region Debug Visualization

    private void OnValidate()
    {
        // Ensure return thresholds are greater than trigger thresholds
        if (sideViewReturnThreshold <= sideViewTriggerThreshold)
        {
            Debug.LogWarning("CameraController: Return threshold should be greater than trigger threshold to prevent flickering!");
        }

        if (downViewReturnThreshold <= downViewTriggerThreshold)
        {
            Debug.LogWarning("CameraController: Return threshold should be greater than trigger threshold to prevent flickering!");
        }
    }

    #endregion
}