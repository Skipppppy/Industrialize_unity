using UnityEngine;
public class planetgravity : MonoBehaviour
{
    [Header("Planet")]
    [SerializeField] private voxelworld _planet;
    [SerializeField] private float _gravity = 25f;
    [SerializeField] private bool _automaticgravityradius = true;
    [SerializeField] private float _gravityradius = 52000f;
    [SerializeField] private float _gravityradiusoffset = 2000f;
    [Header("Character")]
    [SerializeField] private float _surfaceoffset = 0.02f;
    [SerializeField] private float _grounddistance = 0.04f;
    [SerializeField] private float _snapdistance = 5f;
    [SerializeField] private float _rotationspeed = 20f;
    [Header("Rigidbody")]
    [SerializeField] private float _objectoffset = 0.01f;
    [SerializeField] private float _landvelocity = 0.5f;
    private CharacterController _charactercontroller;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private bool _held;
    private float _verticalvelocity;
    public bool isgrounded { get; private set; }
    public Vector3 up
    {
        get
        {
            return getup(transform.position);
        }
    }
    public Vector3 gravity
    {
        get
        {
            return -up * _gravity;
        }
    }
    private void Awake()
    {
        _charactercontroller = GetComponent<CharacterController>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        if (_planet == null)
            _planet = FindFirstObjectByType<voxelworld>();
        if (_rigidbody != null)
        {
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
    private void FixedUpdate()
    {
        if (_rigidbody == null)
            return;
        if (_held || _rigidbody.isKinematic)
            return;
        if (_planet == null)
        {
            _planet = FindFirstObjectByType<voxelworld>();
            if (_planet == null)
                return;
        }
        Vector3 _position = _rigidbody.position;
        if (!ingravitysphere(_position))
            return;
        Vector3 _up = getup(_position);
        _rigidbody.AddForce(-_up * _gravity, ForceMode.Acceleration);
        checkrigidbodysurface(_up);
    }
    private void LateUpdate()
    {
        if (_charactercontroller == null)
            return;
        if (_planet == null)
            return;
        Vector3 _up = getup(transform.position);
        Vector3 _forward = Vector3.ProjectOnPlane(transform.forward, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(transform.right, _up);
        if (_forward.sqrMagnitude < 0.001f)
            return;
        _forward.Normalize();
        Quaternion _target = Quaternion.LookRotation(_forward, _up);
        transform.rotation = Quaternion.Slerp(transform.rotation, _target, _rotationspeed * Time.deltaTime);
    }
    public void movecharacter(Vector3 _movement, bool _jump, float _jumpheight, float _deltatime)
    {
        if (_charactercontroller == null)
            return;
        if (_planet == null)
        {
            _planet = FindFirstObjectByType<voxelworld>();
            if (_planet == null)
                return;
        }
        Vector3 _up = getup(transform.position);
        checkcharacterground(_up);
        if (_jump && isgrounded)
        {
            _verticalvelocity = Mathf.Sqrt(2f * _gravity * Mathf.Max(_jumpheight, 0f));
            isgrounded = false;
        }
        if (isgrounded)
        {
            _verticalvelocity = 0f;
        }
        else
        {
            _verticalvelocity -= _gravity * _deltatime;
        }
        Vector3 _gravitymove = _up * _verticalvelocity * _deltatime;
        Vector3 _totalmove = _movement + _gravitymove;
        _charactercontroller.Move(_totalmove);
        _up = getup(transform.position);
        forcecharactersurface(_up);
        checkcharacterground(_up);
        if (isgrounded)
            _verticalvelocity = 0f;
    }
    private void checkcharacterground(Vector3 _up)
    {
        Vector3 _bottom = getcharacterbottom();
        Vector3 _surfaceup = getup(_bottom);
        float _bottomradius = Vector3.Distance(_bottom, _planet.transform.position);
        float _surfacereadius = _planet.planetradius + _planet.getterrainheight(_surfaceup) + _surfaceoffset;
        float _difference = _bottomradius - _surfacereadius;
        bool _surfacegrounded = _difference <= _grounddistance && _difference >= -_grounddistance;
        bool _physicsgrounded = _charactercontroller.isGrounded;
        isgrounded = _surfacegrounded || _physicsgrounded;
    }
    private void forcecharactersurface(Vector3 _up)
    {
        Vector3 _bottom = getcharacterbottom();
        Vector3 _surfaceup = getup(_bottom);
        float _bottomradius = Vector3.Distance(_bottom, _planet.transform.position);
        float _surfacereadius = _planet.planetradius + _planet.getterrainheight(_surfaceup) + _surfaceoffset;
        float _difference = _bottomradius - _surfacereadius;
        if (Mathf.Abs(_difference) > _snapdistance)
            return;
        if (_verticalvelocity > 0f)
            return;
        _charactercontroller.Move(-_surfaceup * _difference);
        isgrounded = true;
        _verticalvelocity = 0f;
    }
    private Vector3 getcharacterbottom()
    {
        Vector3 _center = transform.TransformPoint(_charactercontroller.center);
        Vector3 _up = getup(_center);
        return _center - _up * (_charactercontroller.height * 0.5f);
    }
    private void checkrigidbodysurface(Vector3 _up)
    {
        float _terrainheight = _planet.getterrainheight(_up);
        float _surfacereadius = _planet.planetradius + _terrainheight;
        float _objectradius = getobjectradius();
        float _targetradius = _surfacereadius + _objectradius + _objectoffset;
        Vector3 _fromcenter = _rigidbody.position - _planet.transform.position;
        float _currentradius = _fromcenter.magnitude;
        if (_currentradius > _targetradius)
            return;
        Vector3 _targetposition = _planet.transform.position + _up * _targetradius;
        _rigidbody.position = _targetposition;
        Vector3 _velocity = _rigidbody.linearVelocity;
        float _radialvelocity = Vector3.Dot(_velocity, _up);
        Vector3 _tangentvelocity = Vector3.ProjectOnPlane(_velocity, _up);
        if (_radialvelocity < 0f)
            _radialvelocity = 0f;
        if (_tangentvelocity.magnitude < _landvelocity)
            _tangentvelocity = Vector3.zero;
        _rigidbody.linearVelocity = _up * _radialvelocity + _tangentvelocity;
        if (_rigidbody.linearVelocity.magnitude < 0.05f)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
    private float getobjectradius()
    {
        if (_collider == null)
            return 0.1f;
        Bounds _bounds = _collider.bounds;
        return Mathf.Max(_bounds.extents.x, _bounds.extents.y, _bounds.extents.z);
    }
    public Vector3 getup(Vector3 _position)
    {
        if (_planet == null)
            return transform.up;
        Vector3 _direction = _position - _planet.transform.position;
        if (_direction.sqrMagnitude < 0.001f)
            return Vector3.up;
        return _direction.normalized;
    }
    public bool ingravitysphere(Vector3 _position)
    {
        if (_planet == null)
            return false;
        float _radius = getgravityradius();
        float _distance = Vector3.Distance(_position, _planet.transform.position);
        return _distance <= _radius;
    }
    private float getgravityradius()
    {
        if (_planet == null)
            return _gravityradius;
        if (!_automaticgravityradius)
            return _gravityradius;
        return _planet.planetradius + _planet.maxterrainheight + _gravityradiusoffset;
    }
    public void setheld(bool _value)
    {
        _held = _value;
        if (_rigidbody == null)
            return;
        _rigidbody.isKinematic = _value;
        _rigidbody.useGravity = false;
        if (_value)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
    public void setplanet(voxelworld _world)
    {
        _planet = _world;
    }
}