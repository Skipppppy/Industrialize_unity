using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class sphericalgravitybody : MonoBehaviour
{
    [Header("Planet")]
    [SerializeField] private pointterrain _planet;
    [SerializeField] private float _gravitystrength = 25f;

    [Header("Settings")]
    [SerializeField] private bool _zerogravity = false;

    private Rigidbody _rigidbody;
    private bool _held;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void FixedUpdate()
    {
        if (_planet == null)
            return;

        if (_zerogravity || _held || _rigidbody.isKinematic)
            return;

        Vector3 _direction = _planet.transform.position - _rigidbody.worldCenterOfMass;

        if (_direction.sqrMagnitude < 0.000001f)
            return;

        _direction.Normalize();

        _rigidbody.AddForce(
            _direction * _gravitystrength,
            ForceMode.Acceleration
        );
    }

    public void setheld(bool _value)
    {
        _held = _value;
    }

    public void setzerogravity(bool _value)
    {
        _zerogravity = _value;
    }
}