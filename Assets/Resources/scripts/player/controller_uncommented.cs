using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform checkGround;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] public PlayerInput playerInput;
    [SerializeField] public CursorManager cursorlogic;
    [SerializeField] private pointterrain _planet;
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _jumpHeight = 1f;
    [SerializeField] private float _gravity = 14f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;
    [SerializeField] private bool _zerogravity = false;
    [Header("Mouse")]
    [SerializeField] private float _sensitivity = 0.2f;
    [SerializeField] private float _uiSensitivity = 0.3f;
    [SerializeField] private float _buildingUISensitivity = 0.03f;
    [Header("Camera Effects")]
    [SerializeField] private float bobSpeed = 7f;
    [SerializeField] public float horizontalDistance = 0.015f;
    [SerializeField] public float verticalDistance = 0.03f;
    [Header("Ground Check")]
    [SerializeField] private float _checkRadius = 0.2f;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _runAction;
    private Vector3 currentmove;
    private Vector3 movevelocity;
    private Vector3 velocity;
    private float rotationX;
    private bool isGrounded;
    private float currentSpeed;
    private float currentRoll;
    private float bobWeight;
    private float bobPitch;
    private float bobRoll;
    private float bobTime;
    private bool _buildingUIMode;
    public Transform cameraHolder;
    private Vector3 defaultCameraPosition;
    private void Awake()
    {
        _moveAction = playerInput.actions["Move"];
        _lookAction = playerInput.actions["Look"];
        _jumpAction = playerInput.actions["Jump"];
        _runAction = playerInput.actions["Sprint"];
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
        defaultCameraPosition = cameraTransform.localPosition;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Update()
    {
        if (_planet != null && !_planet.isready())
        {
            return;
        }
        AlignToPlanet();
        GroundCheck();
        Rotate();
        Move();
        ApplyGravity();
        CorrectSurfacePosition();
        AlignToPlanet();
        CameraSway();
        HeadBob();
        ApplyCameraRotation();
    }
    private bool IsUIMode()
    {
        return cursorlogic != null && cursorlogic.uiMode;
    }
    private bool IsMovementBlocked()
    {
        return IsUIMode() || _buildingUIMode;
    }
    private Vector3 GetPlanetUp()
    {
        if (_planet == null)
        {
            return Vector3.up;
        }
        return _planet.getup(transform.position);
    }
    private Vector3 GetGravityDirection()
    {
        if (_planet == null)
        {
            return Vector3.down;
        }
        return _planet.getgravitydirection(transform.position);
    }
    private float GetGravityStrength()
    {
        if (_zerogravity)
        {
            return 0f;
        }
        if (_planet == null)
        {
            return _gravity;
        }
        return _planet.getgravitystrength(transform.position);
    }
    private void AlignToPlanet()
    {
        if (_planet == null)
        {
            return;
        }
        Vector3 _up = GetPlanetUp();
        Vector3 _forward = Vector3.ProjectOnPlane(
        transform.forward,
        _up
        );
        if (_forward.sqrMagnitude < 0.0001f)
        {
            _forward = Vector3.ProjectOnPlane(
            transform.right,
            _up
            );
        }
        if (_forward.sqrMagnitude < 0.0001f)
        {
            return;
        }
        transform.rotation = Quaternion.LookRotation(
        _forward.normalized,
        _up
        );
    }
    private void Rotate()
    {
        if (IsUIMode())
        {
            return;
        }
        Vector2 _look = _lookAction.ReadValue<Vector2>();
        float _currentsensitivity =
        _buildingUIMode
        ? _buildingUISensitivity
        : _sensitivity;
        Vector3 _up = GetPlanetUp();
        Quaternion _yawrotation = Quaternion.AngleAxis(
        _look.x * _currentsensitivity,
        _up
        );
        transform.rotation =
        _yawrotation *
        transform.rotation;
        rotationX -= _look.y * _currentsensitivity;
        rotationX = Mathf.Clamp(
        rotationX,
        -80f,
        80f
        );
    }
    private void Move()
    {
        if (IsMovementBlocked())
        {
            currentmove = Vector3.zero;
            movevelocity = Vector3.zero;
            currentSpeed = 0f;
            return;
        }
        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 _up = GetPlanetUp();
        Vector3 _forward =
        Vector3.ProjectOnPlane(
        transform.forward,
        _up
        ).normalized;
        Vector3 _right =
        Vector3.ProjectOnPlane(
        transform.right,
        _up
        ).normalized;
        Vector3 move =
        _right * input.x +
        _forward * input.y;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }
        float targetSpeed = 0f;
        if (input.sqrMagnitude > 0f)
        {
            targetSpeed =
            _runAction.IsPressed() && input.y > 0f
            ? _runSpeed
            : _walkSpeed;
        }
        float rate =
        targetSpeed > currentSpeed
        ? _acceleration
        : _deceleration;
        currentSpeed =
        Mathf.MoveTowards(
        currentSpeed,
        targetSpeed,
        rate * Time.deltaTime
        );
        Vector3 targetMove =
        (_right * input.x +
        _forward * input.y) *
        targetSpeed;
        currentmove =
        Vector3.SmoothDamp(
        currentmove,
        targetMove,
        ref movevelocity,
        0.1f
        );
        currentmove =
        Vector3.ProjectOnPlane(
        currentmove,
        _up
        );
        characterController.Move(
        currentmove *
        Time.deltaTime
        );
    }
    private void ApplyGravity()
    {
        if (_zerogravity)
        {
            velocity = Vector3.zero;
            return;
        }
        float _gravityStrength =
        GetGravityStrength();
        if (_gravityStrength <= 0f)
        {
            velocity = Vector3.zero;
            return;
        }
        if (isGrounded)
        {
            velocity = Vector3.zero;
            if (_jumpAction.WasPressedThisFrame())
            {
                Vector3 _jumpDirection =
                -GetGravityDirection();
                velocity =
                _jumpDirection *
                Mathf.Sqrt(
                _jumpHeight *
                2f *
                _gravityStrength
                );
                characterController.Move(
                velocity *
                Time.deltaTime
                );
            }
            return;
        }
        Vector3 _gravityDirection =
        GetGravityDirection();
        velocity +=
        _gravityDirection *
        _gravityStrength *
        Time.deltaTime;
        characterController.Move(
        velocity *
        Time.deltaTime
        );
    }
    private void CorrectSurfacePosition()
    {
        if (_planet == null ||
        characterController == null)
        {
            return;
        }
        Vector3 _offset =
        transform.position -
        _planet.transform.position;
        float _distance = _offset.magnitude;
        if (_distance < 0.001f)
        {
            return;
        }
        Vector3 _up =
        _offset.normalized;
        float _surfaceRadius =
        _planet.getsurfaceradius(_up);
        float _bottomOffset =
        characterController.center.y -
        characterController.height *
        0.5f;
        float _minimumDistance =
        _surfaceRadius -
        _bottomOffset +
        0.02f;
        if (_distance < _minimumDistance)
        {
            float _difference =
            _minimumDistance -
            _distance;
            characterController.Move(
            _up * _difference
            );
            velocity = Vector3.zero;
        }
    }
    private void CameraSway()
    {
        Vector2 look =
        _lookAction.ReadValue<Vector2>();
        float targetRoll =
        Mathf.Clamp(
        -look.x *
        0.15f,
        -1.2f,
        1.2f
        );
        currentRoll =
        Mathf.Lerp(
        currentRoll,
        targetRoll,
        12f *
        Time.deltaTime
        );
    }
    private void HeadBob()
    {
        if (IsMovementBlocked())
        {
            return;
        }
        Vector2 input =
        _moveAction.ReadValue<Vector2>();
        bool isMoving =
        input.sqrMagnitude > 0.01f;
        float targetWeight =
        isMoving ? 1f : 0f;
        bobWeight =
        Mathf.Lerp(
        bobWeight,
        targetWeight,
        8f *
        Time.deltaTime
        );
        float speedMultiplier =
        _runAction.IsPressed()
        ? 1.5f
        : 1f;
        if (isMoving)
        {
            bobTime +=
            Time.deltaTime *
            bobSpeed *
            speedMultiplier;
        }
        else if (bobWeight < 0.01f)
        {
            bobTime = 0f;
        }
        float xOffset =
        Mathf.Sin(bobTime) *
        horizontalDistance *
        bobWeight;
        float yOffset =
        Mathf.Cos(bobTime * 2f) *
        verticalDistance *
        bobWeight;
        cameraTransform.localPosition =
        Vector3.Lerp(
        cameraTransform.localPosition,
        defaultCameraPosition +
        new Vector3(
        xOffset,
        yOffset,
        0f
        ),
        10f *
        Time.deltaTime
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
    private void GroundCheck()
    {
        isGrounded =
        Physics.CheckSphere(
        checkGround.position,
        _checkRadius,
        groundMask,
        QueryTriggerInteraction.Ignore
        );
    }
    public void setbuildinguimode(bool _value)
    {
        _buildingUIMode = _value;
        if (_value)
        {
            currentmove = Vector3.zero;
            movevelocity = Vector3.zero;
            currentSpeed = 0f;
        }
    }
    public bool isbuildinguimode()
    {
        return _buildingUIMode;
    }
}