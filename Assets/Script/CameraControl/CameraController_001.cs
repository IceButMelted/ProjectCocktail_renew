using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CameraController_001 : MonoBehaviour
{
    Camera mainCamera;
    Vector3 mousePos;

    [Header("Camera Control")]
    [SerializeField]
    bool CanTurnSide = false;
    [SerializeField]
    bool CanMoveCamera = false;

    [Header("Transition Threshold")]
    [SerializeField]
    [Range(10, 70)]
    float TolookingSide = 30; //percentage of screen width
    [SerializeField]
    [Range(10, 70)]
    float BackFormSide = 40; // percentage of screen width
    [SerializeField]
    [Range(10, 50)]
    float TolookingDown = 20; // percentage of screen height
    [SerializeField]
    [Range(10, 80)]
    float BackFromDown = 30; // percentage of screen height
    [Space(5)]

    [Header("Camera Angle")]
    [SerializeField]
    Vector3 LookForwardAngle = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 LookSideAngle = new Vector3(0, 90, 0);
    [SerializeField]
    Vector3 LookDownAngle = new Vector3(45, 0, 0);
    [SerializeField]
    float rotateDuration = 0.5f;
    [SerializeField]
    float hoverDelayBeforeRotate = 0.6f;

    bool IsRotateCamera = false;
    Quaternion _targetRotation;
    float rotateProgress = 0f;
    Quaternion startRotation;
    [Space(5)]

    [Header("Camera Position")]
    [SerializeField]
    float moveDownDistance = 1f;

    Vector3 InitPosition;
    Vector3 DownPosition;
    float moveDuration = 0.6f;
    Vector3 _targetPosition;
    float moveProgress = 0f;
    Vector3 startPosition;
    bool IsMoveCamera = false;



    // Hover delay system
    bool isHovering = false;
    float hoverTime = 0f;

    // Looking Properties
    LookingDirection pendingDirection = LookingDirection.Forward;
    private LookingDirection _lookDirection = LookingDirection.Forward;
    public LookingDirection CurrentLookDirection
    {
        get
        {
            return _lookDirection;
        }
        private set
        {
            _lookDirection = value;
        }
    }
    public enum LookingDirection
    {
        Forward,
        Right,
        Left,
        Down,
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        InitPosition = transform.position;
        DownPosition = transform.position - new Vector3(0, moveDownDistance, 0);
    }

    void Start()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.localEulerAngles = LookForwardAngle;
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        mousePos = Mouse.current.position.ReadValue();

        if (IsRotateCamera)
        {
            RotateCamera();
        }
        else
        {
            UpdateHoverDelay();
            CheckTransitions();
        }
        if (IsMoveCamera)
        {
            MoveCamera();
        }
    }

    void UpdateHoverDelay()
    {
        if (isHovering)
        {
            hoverTime += Time.deltaTime;

            if (hoverTime >= hoverDelayBeforeRotate)
            {
                // Hover delay complete, start rotation
                StartRotation(pendingDirection);
                StartTranslation(pendingDirection);
                isHovering = false;
                hoverTime = 0f;
            }
        }
    }

    void ResetHoverDelay()
    {
        isHovering = false;
        hoverTime = 0f;
    }

    void StartHoverDelay(LookingDirection direction)
    {
        if (!isHovering || pendingDirection != direction)
        {
            isHovering = true;
            hoverTime = 0f;
            pendingDirection = direction;
        }
    }

    void CheckTransitions()
    {
        switch (CurrentLookDirection)
        {
            case LookingDirection.Forward:
                CheckForwardTransition();
                break;

            case LookingDirection.Left:
                if (!CanTurnSide)
                    break;
                CheckBackFromLeft();
                break;

            case LookingDirection.Right:
                if (!CanTurnSide)
                    break;
                CheckBackFromRight();
                break;

            case LookingDirection.Down:
                CheckBackFromDown();
                break;
        }
    }

    void CheckForwardTransition()
    {
        float rightEnter = Screen.width * (100 - TolookingSide) / 100f;
        float leftEnter = Screen.width * TolookingSide / 100f;
        float downEnter = Screen.height * TolookingDown / 100f;

        if (mousePos.x > rightEnter)
        {
            StartHoverDelay(LookingDirection.Right);
            Debug.Log("Hovering Right");
        }
        else if (mousePos.x < leftEnter)
        {
            StartHoverDelay(LookingDirection.Left);
            Debug.Log("Hovering Left");
        }
        else if (mousePos.y < downEnter)
        {
            StartHoverDelay(LookingDirection.Down);
            Debug.Log("Hovering Down");
        }
        else
        {
            // Mouse left the trigger zones, reset hover
            ResetHoverDelay();
        }
    }

    void CheckBackFromLeft()
    {
        float leftExit = Screen.width * BackFormSide / 100f;

        if (mousePos.x > leftExit)
        {
            StartHoverDelay(LookingDirection.Forward);
            Debug.Log("Hovering to Forward from Left");
        }
        else
        {
            ResetHoverDelay();
        }
    }

    void CheckBackFromRight()
    {
        float rightExit = Screen.width * (100 - BackFormSide) / 100f;

        if (mousePos.x < rightExit)
        {
            StartHoverDelay(LookingDirection.Forward);
            Debug.Log("Hovering to Forward from Right");
        }
        else
        {
            ResetHoverDelay();
        }
    }

    void CheckBackFromDown()
    {
        float downExit = Screen.height * BackFromDown / 100f;

        if (mousePos.y > downExit)
        {
            StartHoverDelay(LookingDirection.Forward);
            Debug.Log("Hovering to Forward from Down");
        }
        else
        {
            ResetHoverDelay();
        }
    }

    void StartRotation(LookingDirection newDirection)
    {
        CurrentLookDirection = newDirection;
        IsRotateCamera = true;
        rotateProgress = 0f;
        startRotation = mainCamera.transform.localRotation;

        // Reset hover system
        ResetHoverDelay();

        // Set target rotation based on direction
        Vector3 targetEuler = Vector3.zero;
        switch (newDirection)
        {
            case LookingDirection.Forward:
                targetEuler = LookForwardAngle;
                Debug.Log("Starting rotation to Forward");
                break;
            case LookingDirection.Left:
                targetEuler = new Vector3(LookSideAngle.x, -LookSideAngle.y, LookSideAngle.z);
                Debug.Log("Starting rotation to Left");
                break;
            case LookingDirection.Right:
                targetEuler = LookSideAngle;
                Debug.Log("Starting rotation to Right");
                break;
            case LookingDirection.Down:
                targetEuler = LookDownAngle;
                Debug.Log("Starting rotation to Down");
                break;
        }

        // Convert to quaternion - this automatically handles shortest path
        _targetRotation = Quaternion.Euler(targetEuler);
    }

    void StartTranslation(LookingDirection newDirection)
    {
        if (!CanMoveCamera) return; // Check if camera movement is enabled

        IsMoveCamera = true;
        moveProgress = 0f;
        startPosition = transform.position; 
        

        Vector3 targetPosition = Vector3.zero;
        switch (newDirection)
        {
            case LookingDirection.Forward:
                targetPosition = InitPosition;
                Debug.Log("Starting translation to Forward");
                break;
            case LookingDirection.Down:
                targetPosition = DownPosition;
                Debug.Log("Starting translation to Down");
                break;
            default:
                // For Left/Right, don't move camera
                IsMoveCamera = false;
                return;
        }

        _targetPosition = targetPosition;
    }

    void RotateCamera()
    {
        rotateProgress += Time.deltaTime / rotateDuration;

        if (rotateProgress >= 1f)
        {
            // Rotation complete
            mainCamera.transform.localRotation = _targetRotation;
            IsRotateCamera = false;
            rotateProgress = 0f;
        }
        else
        {
            // Smooth rotation using Slerp (Spherical Linear Interpolation)
            float smoothProgress = Mathf.SmoothStep(0f, 1f, rotateProgress);
            mainCamera.transform.localRotation = Quaternion.Slerp(startRotation, _targetRotation, smoothProgress);
        }
    }

    void MoveCamera()
    {
        moveProgress += Time.deltaTime / moveDuration;
        Debug.Log($"{moveProgress} MoveCamera");

        if (moveProgress >= 1f) 
        {
            mainCamera.transform.position = _targetPosition; 
            moveProgress = 0f;
            IsMoveCamera = false;
        }
        else
        {
            float smoothProgress = Mathf.SmoothStep(0f, 1f, moveProgress);
            mainCamera.transform.position = Vector3.Lerp(startPosition, _targetPosition, smoothProgress);
            float smoothStopZ = Mathf.SmoothStep(-1f, 1f, moveDuration);
        }
    }
}