using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class pointterrain : MonoBehaviour
{
    [Header("Planet")]
    [SerializeField] private Transform _player;
    [SerializeField] private float _planetradius = 500f;
    [SerializeField] private float _surfacegravity = 14f;
    [SerializeField] private float _gravityreferenceheight = 2f;
    [SerializeField] private bool _zerogravity = false;
    [Header("Spawn")]
    [SerializeField] private float _spawnlatitude = 45f;
    [SerializeField] private float _spawnlongitude = 0f;
    [SerializeField] private float _spawnheight = 5f;
    [Header("Streaming")]
    [SerializeField] private int _chunksize = 16;
    [SerializeField] private int _chunksperface = 32;
    [SerializeField] private int _renderchunks = 49;
    [SerializeField] private int _colliderchunks = 25;
    [SerializeField] private int _chunksperframe = 4;
    [Header("Terrain")]
    [SerializeField] private float _baseheight = 2f;
    [SerializeField] private float _height = 6f;
    [SerializeField] private float _noisescale = 4f;
    [SerializeField, Range(0f, 1f)] private float _flatness = 0.9f;
    [Header("Texture")]
    [SerializeField] private float _textureworldsize = 4f;
    [SerializeField] private Material _grassmaterial;
    [Header("Ground")]
    [SerializeField] private LayerMask _terrainmask;
    [Header("Debug")]
    [SerializeField] private bool _showpoints = false;
    [SerializeField] private bool _showchunks = false;
    [SerializeField] private float _pointgizmosize = 0.08f;
    [SerializeField] private int _pointgizmestep = 1;
    [SerializeField] private int _maxgizmopoints = 128;
    [Header("Generation")]
    [SerializeField] private int _seed = 12345;
    private Dictionary<Vector3Int, pointterrainchunk> _chunks = new Dictionary<Vector3Int, pointterrainchunk>();
    private Dictionary<Vector3Int, Vector3> _chunkdirections = new Dictionary<Vector3Int, Vector3>();
    private List<Vector3Int> _orderedchunks = new List<Vector3Int>();
    private Vector3 _lastplayerdirection;
    private Vector3Int _lastcenterchunk;
    private bool _streaming;
    private bool _streamrequest;
    private bool _ready;
    private bool _lastshowpoints;
    private bool _lastshowchunks;
    private float _lastpointgizmosize = -1f;
    private int _lastpointgizmestep = -1;
    private int _lastmaxgizmopoints = -1;
    private CharacterController _charactercontroller;
    private void Awake()
    {
        if (_player == null)
        {
            PlayerController _controller = FindFirstObjectByType<PlayerController>();
            if (_controller != null)
            {
                _player = _controller.transform;
            }
        }
        if (_player != null)
        {
            _charactercontroller = _player.GetComponent<CharacterController>();
            if (_charactercontroller != null)
            {
                _charactercontroller.enabled = false;
            }
        }
        builddirectioncache();
        spawnplayer();
    }
    private void Start()
    {
        StartCoroutine(initializeterrain());
    }
    private void Update()
    {
        if (!_ready || _player == null) return;
        Vector3 _playerdirection = getplayerdirection();
        Vector3Int _currentcenter = getnearestchunk(_playerdirection);
        if (_currentcenter != _lastcenterchunk)
        {
            _lastcenterchunk = _currentcenter;
            updatestream();
        }
        if (_showpoints != _lastshowpoints || _showchunks != _lastshowchunks || !Mathf.Approximately(_pointgizmosize, _lastpointgizmosize) || _pointgizmestep != _lastpointgizmestep || _maxgizmopoints != _lastmaxgizmopoints)
        {
            applydebug();
        }
    }
    private IEnumerator initializeterrain()
    {
        _lastplayerdirection = getplayerdirection();
        _lastcenterchunk = getnearestchunk(_lastplayerdirection);
        yield return StartCoroutine(generatechunks());
        if (_charactercontroller != null)
        {
            _charactercontroller.enabled = true;
        }
        _ready = true;
    }
    private void builddirectioncache()
    {
        _chunkdirections.Clear();
        for (int _face = 0; _face < 6; _face++)
        {
            for (int _x = 0; _x < _chunksperface; _x++)
            {
                for (int _z = 0; _z < _chunksperface; _z++)
                {
                    Vector3Int _coordinate = new Vector3Int(_face, _x, _z);
                    _chunkdirections[_coordinate] = getchunkdirection(_face, _x, _z);
                    _orderedchunks.Add(_coordinate);
                }
            }
        }
    }
    private void spawnplayer()
    {
        if (_player == null) return;
        float _latitude = Mathf.Clamp(_spawnlatitude, -89f, 89f) * Mathf.Deg2Rad;
        float _longitude = _spawnlongitude * Mathf.Deg2Rad;
        Vector3 _direction = new Vector3(
        Mathf.Cos(_latitude) * Mathf.Cos(_longitude),
        Mathf.Sin(_latitude),
        Mathf.Cos(_latitude) * Mathf.Sin(_longitude)
        ).normalized;
        float _terrainheight = getterrainheight(_direction);
        float _radius = _planetradius + _terrainheight + _spawnheight;
        _player.position = transform.position + _direction * _radius;
        Vector3 _forward = Vector3.ProjectOnPlane(Vector3.forward, _direction);
        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward = Vector3.ProjectOnPlane(Vector3.right, _direction);
        }
        _player.rotation = Quaternion.LookRotation(_forward.normalized, _direction);
    }
    private void updatestream()
    {
        if (_streaming)
        {
            _streamrequest = true;
            return;
        }
        StartCoroutine(generatechunks());
    }
    private IEnumerator generatechunks()
    {
        _streaming = true;
        _lastplayerdirection = getplayerdirection();
        _lastcenterchunk = getnearestchunk(_lastplayerdirection);
        List<Vector3Int> _wanted = getclosestchunks(_lastplayerdirection);
        HashSet<Vector3Int> _wantedset = new HashSet<Vector3Int>(_wanted);
        int _created = 0;
        for (int _i = 0; _i < _wanted.Count; _i++)
        {
            Vector3Int _coordinate = _wanted[_i];
            if (_chunks.ContainsKey(_coordinate))
            {
                continue;
            }
            bool _collider = _i < _colliderchunks;
            createchunk(_coordinate, _collider);
            _created++;
            if (_created >= _chunksperframe)
            {
                _created = 0;
                yield return null;
                Vector3 _currentdirection = getplayerdirection();
                Vector3Int _currentcenter = getnearestchunk(_currentdirection);
                if (_currentcenter != _lastcenterchunk)
                {
                    _streaming = false;
                    _streamrequest = false;
                    updatestream();
                    yield break;
                }
            }
        }
        yield return null;
        List<Vector3Int> _colliderlist = _wanted;
        for (int _i = 0; _i < _colliderlist.Count; _i++)
        {
            Vector3Int _coordinate = _colliderlist[_i];
            if (!_chunks.ContainsKey(_coordinate)) continue;
            _chunks[_coordinate].setcolliderenabled(_i < _colliderchunks);
        }
        List<Vector3Int> _remove = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, pointterrainchunk> _pair in _chunks)
        {
            if (!_wantedset.Contains(_pair.Key))
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
        _streaming = false;
        if (_streamrequest)
        {
            _streamrequest = false;
            updatestream();
        }
    }
    private List<Vector3Int> getclosestchunks(Vector3 _direction)
    {
        List<Vector3Int> _result = new List<Vector3Int>();
        for (int _i = 0; _i < _orderedchunks.Count; _i++)
        {
            Vector3Int _coordinate = _orderedchunks[_i];
            float _dot = Vector3.Dot(_direction, _chunkdirections[_coordinate]);
            float _distance = 1f - _dot;
            if (_result.Count < _renderchunks)
            {
                _result.Add(_coordinate);
                if (_result.Count == _renderchunks)
                {
                    _result.Sort((a, b) =>
                    {
                        float _da = 1f - Vector3.Dot(_direction, _chunkdirections[a]);
                        float _db = 1f - Vector3.Dot(_direction, _chunkdirections[b]);
                        return _da.CompareTo(_db);
                    });
                }
            }
            else
            {
                int _lastindex = _result.Count - 1;
                float _lastdistance = 1f - Vector3.Dot(_direction, _chunkdirections[_result[_lastindex]]);
                if (_distance < _lastdistance)
                {
                    _result[_lastindex] = _coordinate;
                    _result.Sort((a, b) =>
                    {
                        float _da = 1f - Vector3.Dot(_direction, _chunkdirections[a]);
                        float _db = 1f - Vector3.Dot(_direction, _chunkdirections[b]);
                        return _da.CompareTo(_db);
                    });
                }
            }
        }
        return _result;
    }
    private Vector3Int getnearestchunk(Vector3 _direction)
    {
        Vector3Int _nearest = new Vector3Int(0, 0, 0);
        float _best = -1f;
        foreach (KeyValuePair<Vector3Int, Vector3> _pair in _chunkdirections)
        {
            float _dot = Vector3.Dot(_direction, _pair.Value);
            if (_dot > _best)
            {
                _best = _dot;
                _nearest = _pair.Key;
            }
        }
        return _nearest;
    }
    private void createchunk(Vector3Int _coordinate, bool _collider)
    {
        if (_chunks.ContainsKey(_coordinate)) return;
        GameObject _object = new GameObject("chunk_" + _coordinate.x + "_" + _coordinate.y + "_" + _coordinate.z);
        _object.transform.SetParent(transform, false);
        int _layer = getterrainlayer();
        if (_layer >= 0)
        {
            _object.layer = _layer;
        }
        pointterrainchunk _chunk = _object.AddComponent<pointterrainchunk>();
        _chunk.initialize(
        _coordinate.x,
        _coordinate.y,
        _coordinate.z,
        _chunksize,
        _chunksperface,
        _planetradius,
        _baseheight,
        _height,
        _noisescale,
        _flatness,
        _textureworldsize,
        _seed,
        _grassmaterial
        );
        _chunk.setdebug(
        _showpoints,
        _showchunks,
        _pointgizmosize,
        _pointgizmestep,
        _maxgizmopoints
        );
        _chunk.setcolliderenabled(_collider);
        _chunks.Add(_coordinate, _chunk);
    }
    private int getterrainlayer()
    {
        for (int _i = 0; _i < 32; _i++)
        {
            if ((_terrainmask.value & (1 << _i)) != 0)
            {
                return _i;
            }
        }
        return -1;
    }
    private Vector3 getplayerdirection()
    {
        Vector3 _offset = _player.position - transform.position;
        if (_offset.sqrMagnitude < 0.000001f)
        {
            return Vector3.up;
        }
        return _offset.normalized;
    }
    private Vector3 getchunkdirection(int _face, int _chunkx, int _chunkz)
    {
        float _u = -1f + ((_chunkx + 0.5f) / (float)_chunksperface) * 2f;
        float _v = -1f + ((_chunkz + 0.5f) / (float)_chunksperface) * 2f;
        return getdirection(_face, _u, _v);
    }
    private static Vector3 getdirection(int _face, float _u, float _v)
    {
        Vector3 _cube;
        switch (_face)
        {
            case 0: _cube = new Vector3(1f, _v, -_u); break;
            case 1: _cube = new Vector3(-1f, _v, _u); break;
            case 2: _cube = new Vector3(_u, 1f, -_v); break;
            case 3: _cube = new Vector3(_u, -1f, _v); break;
            case 4: _cube = new Vector3(_u, _v, 1f); break;
            default: _cube = new Vector3(-_u, _v, -1f); break;
        }
        return _cube.normalized;
    }
    private float getterrainheight(Vector3 _direction)
    {
        float _seedx = _seed * 0.12345f;
        float _seedz = _seed * 0.67891f;
        float _noise1 = Mathf.PerlinNoise((_direction.x + 2f) * _noisescale + _seedx, (_direction.y + 2f) * _noisescale + _seedz);
        float _noise2 = Mathf.PerlinNoise((_direction.y + 2f) * _noisescale + _seedz * 0.71f, (_direction.z + 2f) * _noisescale + _seedx * 0.53f);
        float _noise3 = Mathf.PerlinNoise((_direction.z + 2f) * _noisescale + _seedx * 0.37f, (_direction.x + 2f) * _noisescale + _seedz * 0.83f);
        float _noise = (_noise1 + _noise2 + _noise3) / 3f;
        _noise = Mathf.SmoothStep(0f, 1f, _noise);
        _noise = Mathf.Lerp(_noise, 0.5f, _flatness);
        return _baseheight + (_noise - 0.5f) * _height;
    }
    private void applydebug()
    {
        _lastshowpoints = _showpoints;
        _lastshowchunks = _showchunks;
        _lastpointgizmosize = _pointgizmosize;
        _lastpointgizmestep = _pointgizmestep;
        _lastmaxgizmopoints = _maxgizmopoints;
        foreach (KeyValuePair<Vector3Int, pointterrainchunk> _pair in _chunks)
        {
            if (_pair.Value == null) continue;
            _pair.Value.setdebug(
            _showpoints,
            _showchunks,
            _pointgizmosize,
            _pointgizmestep,
            _maxgizmopoints
            );
        }
    }
    public bool isready()
    {
        return _ready;
    }
    public Vector3 getup(Vector3 _position)
    {
        Vector3 _offset = _position - transform.position;
        if (_offset.sqrMagnitude < 0.000001f)
        {
            return Vector3.up;
        }
        return _offset.normalized;
    }
    public Vector3 getgravitydirection(Vector3 _position)
    {
        return -getup(_position);
    }
    public float getgravitystrength(Vector3 _position)
    {
        if (_zerogravity) return 0f;
        float _referenceradius = _planetradius + _gravityreferenceheight;
        float _distance = Vector3.Distance(_position, transform.position);
        if (_distance < 0.001f) return 0f;
        float _ratio = _referenceradius / _distance;
        return _surfacegravity * _ratio * _ratio;
    }
    public float getsurfaceradius(Vector3 _direction)
    {
        return _planetradius + getterrainheight(_direction);
    }
    public bool getzerogravity()
    {
        return _zerogravity;
    }
    public float getplanetradius()
    {
        return _planetradius;
    }
}