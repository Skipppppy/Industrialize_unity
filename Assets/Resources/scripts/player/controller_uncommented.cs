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

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _jumpHeight = 1f;
    [SerializeField] private float _gravity = -14f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;

    [Header("Mouse")]
    [SerializeField] private float _sensitivity = 10f;

    [Header("Camera Effects")]
    //[SerializeField] private float swayAmount = 5f;
    //[SerializeField] private float swaySmooth = 8f;
    [SerializeField] private float bobSpeed = 7f;
    //[SerializeField] private float pitchAmount = 2f;
    //[SerializeField] private float rollAmount = 3f;
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
        GroundCheck();
        Rotate();
        Move();
        ApplyGravity();
        CameraSway();
        HeadBob();
        ApplyCameraRotation();
    }

    private void Rotate()
    {
        Vector2 look = _lookAction.ReadValue<Vector2>();

        rotationX -= look.y * _sensitivity;
        rotationX = Mathf.Clamp(rotationX, -80f, 80f);

        transform.Rotate(Vector3.up * look.x * _sensitivity);

        if (cursorlogic.uiMode)
        {
            _sensitivity = 0.03f;
        }
        else
        {
            _sensitivity = 0.2f;
        }
    }

    private void Move()
    {
        if (cursorlogic.uiMode)
            return;

        Vector2 input = _moveAction.ReadValue<Vector2>();

        Vector3 move = (transform.right * input.x + transform.forward * input.y).normalized;

        float targetSpeed = 0f;

        if (input.sqrMagnitude > 0)
        {
            targetSpeed = _runAction.IsPressed() && input.y > 0
                ? _runSpeed
                : _walkSpeed;
        }

        float rate = targetSpeed > currentSpeed
            ? _acceleration
            : _deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            rate * Time.deltaTime
        );

        Vector3 targetMove =
            (transform.right * input.x + transform.forward * input.y) * targetSpeed;

        currentmove = Vector3.SmoothDamp(
            currentmove,
            targetMove,
            ref movevelocity,
            0.1f
        );

        characterController.Move(
            move * currentSpeed * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (_jumpAction.WasPressedThisFrame() && isGrounded)
            velocity.y = Mathf.Sqrt(
                _jumpHeight * -2f * _gravity
            );

        velocity.y += _gravity * Time.deltaTime;

        characterController.Move(
            velocity * Time.deltaTime
        );
    }

    private void CameraSway()
    {
        Vector2 look = _lookAction.ReadValue<Vector2>();

        float targetRoll = Mathf.Clamp(
            -look.x * 0.15f,
            -1.2f,
            1.2f
        );

        currentRoll = Mathf.Lerp(
            currentRoll,
            targetRoll,
            12f * Time.deltaTime
        );
    }

    private void HeadBob()
    {
        if (cursorlogic.uiMode)
            return;

        Vector2 input = _moveAction.ReadValue<Vector2>();

        bool isMoving = input.sqrMagnitude > 0.01f;

        float targetWeight = isMoving ? 1f : 0f;

        bobWeight = Mathf.Lerp(
            bobWeight,
            targetWeight,
            8f * Time.deltaTime
        );

        float speedMultiplier = _runAction.IsPressed()
            ? 1.5f
            : 1f;

        if (isMoving)
        {
            bobTime += Time.deltaTime * bobSpeed * speedMultiplier;
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

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            defaultCameraPosition +
            new Vector3(xOffset, yOffset, 0f),
            10f * Time.deltaTime
        );
    }

    private void ApplyCameraRotation()
    {
        cameraTransform.localRotation = Quaternion.Euler(
            rotationX - bobPitch,
            0f,
            currentRoll + bobRoll
        );
    }

    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(
            checkGround.position,
            _checkRadius,
            groundMask
        );
    }
}