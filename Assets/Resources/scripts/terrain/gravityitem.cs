using UnityEngine;

public class planetgravityfield : MonoBehaviour
{
    [SerializeField] private voxelworld _planet;
    [SerializeField] private float _gravity = 25f;
    [SerializeField] private float _gravityradius = 55000f;

    private void Awake()
    {
        if (_planet == null)
            _planet = GetComponent<voxelworld>();
    }

    private void FixedUpdate()
    {
        if (_planet == null)
            return;

        GameObject[] _items =
            GameObject.FindGameObjectsWithTag("Item");

        for (int _i = 0; _i < _items.Length; _i++)
        {
            GameObject _item = _items[_i];

            if (_item == null)
                continue;

            Rigidbody _rigidbody =
                _item.GetComponent<Rigidbody>();

            if (_rigidbody == null)
                continue;

            if (_rigidbody.isKinematic)
                continue;

            Vector3 _offset =
                _rigidbody.position -
                _planet.transform.position;

            float _distance =
                _offset.magnitude;

            if (_distance < 0.001f)
                continue;

            if (_distance > _gravityradius)
                continue;

            Vector3 _direction =
                _offset.normalized;

            _rigidbody.useGravity = false;

            _rigidbody.AddForce(
                -_direction * _gravity,
                ForceMode.Acceleration
            );
        }
    }
}