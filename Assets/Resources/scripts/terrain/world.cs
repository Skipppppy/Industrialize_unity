using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-100)]
public class voxelworld : MonoBehaviour
{
    [Header("Planet")]
    [SerializeField] private float _planetradius = 50000f;
    [SerializeField] private float _surfaceplaneheight = 0f;
    [SerializeField] private float _minterrainheight = -100f;
    [SerializeField] private float _maxterrainheight = 350f;
    [Header("Spawn")]
    [SerializeField] private bool _movetoplanetspawn = true;
    [SerializeField] private float _spawnlatitude = 0f;
    [SerializeField] private float _spawnlongitude = 0f;
    [SerializeField] private float _spawnheight = 0.02f;
    [Header("Terrain")]
    [SerializeField] private int _seed = 12345;
    [SerializeField] private float _terrainfrequency = 0.00012f;
    [SerializeField] private float _terrainamplitude = 180f;
    [SerializeField] private int _terrainoctaves = 4;
    [SerializeField] private float _terrainflatness = 12f;
    [Header("Lowlands")]
    [SerializeField] private float _lowlandfrequency = 0.000055f;
    [SerializeField] private float _lowlandthreshold = 0.60f;
    [SerializeField] private float _lowlandstrength = 0.8f;
    [Header("Rivers")]
    [SerializeField] private bool _generateriverdepressions = true;
    [SerializeField] private float _riverfrequency = 0.00015f;
    [SerializeField, Range(0.001f, 0.1f)] private float _riverwidth = 0.018f;
    [SerializeField] private float _riverdepth = 25f;
    [Header("Surface Material")]
    [SerializeField] private float _dirtheightthreshold = -40f;
    [Header("Texture")]
    [SerializeField] private float _textureworldsize = 8f;
    [Header("LOD")]
    [SerializeField] private int _patchresolution = 17;
    [SerializeField] private int _maxlod = 11;
    [SerializeField] private float _lodfactor = 1.15f;
    [SerializeField] private float _colliderdistance = 5000f;
    [SerializeField] private int _colliderstartlod = 0;
    [Header("Camera")]
    [SerializeField] private float _camerafarclip = 300000f;
    [Header("Player")]
    [SerializeField] private Transform _player;
    [Header("Materials")]
    public Material dirtmaterial;
    public Material grassmaterial;
    public Material sandmaterial;
    private readonly List<planetpatch> _rootpatches = new List<planetpatch>();
    private float _lodtimer;
    public float planetradius => _planetradius;
    public float maxterrainheight => _maxterrainheight;
    public int patchresolution => _patchresolution;
    public int maxlod => _maxlod;
    public float lodfactor => _lodfactor;
    public float textureworldsize => _textureworldsize;
    public float colliderdistance => _colliderdistance;
    public int colliderstartlod => _colliderstartlod;
    private void Start()
    {
        if (_player == null)
        {
            GameObject _playerobject = GameObject.FindGameObjectWithTag("Player");
            if (_playerobject != null)
                _player = _playerobject.transform;
        }
        createplanet();
        if (_movetoplanetspawn && _player != null)
        {
            Vector3 _direction = getdirectionfromlatlon(_spawnlatitude, _spawnlongitude);
            CharacterController _controller = _player.GetComponent<CharacterController>();
            float _height = 1f;
            float _skin = 0f;
            if (_controller != null)
            {
                _height = _controller.height * 0.5f;
                _skin = _controller.skinWidth;
            }
            alignplayer(_player, _direction);
            _player.position = getsurfaceposition(_direction, _height + _skin + _spawnheight);
            Physics.SyncTransforms();
        }
        updatecamera();
    }
    private void Update()
    {
        if (_player == null)
            return;
        _lodtimer += Time.deltaTime;
        if (_lodtimer >= 0.1f)
        {
            _lodtimer = 0f;
            for (int _i = 0; _i < _rootpatches.Count; _i++)
            {
                if (_rootpatches[_i] != null)
                    _rootpatches[_i].updatelod(_player.position);
            }
        }
        updatecamera();
    }
    private void createplanet()
    {
        if (_rootpatches.Count > 0)
            return;
        for (int _i = 0; _i < 6; _i++)
        {
            GameObject _object = new GameObject($"PlanetFace_{_i}");
            _object.transform.SetParent(transform);
            _object.transform.localPosition = Vector3.zero;
            _object.transform.localRotation = Quaternion.identity;
            planetpatch _patch = _object.AddComponent<planetpatch>();
            _patch.initialize(this, _i, 0, 0, 0);
            _rootpatches.Add(_patch);
        }
    }
    private void updatecamera()
    {
        if (_player == null)
            return;
        Camera _camera = _player.GetComponentInChildren<Camera>();
        if (_camera == null)
            return;
        _camera.farClipPlane = Mathf.Max(_camera.farClipPlane, _camerafarclip);
        if (_camera.nearClipPlane < 0.03f)
            _camera.nearClipPlane = 0.03f;
    }
    private void alignplayer(Transform _player, Vector3 _up)
    {
        if (_up.sqrMagnitude < 0.001f)
            return;
        _up.Normalize();
        Vector3 _forward = Vector3.ProjectOnPlane(_player.forward, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(Vector3.forward, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(Vector3.right, _up);
        _forward.Normalize();
        _player.rotation = Quaternion.LookRotation(_forward, _up);
    }
    public Vector3 getdirectionfromlatlon(float _latitude, float _longitude)
    {
        float _lat = _latitude * Mathf.Deg2Rad;
        float _lon = _longitude * Mathf.Deg2Rad;
        float _coslat = Mathf.Cos(_lat);
        return new Vector3(_coslat * Mathf.Cos(_lon), Mathf.Sin(_lat), _coslat * Mathf.Sin(_lon)).normalized;
    }
    public Vector3 getsurfaceposition(Vector3 _direction, float _offset = 0f)
    {
        _direction.Normalize();
        float _height = getterrainheight(_direction);
        return _direction * (_planetradius + _height + _offset);
    }
    public Vector3 cubedirection(int _face, float _u, float _v)
    {
        Vector3 _cube;
        switch (_face)
        {
            case 0:
                _cube = new Vector3(1f, _v, -_u);
                break;
            case 1:
                _cube = new Vector3(-1f, _v, _u);
                break;
            case 2:
                _cube = new Vector3(_u, 1f, -_v);
                break;
            case 3:
                _cube = new Vector3(_u, -1f, _v);
                break;
            case 4:
                _cube = new Vector3(_u, _v, 1f);
                break;
            default:
                _cube = new Vector3(-_u, _v, -1f);
                break;
        }
        return _cube.normalized;
    }
    public float getterrainheight(Vector3 _direction)
    {
        _direction.Normalize();
        Vector3 _position = _direction * _planetradius;
        float _value = planetnoise.fbm(_position, _terrainfrequency, _terrainoctaves, _seed);
        float _lowland = planetnoise.value(_position * _lowlandfrequency, _seed + 100);
        if (_lowland > _lowlandthreshold)
        {
            float _amount = Mathf.InverseLerp(_lowlandthreshold, 1f, _lowland);
            _amount = Mathf.SmoothStep(0f, 1f, _amount);
            _value = Mathf.Lerp(_value, -1f, _amount * _lowlandstrength);
        }
        float _detail = planetnoise.value(_position * 0.0008f, _seed + 200);
        _detail = (_detail - 0.5f) * 2f;
        float _height = _surfaceplaneheight + _value * _terrainamplitude + _detail * _terrainflatness;
        if (_generateriverdepressions)
        {
            float _river = planetnoise.value(_position * _riverfrequency, _seed + 300);
            float _distance = Mathf.Abs(_river - 0.5f) * 2f;
            if (_distance < _riverwidth)
            {
                float _amount = 1f - Mathf.Clamp01(_distance / _riverwidth);
                _amount = Mathf.SmoothStep(0f, 1f, _amount);
                _height -= _amount * _riverdepth;
            }
        }
        return Mathf.Clamp(_height, _minterrainheight, _maxterrainheight);
    }
    public voxelmaterial getsurfacematerial(Vector3 _direction)
    {
        _direction.Normalize();
        float _height = getterrainheight(_direction);
        if (isriver(_direction))
            return voxelmaterial.sand;
        if (_height < _dirtheightthreshold)
            return voxelmaterial.dirt;
        return voxelmaterial.grass;
    }
    private bool isriver(Vector3 _direction)
    {
        if (!_generateriverdepressions)
            return false;
        Vector3 _position = _direction * _planetradius;
        float _river = planetnoise.value(_position * _riverfrequency, _seed + 300);
        float _distance = Mathf.Abs(_river - 0.5f) * 2f;
        return _distance < _riverwidth;
    }
    public Material[] getmaterials()
    {
        return new Material[]
        {
            dirtmaterial,
            grassmaterial,
            sandmaterial
        };
    }
    public int getmaterialindex(voxelmaterial _material)
    {
        switch (_material)
        {
            case voxelmaterial.dirt:
                return 0;
            case voxelmaterial.grass:
                return 1;
            case voxelmaterial.sand:
                return 2;
        }
        return 0;
    }
}