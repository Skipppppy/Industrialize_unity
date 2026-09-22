using System.Collections;
using UnityEngine;

public class tree : MonoBehaviour
{
    [SerializeField] private float _health = 100f;
    [SerializeField] private float _fallenhealth = 100f;
    [SerializeField] private float _fallforce = 900f;
    [SerializeField] private float _fallcutdelay = 1f;
    [SerializeField] private int _sections = 4;
    [SerializeField] private float _sectionlength = 1f;
    [SerializeField] private GameObject _logprefab;

    private float _currenthealth;
    private Rigidbody _rigidbody;
    private bool _fallen;
    private bool _canCut;
    private bool _cut;

    private void Awake()
    {
        _currenthealth = _health;

        _rigidbody = GetComponent<Rigidbody>();

        if (_rigidbody == null)
            _rigidbody = gameObject.AddComponent<Rigidbody>();

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void damage(float _damage, Vector3 _hitpoint)
    {
        if (_damage <= 0f)
            return;

        if (_cut)
            return;

        if (!_fallen)
        {
            _currenthealth -= _damage;

            if (_currenthealth <= 0f)
            {
                _currenthealth = 0f;
                fall(_hitpoint);
            }

            return;
        }

        if (!_canCut)
            return;

        _currenthealth -= _damage;

        if (_currenthealth <= 0f)
        {
            _currenthealth = 0f;
            cut();
        }
    }

    private void fall(Vector3 _hitpoint)
    {
        _fallen = true;

        Vector3 _direction = transform.position - _hitpoint;
        _direction.y = 0f;

        if (_direction.sqrMagnitude < 0.001f)
            _direction = -transform.forward;

        _direction.Normalize();

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        Vector3 _axis = Vector3.Cross(Vector3.up, _direction);

        if (_axis.sqrMagnitude < 0.001f)
            _axis = transform.right;

        _axis.Normalize();

        _rigidbody.AddTorque(_axis * _fallforce, ForceMode.Impulse);

        StartCoroutine(enablecut());
    }

    private IEnumerator enablecut()
    {
        yield return new WaitForSeconds(_fallcutdelay);

        if (!_cut)
            _canCut = true;
    }

    private void cut()
    {
        if (_cut)
            return;

        _cut = true;

        int _count = Mathf.Max(1, _sections);

        if (_logprefab == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 _direction = transform.up;

        if (_direction.sqrMagnitude < 0.001f)
            _direction = Vector3.forward;

        _direction.Normalize();

        float _center = (_count - 1) * 0.5f;

        for (int _i = 0; _i < _count; _i++)
        {
            float _offset = (_i - _center) * _sectionlength;
            Vector3 _position = transform.position + _direction * _offset;

            Instantiate(
                _logprefab,
                _position,
                transform.rotation
            );
        }

        Destroy(gameObject);
    }
}