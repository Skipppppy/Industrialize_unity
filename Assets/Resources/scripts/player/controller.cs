using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform checkGround;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] public PlayerInput playerInput;
    [SerializeField] public CursorManager cursorlogic;
    [SerializeField] private voxelworld planet;

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _jumpHeight = 1f;
    [SerializeField] private float _planetgravity = 25f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;

    [Header("Mouse")]
    [SerializeField] private float _gameSensitivity = 0.2f;
    [SerializeField] private float _uiSensitivity = 0.03f;
    [Header("Camera Effects")]
    [SerializeField] private float bobSpeed = 7f;
    [SerializeField] public float horizontalDistance = 0.015f;
    [SerializeField] public float verticalDistance = 0.03f;
    [Header("Camera Effects")]
    [SerializeField] private float _bobSpeed = 5f;
    [SerializeField] private float _bobPitchAmount = 0.5f;
    [SerializeField] private float _bobRollAmount = 0.7f;
    [SerializeField] private float _bobPositionAmount = 0.004f;
    [SerializeField] private float _bobSmooth = 6f;
    [SerializeField] private float _swayAmount = 0.6f;
    [SerializeField] private float _swaySmooth = 8f;
    [SerializeField] private float _runBobMultiplier = 1.2f;

    private float _bobWeight;
    private float _bobTime;
    private float _bobPitch;
    private float _bobRoll;
    private float _swayRoll;
    private Vector3 _defaultCameraPosition;
    private float currentRoll;
    private float bobWeight;
    private float bobPitch;
    private float bobRoll;
    private float bobTime;
    private Vector3 defaultCameraPosition;

    [Header("Ground")]
    [SerializeField] private float _groundTolerance = 0.15f;
    [SerializeField] private float _surfaceOffset = 0.08f;

    [Header("Planet Rotation")]
    [SerializeField] private float _planetRotationSpeed = 20f;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _runAction;
    [SerializeField] private planetgravity _gravitybody;
    private Vector3 currentMove;
    private Vector3 moveVelocity;

    private float rotationX;
    private float currentSpeed;

    public Transform cameraHolder;

    private Vector3 planetUp
    {
        get
        {
            if (_gravitybody == null)
                return transform.up;

            return _gravitybody.up;
        }
    }

    private void Awake()
    {
        _moveAction = playerInput.actions["Move"];
        _lookAction = playerInput.actions["Look"];
        _jumpAction = playerInput.actions["Jump"];
        _runAction = playerInput.actions["Sprint"];

        if (planet == null)
            planet = FindFirstObjectByType<voxelworld>();
        if (_gravitybody == null)
            _gravitybody =
                GetComponent<planetgravity>();
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _lookAction.Enable();
        _jumpAction.Enable();
        _runAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _lookAction.Disable();
        _jumpAction.Disable();
        _runAction.Disable();
    }

    private void Start()
    {
        _defaultCameraPosition = cameraTransform.localPosition;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        alignwithplanet(true);
    }

    private void Update()
    {
        Rotate();
        Move();
        ApplyGravity();
        alignwithplanet(false);
        CameraSway();
        HeadBob();
        ApplyCameraRotation();
    }

    private void Rotate()
    {
        Vector2 _look =
            _lookAction.ReadValue<Vector2>();

        float _sensitivity =
            cursorlogic.uiMode
            ? _uiSensitivity
            : _gameSensitivity;

        rotationX -=
            _look.y *
            _sensitivity;

        rotationX =
            Mathf.Clamp(
                rotationX,
                -80f,
                80f
            );

        Vector3 _up =
            planetUp;

        Vector3 _forward =
            Vector3.ProjectOnPlane(
                transform.forward,
                _up
            );

        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward =
                Vector3.ProjectOnPlane(
                    cameraTransform.forward,
                    _up
                );
        }

        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
                    _up
                );
        }

        _forward.Normalize();

        _forward =
            Quaternion.AngleAxis(
                _look.x *
                _sensitivity,
                _up
            ) *
            _forward;

        transform.rotation =
            Quaternion.LookRotation(
                _forward,
                _up
            );
    }

    private void Move()
    {
        if (cursorlogic.uiMode)
        {
            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    0f,
                    _deceleration *
                    Time.deltaTime
                );

            currentMove =
                Vector3.MoveTowards(
                    currentMove,
                    Vector3.zero,
                    _deceleration *
                    Time.deltaTime
                );

            return;
        }

        Vector2 _input =
            _moveAction.ReadValue<Vector2>();

        Vector3 _up =
            planetUp;

        Vector3 _forward =
            Vector3.ProjectOnPlane(
                transform.forward,
                _up
            );

        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
                    _up
                );
        }

        _forward.Normalize();

        Vector3 _right =
            Vector3.Cross(
                _up,
                _forward
            ).normalized;

        Vector3 _direction =
            _right *
            _input.x +
            _forward *
            _input.y;

        if (_direction.sqrMagnitude > 1f)
            _direction.Normalize();

        float _targetSpeed = 0f;

        if (_input.sqrMagnitude > 0.01f)
        {
            _targetSpeed =
                _runAction.IsPressed() &&
                _input.y > 0f
                ? _runSpeed
                : _walkSpeed;
        }

        float _rate =
            _targetSpeed > currentSpeed
            ? _acceleration
            : _deceleration;

        currentSpeed =
            Mathf.MoveTowards(
                currentSpeed,
                _targetSpeed,
                _rate *
                Time.deltaTime
            );

        Vector3 _targetMove =
            _direction *
            currentSpeed;

        currentMove =
            Vector3.SmoothDamp(
                currentMove,
                _targetMove,
                ref moveVelocity,
                0.1f
            );
    }

    private void ApplyGravity()
    {
        if (_gravitybody == null)
            return;

        Vector3 _movement =
            currentMove *
            Time.deltaTime;

        _gravitybody.movecharacter(
            _movement,
            _jumpAction.WasPressedThisFrame() &&
            !cursorlogic.uiMode,
            _jumpHeight,
            Time.deltaTime
        );
    }

    private void alignwithplanet(bool _instant)
    {
        if (planet == null)
            return;

        Vector3 _up =
            planetUp;

        Vector3 _forward =
            Vector3.ProjectOnPlane(
                transform.forward,
                _up
            );

        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward =
                Vector3.ProjectOnPlane(
                    cameraTransform.forward,
                    _up
                );
        }

        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward =
                Vector3.ProjectOnPlane(
                    Vector3.forward,
                    _up
                );
        }

        _forward.Normalize();

        Quaternion _target =
            Quaternion.LookRotation(
                _forward,
                _up
            );

        if (_instant)
        {
            transform.rotation =
                _target;

            return;
        }

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                _target,
                _planetRotationSpeed *
                Time.deltaTime
            );
    }
    private void CameraSway()
    {
        Vector2 _look = _lookAction.ReadValue<Vector2>();

        float _targetRoll =
            Mathf.Clamp(
                -_look.x * 0.15f,
                -1.2f,
                1.2f
            );

        currentRoll =
            Mathf.Lerp(
                currentRoll,
                _targetRoll,
                12f * Time.deltaTime
            );
    }

    private void HeadBob()
    {
        if (cursorlogic.uiMode)
            return;

        Vector2 _input = _moveAction.ReadValue<Vector2>();

        bool _isMoving =
            _input.sqrMagnitude > 0.01f;

        float _targetWeight =
            _isMoving ? 1f : 0f;

        bobWeight =
            Mathf.Lerp(
                bobWeight,
                _targetWeight,
                8f * Time.deltaTime
            );

        float _speedMultiplier = 1f;

        if (_runAction.IsPressed())
        {
            _speedMultiplier = 1.5f;
        }

        if (_isMoving)
        {
            bobTime +=
                Time.deltaTime *
                bobSpeed *
                _speedMultiplier;
        }
        else if (bobWeight < 0.01f)
        {
            bobTime = 0f;
        }

        float _xOffset =
            Mathf.Sin(bobTime) *
            horizontalDistance *
            bobWeight;

        float _yOffset =
            Mathf.Cos(bobTime * 2f) *
            verticalDistance *
            bobWeight;

        cameraTransform.localPosition =
            Vector3.Lerp(
                cameraTransform.localPosition,
                defaultCameraPosition +
                new Vector3(
                    _xOffset,
                    _yOffset,
                    0f
                ),
                10f * Time.deltaTime
            );
    }

    private void ApplyCameraRotation()
    {
        cameraTransform.localRotation =
            Quaternion.Euler(
                rotationX - bobPitch,
                0f,
                currentRoll + bobRoll
            );
    }
}