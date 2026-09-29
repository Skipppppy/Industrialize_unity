using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class sphericalgravitybody : MonoBehaviour
{
    [SerializeField] private pointterrain _planet;
    [SerializeField] private LayerMask _groundmask;
    [SerializeField] private bool _zerogravity = false;
    [SerializeField] private float _grounddamping = 8f;
    private Rigidbody _rigidbody;
    private bool _held;
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rigidbody.sleepThreshold = 0.01f;
    }
    private void Start()
    {
        if (_planet == null)
        {
            _planet = FindFirstObjectByType<pointterrain>();
        }
    }
    private void FixedUpdate()
    {
        if (_planet == null || _zerogravity || _held || _rigidbody.isKinematic) return;
        float _gravity = _planet.getgravitystrength(transform.position);
        if (_gravity <= 0f) return;
        Vector3 _gravitydirection = _planet.getgravitydirection(transform.position);
        _rigidbody.AddForce(
        _gravitydirection * _gravity,
        ForceMode.Acceleration
        );
    }
    private void OnCollisionStay(Collision _collision)
    {
        if (_held || _rigidbody.isKinematic) return;
        if ((_groundmask.value & (1 << _collision.gameObject.layer)) == 0) return;
        Vector3 _normal = Vector3.zero;
        int _contacts = _collision.contactCount;
        for (int _i = 0; _i < _contacts; _i++)
        {
            _normal += _collision.GetContact(_i).normal;
        }
        if (_contacts <= 0) return;
        _normal.Normalize();
        if (_normal.sqrMagnitude < 0.001f) return;
        Vector3 _velocity = _rigidbody.linearVelocity;
        float _normalvelocity = Vector3.Dot(_velocity, _normal);
        _velocity -= _normal * _normalvelocity;
        Vector3 _tangentvelocity = Vector3.ProjectOnPlane(_velocity, _normal);
        _tangentvelocity *= Mathf.Clamp01(1f - _grounddamping * Time.fixedDeltaTime);
        _rigidbody.linearVelocity = _tangentvelocity;
    }
    public void setheld(bool _value)
    {
        _held = _value;
        _rigidbody.useGravity = false;
        if (_value)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
    public bool isheld()
    {
        return _held;
    }
    public void throwobject(Vector3 _direction, float _force)
    {
        _held = false;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.AddForce(
        _direction.normalized * _force,
        ForceMode.VelocityChange
        );
    }
}