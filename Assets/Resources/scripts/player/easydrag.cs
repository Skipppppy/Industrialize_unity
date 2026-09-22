using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerDragging : MonoBehaviour
{
    [Header("Drag Point")]
    [SerializeField] private Transform dragPoint;

    [Header("Drag Physics")]
    [SerializeField] private float followForce = 30f;
    [SerializeField] private float followDamper = 8f;
    [SerializeField] private float maxForce = 500f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private EasyInv inventory;
    [SerializeField] private Image crosshairImage;

    [Header("Crosshair")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite canGrabSprite;
    [SerializeField] private Sprite cannotGrabSprite;
    [SerializeField] private Sprite draggingSprite;

    [Header("Dragging")]
    [SerializeField] private float pickupDistance = 5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 0.35f;
    [SerializeField] private float maxAngularVelocity = 15f;

    [Header("Throw")]
    [SerializeField] private float throwForce = 8f;
    [SerializeField] private float throwUpForce = 1f;

    [Header("Layers")]
    [SerializeField] private LayerMask draggableMask;

    public CursorManager ui;

    private Rigidbody _draggedRigidbody;
    private Collider _draggedCollider;
    private Vector3 _localGrabPoint;
    private bool _isDragging;
    private bool _isRotating;

    public bool IsDragging => _isDragging;

    private void Start()
    {
        SetCrosshair(defaultSprite);
    }

    private void Update()
    {
        if (_isDragging)
        {
            UpdateDragging();
            return;
        }

        UpdateCrosshair();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryGrabObject();
    }

    private void UpdateDragging()
    {
        if (_draggedRigidbody == null)
        {
            ReleaseObject();
            return;
        }

        SetCrosshair(draggingSprite);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ThrowObject();
            return;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ReleaseObject();
            return;
        }

        _isRotating =
            Keyboard.current != null &&
            Keyboard.current.rKey.isPressed &&
            Mouse.current.leftButton.isPressed;

        if (_isRotating)
            RotateObject();
    }

    private void FixedUpdate()
    {
        if (!_isDragging)
            return;

        if (_draggedRigidbody == null)
            return;

        FollowDragPoint();
    }

    private void UpdateCrosshair()
    {
        if (playerCamera == null)
            return;

        Ray _ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        if (!Physics.Raycast(
            _ray,
            out RaycastHit _hit,
            pickupDistance,
            draggableMask,
            QueryTriggerInteraction.Ignore))
        {
            SetCrosshair(defaultSprite);
            return;
        }

        if (!IsMainHandFree())
        {
            SetCrosshair(cannotGrabSprite);
            return;
        }

        if (_hit.collider.attachedRigidbody == null)
        {
            SetCrosshair(cannotGrabSprite);
            return;
        }

        SetCrosshair(canGrabSprite);
    }

    private void SetCrosshair(Sprite _sprite)
    {
        if (crosshairImage == null)
            return;

        if (_sprite != null)
            crosshairImage.sprite = _sprite;
    }

    private bool IsMainHandFree()
    {
        if (inventory == null)
            return false;

        return inventory.GetItem(
            inventory.ActiveHand
        ) == null;
    }

    private void TryGrabObject()
    {
        if (ui != null && ui.uiMode)
            return;

        if (!IsMainHandFree())
            return;

        Ray _ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        if (!Physics.Raycast(
            _ray,
            out RaycastHit _hit,
            pickupDistance,
            draggableMask,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Rigidbody _rb =
            _hit.collider.attachedRigidbody;

        if (_rb == null)
            return;

        StartDragging(
            _rb,
            _hit.collider,
            _hit.point
        );
    }

    private void StartDragging(
        Rigidbody _rb,
        Collider _collider,
        Vector3 _worldGrabPoint)
    {
        _draggedRigidbody = _rb;
        _draggedCollider = _collider;

        _localGrabPoint =
            _rb.transform.InverseTransformPoint(
                _worldGrabPoint
            );

        _isDragging = true;
        _isRotating = false;

        _rb.useGravity = false;
        _rb.linearDamping = 2f;
        _rb.angularDamping = 2f;
        _rb.maxAngularVelocity = maxAngularVelocity;

        SetCrosshair(draggingSprite);
    }

    private void FollowDragPoint()
    {
        if (dragPoint == null)
            return;

        Vector3 _grabPoint =
            _draggedRigidbody.transform.TransformPoint(
                _localGrabPoint
            );

        Vector3 _difference =
            dragPoint.position - _grabPoint;

        Vector3 _pointVelocity =
            _draggedRigidbody.GetPointVelocity(
                _grabPoint
            );

        Vector3 _force =
            _difference * followForce -
            _pointVelocity * followDamper;

        _force =
            Vector3.ClampMagnitude(
                _force,
                maxForce
            );

        _draggedRigidbody.AddForceAtPosition(
            _force,
            _grabPoint,
            ForceMode.Force
        );
    }

    private void RotateObject()
    {
        if (_draggedRigidbody == null)
            return;

        Vector2 _mouseDelta =
            Mouse.current.delta.ReadValue();

        if (_mouseDelta.sqrMagnitude <= 0.0001f)
            return;

        Vector3 _rotation =
            playerCamera.transform.up *
            _mouseDelta.x *
            rotationSpeed;

        _rotation +=
            playerCamera.transform.right *
            -_mouseDelta.y *
            rotationSpeed;

        _draggedRigidbody.angularVelocity =
            Vector3.ClampMagnitude(
                _rotation,
                maxAngularVelocity
            );
    }

    private void ReleaseObject()
    {
        if (_draggedRigidbody != null)
        {
            _draggedRigidbody.useGravity = true;
            _draggedRigidbody.linearDamping = 0f;
            _draggedRigidbody.angularDamping = 0.05f;
        }

        _draggedRigidbody = null;
        _draggedCollider = null;
        _isDragging = false;
        _isRotating = false;

        SetCrosshair(defaultSprite);
    }

    private void ThrowObject()
    {
        if (_draggedRigidbody == null)
            return;

        Rigidbody _rb =
            _draggedRigidbody;

        _rb.useGravity = true;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0.05f;

        Vector3 _velocity =
            playerCamera.transform.forward *
            throwForce;

        _velocity +=
            Vector3.up *
            throwUpForce;

        _rb.linearVelocity = _velocity;

        _draggedRigidbody = null;
        _draggedCollider = null;
        _isDragging = false;
        _isRotating = false;

        SetCrosshair(defaultSprite);
    }
}