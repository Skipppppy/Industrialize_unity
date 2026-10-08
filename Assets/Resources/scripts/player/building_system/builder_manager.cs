using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class buildingmanager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EasyInv _inventory;
    [SerializeField] private PlayerInput _playerinput;
    [SerializeField] private CursorManager _cursorlogic;
    [SerializeField] private PlayerController _playercontroller;
    [SerializeField] private Camera _camera;
    [SerializeField] private pointterrain _planet;
    [Header("Building Plan")]
    [SerializeField] private int _buildingplanid = 10;
    [Header("Buildings")]
    [SerializeField] private List<buildingdefinition> _buildings = new List<buildingdefinition>();
    [Header("Catalog")]
    [SerializeField] private GameObject _catalog;
    [SerializeField] private Transform _buildingcontent;
    [SerializeField] private buildingcard _buildingcardprefab;
    [SerializeField] private Button _selectbutton;
    [SerializeField] private Button _closebutton;
    [SerializeField] private Button _liquidbutton;
    [SerializeField] private Button _electricitybutton;
    [SerializeField] private Button _wallsbutton;
    [Header("Placement")]
    [SerializeField] private LayerMask _groundmask;
    [SerializeField] private float _placementdistance = 1000f;
    [SerializeField] private float _builddistance = 5f;
    [SerializeField] private float _maxslopeangle = 8f;
    [SerializeField] private float _maxheightdifference = 0.2f;
    [SerializeField] private float _rayheight = 4f;
    [SerializeField] private float _positionsmooth = 30f;
    [SerializeField] private float _rotationsmooth = 30f;
    [Header("Rotation")]
    [SerializeField] private float _rotation15 = 15f;
    [SerializeField] private float _rotation1 = 1f;
    [Header("Ghost")]
    [SerializeField] private Color _bluecolor = new Color(0.1f, 0.45f, 1f, 0.45f);
    [SerializeField] private Color _validcolor = new Color(0.1f, 1f, 0.2f, 0.45f);
    [SerializeField] private Color _invalidcolor = new Color(1f, 0.1f, 0.1f, 0.45f);
    private buildingcard _selectedcard;
    private buildingdefinition _selectedbuilding;
    private GameObject _movingghost;
    private readonly List<buildingghost> _placedghosts = new List<buildingghost>();
    private bool _catalogopen;
    private bool _buildmode;
    private float _ghostrotation;
    private Vector3 _localboundscenter;
    private Vector3 _localboundsextents = Vector3.one * 0.5f;
    private float _localbottom;
    private readonly int _basecolorid = Shader.PropertyToID("_BaseColor");
    private readonly int _colorid = Shader.PropertyToID("_Color");
    private readonly int _basemapid = Shader.PropertyToID("_BaseMap");
    private readonly int _maintexid = Shader.PropertyToID("_MainTex");
    private void Start()
    {
        if (_playercontroller == null)
            _playercontroller = FindFirstObjectByType<PlayerController>();
        if (_catalog != null)
            _catalog.SetActive(false);
        if (_selectbutton != null)
        {
            _selectbutton.onClick.RemoveAllListeners();
            _selectbutton.onClick.AddListener(confirmselection);
            _selectbutton.interactable = false;
        }
        if (_closebutton == null && _catalog != null)
        {
            Button[] _buttons = _catalog.GetComponentsInChildren<Button>(true);
            for (int _i = 0; _i < _buttons.Length; _i++)
            {
                string _name = _buttons[_i].gameObject.name;
                if (_name == "CloseButton" || _name == "closebutton" || _name == "Close" || _name == "X")
                {
                    _closebutton = _buttons[_i];
                    break;
                }
            }
        }
        if (_closebutton != null)
        {
            _closebutton.onClick.RemoveAllListeners();
            _closebutton.onClick.AddListener(closecatalog);
        }
        if (_liquidbutton != null)
        {
            _liquidbutton.onClick.RemoveAllListeners();
            _liquidbutton.onClick.AddListener(showliquid);
        }
        if (_electricitybutton != null)
        {
            _electricitybutton.onClick.RemoveAllListeners();
            _electricitybutton.onClick.AddListener(showelectricity);
        }
        if (_wallsbutton != null)
        {
            _wallsbutton.onClick.RemoveAllListeners();
            _wallsbutton.onClick.AddListener(showwalls);
        }
        setcatalogcursor(false);
    }
    private void Update()
    {
        if (_catalogopen)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                closecatalog();
            return;
        }
        if (_buildmode)
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                cancelbuildmode();
                return;
            }
            if (_movingghost != null)
            {
                updaterotation();
                updatemovingghost();
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    placeblueprint();
            }
            updateblueprintbuilding();
            return;
        }
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && hasbuildingplan())
        {
            opencatalog();
        }
    }
    private void opencatalog()
    {
        if (!hasbuildingplan())
            return;
        _catalogopen = true;
        _selectedcard = null;
        _selectedbuilding = null;
        setselectbutton(false);
        if (_catalog != null)
            _catalog.SetActive(true);
        setcatalogcursor(true);
        showliquid();
    }
    private void closecatalog()
    {
        _catalogopen = false;
        if (_catalog != null)
            _catalog.SetActive(false);
        if (_selectedcard != null)
            _selectedcard.setselected(false);
        _selectedcard = null;
        _selectedbuilding = null;
        setselectbutton(false);
        setcatalogcursor(false);
    }
    private void setcatalogcursor(bool _value)
    {
        if (_cursorlogic != null)
            _cursorlogic.uiMode = false;
        Cursor.visible = _value;
        Cursor.lockState = _value ? CursorLockMode.None : CursorLockMode.Locked;
    }
    public void showliquid()
    {
        setcategory(buildingcategory.liquid);
    }
    public void showelectricity()
    {
        setcategory(buildingcategory.electricity);
    }
    public void showwalls()
    {
        setcategory(buildingcategory.wallsfloors);
    }
    private void setcategory(buildingcategory _category)
    {
        if (_buildingcontent == null || _buildingcardprefab == null)
            return;
        for (int _i = _buildingcontent.childCount - 1; _i >= 0; _i--)
            Destroy(_buildingcontent.GetChild(_i).gameObject);
        if (_selectedcard != null)
            _selectedcard.setselected(false);
        _selectedcard = null;
        _selectedbuilding = null;
        setselectbutton(false);
        for (int _i = 0; _i < _buildings.Count; _i++)
        {
            buildingdefinition _definition = _buildings[_i];
            if (_definition == null || _definition._category != _category)
                continue;
            buildingcard _card = Instantiate(_buildingcardprefab, _buildingcontent);
            _card.initialize(_definition, this);
        }
    }
    public void selectcard(buildingcard _card, buildingdefinition _building)
    {
        if (_card == null || _building == null)
            return;
        if (_selectedcard != null && _selectedcard != _card)
            _selectedcard.setselected(false);
        _selectedcard = _card;
        _selectedbuilding = _building;
        _selectedcard.setselected(true);
        setselectbutton(true);
    }
    private void setselectbutton(bool _enabled)
    {
        if (_selectbutton == null)
            return;
        _selectbutton.interactable = _enabled;
        Image _image = _selectbutton.GetComponent<Image>();
        if (_image != null)
            _image.color = _enabled ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
    }
    private void confirmselection()
    {
        if (_selectedbuilding == null || _selectedbuilding._prefab == null)
            return;
        _catalogopen = false;
        if (_catalog != null)
            _catalog.SetActive(false);
        if (_selectedcard != null)
            _selectedcard.setselected(false);
        _selectedcard = null;
        _ghostrotation = 0f;
        _buildmode = true;
        createMovingGhost();
        setbuildmode(true);
    }
    private void setbuildmode(bool _value)
    {
        if (_playercontroller != null)
            _playercontroller.setbuildinguimode(_value);
        if (_cursorlogic != null)
            _cursorlogic.uiMode = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void cancelbuildmode()
    {
        destroyghost();
        _buildmode = false;
        _selectedcard = null;
        _selectedbuilding = null;
        if (_selectbutton != null)
            _selectbutton.interactable = false;
        setbuildmode(false);
    }
    private void updaterotation()
    {
        if (_movingghost == null || Mouse.current == null)
            return;
        float _scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(_scroll) < 0.01f)
            return;
        bool _r = Keyboard.current != null && Keyboard.current.rKey.isPressed;
        float _step = _r ? _rotation1 : _rotation15;
        _ghostrotation += _scroll > 0f ? _step : -_step;
        _ghostrotation = Mathf.Repeat(_ghostrotation, 360f);
    }
    private void updatemovingghost()
    {
        if (_movingghost == null || _selectedbuilding == null || _camera == null)
            return;
        Ray _ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(_ray, out RaycastHit _hit, _placementdistance, _groundmask, QueryTriggerInteraction.Ignore))
        {
            setghostcolor(_invalidcolor, _movingghost);
            return;
        }
        Vector3 _up = _planet != null ? _planet.getup(_hit.point) : _hit.normal;
        if (_up.sqrMagnitude < 0.001f)
            _up = Vector3.up;
        _up.Normalize();
        Vector3 _forward = Vector3.ProjectOnPlane(_camera.transform.forward, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(_camera.transform.right, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(Vector3.forward, _up);
        _forward.Normalize();
        Quaternion _surfaceRotation = Quaternion.LookRotation(_forward, _up);
        Quaternion _rotation = Quaternion.AngleAxis(_ghostrotation, _up) * _surfaceRotation;
        Vector3 _position = getgroundposition(_hit.point, _up, _rotation);
        bool _valid = checksurface(_position, _up, _rotation) && checkblueprintoverlap(_position, _rotation) == false;
        _movingghost.SetActive(true);
        _movingghost.transform.position = Vector3.Lerp(_movingghost.transform.position, _position, _positionsmooth * Time.deltaTime);
        _movingghost.transform.rotation = Quaternion.Slerp(_movingghost.transform.rotation, _rotation, _rotationsmooth * Time.deltaTime);
        setghostcolor(_valid ? _bluecolor : _invalidcolor, _movingghost);
    }
    private Vector3 getgroundposition(Vector3 _hitpoint, Vector3 _up, Quaternion _rotation)
    {
        Vector3 _position = _hitpoint - _rotation * new Vector3(_localboundscenter.x, _localbottom, _localboundscenter.z);
        Vector3[] _samples = getgroundsamples(_position, _rotation);
        float _offset = 0f;
        for (int _i = 0; _i < _samples.Length; _i++)
        {
            Vector3 _origin = _samples[_i] + _up * _rayheight;
            if (!Physics.Raycast(_origin, -_up, out RaycastHit _hit, _rayheight * 2f, _groundmask, QueryTriggerInteraction.Ignore))
                continue;
            float _difference = Vector3.Dot(_hit.point - _hitpoint, _up);
            if (_difference > _offset)
                _offset = _difference;
        }
        return _position + _up * _offset;
    }
    private Vector3[] getgroundsamples(Vector3 _position, Quaternion _rotation)
    {
        Vector3 _min = new Vector3(_localboundscenter.x - _localboundsextents.x, _localbottom, _localboundscenter.z - _localboundsextents.z);
        Vector3 _max = new Vector3(_localboundscenter.x + _localboundsextents.x, _localbottom, _localboundscenter.z + _localboundsextents.z);
        return new Vector3[]
        {
_position+_rotation*new Vector3(_localboundscenter.x,_localbottom,_localboundscenter.z),
_position+_rotation*new Vector3(_min.x,_localbottom,_min.z),
_position+_rotation*new Vector3(_min.x,_localbottom,_max.z),
_position+_rotation*new Vector3(_max.x,_localbottom,_min.z),
_position+_rotation*new Vector3(_max.x,_localbottom,_max.z)
        };
    }
    private bool checksurface(Vector3 _position, Vector3 _up, Quaternion _rotation)
    {
        if (_selectedbuilding == null)
            return false;
        Vector3[] _samples = getgroundsamples(_position, _rotation);
        Vector3 _centerpoint = Vector3.zero;
        bool _havecenter = false;
        for (int _i = 0; _i < _samples.Length; _i++)
        {
            Vector3 _origin = _samples[_i] + _up * _rayheight;
            if (!Physics.Raycast(_origin, -_up, out RaycastHit _hit, _rayheight * 2f, _groundmask, QueryTriggerInteraction.Ignore))
                return false;
            if (!_havecenter)
            {
                _centerpoint = _hit.point;
                _havecenter = true;
            }
            if (Vector3.Angle(_hit.normal, _up) > _maxslopeangle)
                return false;
            float _heightdifference = Vector3.Dot(_hit.point - _centerpoint, _up);
            if (Mathf.Abs(_heightdifference) > _maxheightdifference)
                return false;
        }
        return true;
    }
    private bool isplacementvalid()
    {
        if (_movingghost == null || _selectedbuilding == null)
            return false;
        Vector3 _position = _movingghost.transform.position;
        Vector3 _up = _movingghost.transform.up;
        if (Vector3.Distance(transform.position, _position) > _builddistance)
            return false;
        if (!checksurface(_position, _up, _movingghost.transform.rotation))
            return false;
        if (checkblueprintoverlap(_position, _movingghost.transform.rotation))
            return false;
        return true;
    }
    private bool checkblueprintoverlap(Vector3 _position, Quaternion _rotation)
    {
        Vector3 _center = _position + _rotation * _localboundscenter;
        Collider[] _hits = Physics.OverlapBox(_center, _localboundsextents + Vector3.one * 0.02f, _rotation, ~0, QueryTriggerInteraction.Collide);
        for (int _i = 0; _i < _hits.Length; _i++)
        {
            if (_hits[_i] == null)
                continue;
            buildingghost _ghost = _hits[_i].GetComponentInParent<buildingghost>();
            if (_ghost == null)
                continue;
            if (!_placedghosts.Contains(_ghost))
                continue;
            return true;
        }
        return false;
    }
    private void placeblueprint()
    {
        if (_movingghost == null || _selectedbuilding == null)
            return;
        if (!isplacementvalid())
            return;
        GameObject _blueprint = Instantiate(_selectedbuilding._prefab, _movingghost.transform.position, _movingghost.transform.rotation);
        setupblueprint(_blueprint);
    }
    private void setupblueprint(GameObject _blueprint)
    {
        if (_blueprint == null)
            return;
        MonoBehaviour[] _scripts = _blueprint.GetComponentsInChildren<MonoBehaviour>(true);
        for (int _i = 0; _i < _scripts.Length; _i++)
            _scripts[_i].enabled = false;
        Collider[] _colliders = _blueprint.GetComponentsInChildren<Collider>(true);
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            _colliders[_i].enabled = true;
            _colliders[_i].isTrigger = true;
        }
        Rigidbody[] _rigidbodies = _blueprint.GetComponentsInChildren<Rigidbody>(true);
        for (int _i = 0; _i < _rigidbodies.Length; _i++)
        {
            _rigidbodies[_i].isKinematic = true;
            _rigidbodies[_i].useGravity = false;
            _rigidbodies[_i].detectCollisions = true;
        }
        Renderer[] _renderers = _blueprint.GetComponentsInChildren<Renderer>(true);
        for (int _i = 0; _i < _renderers.Length; _i++)
            applyghostmaterial(_renderers[_i]);
        buildingghost _marker = _blueprint.GetComponent<buildingghost>();
        if (_marker == null)
            _marker = _blueprint.AddComponent<buildingghost>();
        _marker.initialize(_selectedbuilding, this);
        if (!_placedghosts.Contains(_marker))
            _placedghosts.Add(_marker);
        setghostcolor(_bluecolor, _blueprint);
    }
    private void createMovingGhost()
    {
        destroyghost();
        if (_selectedbuilding == null || _selectedbuilding._prefab == null)
            return;
        _movingghost = Instantiate(_selectedbuilding._prefab);
        _movingghost.name = "building_moving_ghost";
        setupmovingghost(_movingghost);
        _movingghost.SetActive(true);
        if (_camera != null)
        {
            _movingghost.transform.position = _camera.transform.position + _camera.transform.forward * Mathf.Min(2f, _builddistance);
            _movingghost.transform.rotation = _camera.transform.rotation;
        }
        _calculatebounds();
        setghostcolor(_bluecolor, _movingghost);
    }
    private void setupmovingghost(GameObject _ghost)
    {
        if (_ghost == null)
            return;
        Collider[] _colliders = _ghost.GetComponentsInChildren<Collider>(true);
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            _colliders[_i].enabled = true;
            _colliders[_i].isTrigger = true;
        }
        Rigidbody[] _rigidbodies = _ghost.GetComponentsInChildren<Rigidbody>(true);
        for (int _i = 0; _i < _rigidbodies.Length; _i++)
        {
            _rigidbodies[_i].isKinematic = true;
            _rigidbodies[_i].useGravity = false;
            _rigidbodies[_i].detectCollisions = false;
        }
        MonoBehaviour[] _scripts = _ghost.GetComponentsInChildren<MonoBehaviour>(true);
        for (int _i = 0; _i < _scripts.Length; _i++)
            _scripts[_i].enabled = false;
        Renderer[] _renderers = _ghost.GetComponentsInChildren<Renderer>(true);
        for (int _i = 0; _i < _renderers.Length; _i++)
            applyghostmaterial(_renderers[_i]);
    }
    private void applyghostmaterial(Renderer _renderer)
    {
        if (_renderer == null)
            return;
        Material[] _materials = _renderer.materials;
        for (int _i = 0; _i < _materials.Length; _i++)
        {
            if (_materials[_i] == null)
                continue;
            Material _material = _materials[_i];
            if (_material.HasProperty(_basemapid))
                _material.SetTexture(_basemapid, null);
            if (_material.HasProperty(_maintexid))
                _material.SetTexture(_maintexid, null);
            if (_material.HasProperty(_basecolorid))
                _material.SetColor(_basecolorid, _bluecolor);
            if (_material.HasProperty(_colorid))
                _material.SetColor(_colorid, _bluecolor);
            if (_material.HasProperty("_Surface"))
                _material.SetFloat("_Surface", 1f);
            if (_material.HasProperty("_Blend"))
                _material.SetFloat("_Blend", 0f);
            if (_material.HasProperty("_AlphaClip"))
                _material.SetFloat("_AlphaClip", 0f);
            if (_material.HasProperty("_Mode"))
                _material.SetFloat("_Mode", 3f);
            if (_material.HasProperty("_SrcBlend"))
                _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (_material.HasProperty("_DstBlend"))
                _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (_material.HasProperty("_ZWrite"))
                _material.SetInt("_ZWrite", 0);
            _material.EnableKeyword("_ALPHABLEND_ON");
            _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _material.renderQueue = 3000;
        }
    }
    private void setghostcolor(Color _color, GameObject _object)
    {
        if (_object == null)
            return;
        Renderer[] _renderers = _object.GetComponentsInChildren<Renderer>(true);
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            Material[] _materials = _renderers[_i].materials;
            for (int _j = 0; _j < _materials.Length; _j++)
            {
                if (_materials[_j] == null)
                    continue;
                if (_materials[_j].HasProperty(_basecolorid))
                    _materials[_j].SetColor(_basecolorid, _color);
                if (_materials[_j].HasProperty(_colorid))
                    _materials[_j].SetColor(_colorid, _color);
            }
        }
    }
    private void destroyghost()
    {
        if (_movingghost != null)
        {
            Destroy(_movingghost);
            _movingghost = null;
        }
    }
    private void updateblueprintbuilding()
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.isPressed || _camera == null)
            return;
        Ray _ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] _hits = Physics.RaycastAll(_ray, _builddistance, ~0, QueryTriggerInteraction.Collide);
        buildingghost _target = null;
        float _distance = float.MaxValue;
        for (int _i = 0; _i < _hits.Length; _i++)
        {
            if (_hits[_i].collider == null)
                continue;
            buildingghost _ghost = _hits[_i].collider.GetComponentInParent<buildingghost>();
            if (_ghost == null || !_placedghosts.Contains(_ghost))
                continue;
            if (_hits[_i].distance < _distance)
            {
                _distance = _hits[_i].distance;
                _target = _ghost;
            }
        }
        if (_target == null || _target.getbuilding() == null)
            return;
        if (_playercontroller != null && Vector3.Distance(_playercontroller.transform.position, _target.transform.position) > _builddistance)
            return;
        buildingdefinition _definition = _target.getbuilding();
        if (!hasallresources(_definition))
            return;
        _target.addprogress(Time.deltaTime);
        if (_target.getprogress() >= Mathf.Max(0.01f, _definition._buildtime))
            finishblueprint(_target);
    }
    private bool hasallresources(buildingdefinition _building)
    {
        if (_building == null)
            return false;
        if (_building._cost == null || _building._cost.Length == 0)
            return true;
        if (_inventory == null)
            return false;
        for (int _i = 0; _i < _building._cost.Length; _i++)
        {
            buildingcost _cost = _building._cost[_i];
            if (_cost == null || _cost._amount <= 0)
                continue;
            if (_inventory.GetTotalItemAmount(_cost._itemid) < _cost._amount)
                return false;
        }
        return true;
    }
    private void finishblueprint(buildingghost _blueprint)
    {
        if (_blueprint == null)
            return;
        buildingdefinition _definition = _blueprint.getbuilding();
        if (_definition == null || _definition._prefab == null)
            return;
        if (!hasallresources(_definition))
        {
            _blueprint.resetprogress();
            return;
        }
        if (_definition._cost != null)
        {
            for (int _i = 0; _i < _definition._cost.Length; _i++)
            {
                buildingcost _cost = _definition._cost[_i];
                if (_cost == null || _cost._amount <= 0)
                    continue;
                if (!_inventory.RemoveItemAmount(_cost._itemid, _cost._amount))
                    return;
            }
        }
        Vector3 _position = _blueprint.transform.position;
        Quaternion _rotation = _blueprint.transform.rotation;
        _placedghosts.Remove(_blueprint);
        Destroy(_blueprint.gameObject);
        Instantiate(_definition._prefab, _position, _rotation);
        if (_inventory != null)
            _inventory.RefreshUI();
    }
    private void _calculatebounds()
    {
        if (_movingghost == null)
            return;
        Renderer[] _renderers = _movingghost.GetComponentsInChildren<Renderer>(true);
        Collider[] _colliders = _movingghost.GetComponentsInChildren<Collider>(true);
        Bounds _worldbounds = new Bounds(_movingghost.transform.position, Vector3.zero);
        bool _hasbounds = false;
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            if (!_hasbounds)
            {
                _worldbounds = _renderers[_i].bounds;
                _hasbounds = true;
            }
            else
                _worldbounds.Encapsulate(_renderers[_i].bounds);
        }
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            if (!_hasbounds)
            {
                _worldbounds = _colliders[_i].bounds;
                _hasbounds = true;
            }
            else
                _worldbounds.Encapsulate(_colliders[_i].bounds);
        }
        if (!_hasbounds)
        {
            _localboundscenter = Vector3.zero;
            _localboundsextents = Vector3.one * 0.5f;
            _localbottom = -0.5f;
            return;
        }
        Vector3[] _corners = getboundscorners(_worldbounds);
        Bounds _local = new Bounds(_movingghost.transform.InverseTransformPoint(_corners[0]), Vector3.zero);
        for (int _i = 1; _i < _corners.Length; _i++)
            _local.Encapsulate(_movingghost.transform.InverseTransformPoint(_corners[_i]));
        _localboundscenter = _local.center;
        _localboundsextents = _local.extents;
        _localbottom = _local.min.y;
    }
    private Vector3[] getboundscorners(Bounds _bounds)
    {
        Vector3 _min = _bounds.min;
        Vector3 _max = _bounds.max;
        return new Vector3[]
        {
new Vector3(_min.x,_min.y,_min.z),
new Vector3(_min.x,_min.y,_max.z),
new Vector3(_min.x,_max.y,_min.z),
new Vector3(_min.x,_max.y,_max.z),
new Vector3(_max.x,_min.y,_min.z),
new Vector3(_max.x,_min.y,_max.z),
new Vector3(_max.x,_max.y,_min.z),
new Vector3(_max.x,_max.y,_max.z)
        };
    }
    private bool hasbuildingplan()
    {
        if (_inventory == null)
            return false;
        itemz _item = _inventory.getmainhanditem();
        if (_item == null || _item.definition == null)
            return false;
        return _item.definition.id == _buildingplanid;
    }
    private void OnDestroy()
    {
        if (_movingghost != null)
            Destroy(_movingghost);
    }
}
