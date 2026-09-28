using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pointterrain : MonoBehaviour
{
    [Header("Streaming")]
    [SerializeField] private Transform _player;
    [SerializeField] private int _chunksize = 16;
    [SerializeField] private int _viewdistance = 2;
    [SerializeField] private int _chunksperframe = 0;

    [Header("Points")]
    [SerializeField] private float _pointsize = 1f;

    [Header("Terrain")]
    [SerializeField] private float _baseheight = 1f;
    [SerializeField] private float _height = 1.5f;
    [SerializeField] private float _noisescale = 0.012f;
    [SerializeField, Range(0f, 1f)] private float _flatness = 0.9f;

    [Header("Texture")]
    [SerializeField] private float _textureworldsize = 4f;
    [SerializeField] private Material _grassmaterial;

    [Header("Debug")]
    [SerializeField] private bool _showpoints = false;
    [SerializeField] private bool _showchunks = false;
    [SerializeField] private float _pointgizmosize = 0.08f;

    [Header("Generation")]
    [SerializeField] private int _seed = 12345;

    private Dictionary<Vector2Int, pointterrainchunk> _chunks = new Dictionary<Vector2Int, pointterrainchunk>();
    private Vector2Int _lastplayerchunk = new Vector2Int(int.MinValue, int.MinValue);
    private bool _lastshowpoints;
    private bool _lastshowchunks;
    private float _lastpointgizmosize = -1f;
    private bool _streaming;

    private void Start()
    {
        updatestream();
        applydebug();
    }

    private void Update()
    {
        if (_player == null) return;

        Vector2Int _playerchunk = getplayerchunk();

        if (_playerchunk != _lastplayerchunk)
        {
            _lastplayerchunk = _playerchunk;
            updatestream();
        }

        if (_showpoints != _lastshowpoints ||
        _showchunks != _lastshowchunks ||
        !Mathf.Approximately(_pointgizmosize, _lastpointgizmosize))
        {
            applydebug();
        }
    }

    private Vector2Int getplayerchunk()
    {
        int _cellx = Mathf.FloorToInt(_player.position.x / _pointsize);
        int _cellz = Mathf.FloorToInt(_player.position.z / _pointsize);

        int _chunkx = Mathf.FloorToInt((float)_cellx / _chunksize);
        int _chunkz = Mathf.FloorToInt((float)_cellz / _chunksize);

        return new Vector2Int(_chunkx, _chunkz);
    }

    private void updatestream()
    {
        removefarchunks();

        if (!_streaming)
        {
            StartCoroutine(generatechunks());
        }
    }

    private IEnumerator generatechunks()
    {
        _streaming = true;

        Vector2Int _center = getplayerchunk();

        List<Vector2Int> _wanted = new List<Vector2Int>();

        for (int _x = -_viewdistance; _x <= _viewdistance; _x++)
        {
            for (int _z = -_viewdistance; _z <= _viewdistance; _z++)
            {
                Vector2Int _coordinate = new Vector2Int(
                _center.x + _x,
                _center.y + _z
                );

                if (_chunks.ContainsKey(_coordinate)) continue;

                _wanted.Add(_coordinate);
            }
        }

        _wanted.Sort((a, b) =>
        {
            int _distancea = (a.x - _center.x) * (a.x - _center.x) + (a.y - _center.y) * (a.y - _center.y);
            int _distanceb = (b.x - _center.x) * (b.x - _center.x) + (b.y - _center.y) * (b.y - _center.y);

            return _distancea.CompareTo(_distanceb);
        });

        int _created = 0;

        for (int _i = 0; _i < _wanted.Count; _i++)
        {
            Vector2Int _coordinate = _wanted[_i];

            if (_chunks.ContainsKey(_coordinate)) continue;

            createchunk(_coordinate);

            _created++;

            if (_chunksperframe > 0 && _created >= _chunksperframe)
            {
                _created = 0;
                yield return null;

                Vector2Int _newcenter = getplayerchunk();

                if (_newcenter != _center)
                {
                    _streaming = false;
                    updatestream();
                    yield break;
                }
            }
        }

        _streaming = false;

        Vector2Int _finalcenter = getplayerchunk();

        if (_finalcenter != _center)
        {
            updatestream();
        }
    }

    private void removefarchunks()
    {
        Vector2Int _center = getplayerchunk();

        List<Vector2Int> _remove = new List<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, pointterrainchunk> _pair in _chunks)
        {
            int _distanceX = Mathf.Abs(_pair.Key.x - _center.x);
            int _distanceZ = Mathf.Abs(_pair.Key.y - _center.y);

            if (_distanceX > _viewdistance || _distanceZ > _viewdistance)
            {
                _remove.Add(_pair.Key);
            }
        }

        for (int _i = 0; _i < _remove.Count; _i++)
        {
            if (_chunks[_remove[_i]] != null)
            {
                Destroy(_chunks[_remove[_i]].gameObject);
            }

            _chunks.Remove(_remove[_i]);
        }
    }

    private void createchunk(Vector2Int _coordinate)
    {
        int _startx = _coordinate.x * _chunksize;
        int _startz = _coordinate.y * _chunksize;

        GameObject _object = new GameObject(
        "chunk_" + _coordinate.x + "_" + _coordinate.y
        );

        _object.transform.SetParent(transform);

        pointterrainchunk _chunk = _object.AddComponent<pointterrainchunk>();

        float _seedx = _seed * 0.12345f;
        float _seedz = _seed * 0.67891f;

        _chunk.initialize(
        _startx,
        _startz,
        _chunksize,
        _chunksize,
        0,
        0,
        _pointsize,
        _baseheight,
        _height,
        _noisescale,
        _flatness,
        _textureworldsize,
        _seedx,
        _seedz,
        _grassmaterial
        );

        _chunk.setdebug(
        _showpoints,
        _showchunks,
        _pointgizmosize
        );

        _chunks.Add(_coordinate, _chunk);
    }

    private void applydebug()
    {
        _lastshowpoints = _showpoints;
        _lastshowchunks = _showchunks;
        _lastpointgizmosize = _pointgizmosize;

        foreach (KeyValuePair<Vector2Int, pointterrainchunk> _pair in _chunks)
        {
            if (_pair.Value == null) continue;

            _pair.Value.setdebug(
            _showpoints,
            _showchunks,
            _pointgizmosize
            );
        }
    }
}
