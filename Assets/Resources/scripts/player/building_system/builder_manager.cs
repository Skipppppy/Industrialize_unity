
using System.Collections.Generic;
using System.Reflection;
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
    [SerializeField] private float _rayheight = 8f;
    [SerializeField] private float _maxslopeangle = 8f;
    [SerializeField] private float _maxheightdifference = 0.25f;
    [SerializeField] private float _positionsmooth = 25f;
    [SerializeField] private float _rotationsmooth = 25f;
    [SerializeField] private float _rotationstep = 15f;
    [Header("Ghost Colors")]
    [SerializeField] private Color _bluecolor = new Color(0.1f, 0.45f, 1f, 0.45f);
    [SerializeField] private Color _validcolor = new Color(0.1f, 1f, 0.2f, 0.45f);
    [SerializeField] private Color _invalidcolor = new Color(1f, 0.1f, 0.1f, 0.45f);
    private buildingcard _selectedcard;
    private buildingdefinition _selectedbuilding;
    private GameObject _movingghost;
    private readonly List<buildingghost> _placedghosts = new List<buildingghost>();
    private readonly Dictionary<GameObject, List<Material>> _ghostmaterials = new Dictionary<GameObject, List<Material>>();
    private bool _catalogopen;
    private bool _placementactive;
    private bool _cursorlogicwasenabled;
    private bool _previewhasground;
    private bool _previewvalid;
    private int _placementstartframe;
    private float _ghostrotation;
    private Vector3 _localboundscenter;
    private Vector3 _localboundsextents = Vector3.one * 0.5f;
    private Vector3 _localbottomcenter;
    private Vector3 _previewtargetposition;
    private Quaternion _previewtargetrotation;
    [SerializeField] private float _catalogsensitivity = 0.03f;
    private readonly Dictionary<FieldInfo, object> _savedplayerfields = new Dictionary<FieldInfo, object>();
    private readonly Dictionary<InputAction, bool> _savedactionstates = new Dictionary<InputAction, bool>();
    private readonly string[] _movementactionnames = { "Move", "Jump", "Sprint" };
    [Header("Construction")]
    [SerializeField] private neardysystem _neardysystem;
    [SerializeField] private float _builddistance = 5f;
    private InputAction _pickupaction;
    private buildingghost _constructiontarget;
    [Header("Required Resources HUD")]
    [SerializeField] private bool _showrequirements = true;
    [SerializeField] private float _requirementswidth = 360f;
    [SerializeField] private float _requirementsmargin = 12f;
    [SerializeField] private float _requirementsrefreshinterval = 0.25f;
    private buildingghost _aimedblueprint;
    private readonly Dictionary<int, string> _resourcenames = new Dictionary<int, string>();
    private readonly Dictionary<int, int> _aimedrequiredresources = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _aimedavailableresources = new Dictionary<int, int>();
    private string _aimedbuildingname = string.Empty;
    private float _nextrequirementsrefresh;
    private GUIStyle _requirementstitlestyle;
    private GUIStyle _requirementslinestyle;
    [Header("Construction Debug")]
    [SerializeField] private bool _debugconstruction = false;
    [SerializeField] private bool _showconstructiondebughud = false;
    [SerializeField] private float _debugloginterval = 0.5f;
    private float _nextconstructiondebuglog;
    private bool _lastdebuge;
    private string _constructiondebuginput = "Input has not been checked yet.";
    private string _constructiondebugstatus = "Waiting for E.";
    private string _constructiondebugrayinfo = "Ray has not been checked yet.";
    private float _constructiondebugprogress;
    private float _constructiondebugrequired;
    private GUIStyle _constructiondebugstyle;
    private void Awake()
    {
        if (_playercontroller == null)
            _playercontroller = FindFirstObjectByType<PlayerController>();
        if (_playerinput == null && _playercontroller != null)
            _playerinput = _playercontroller.playerInput;
        if (_camera == null)
            _camera = Camera.main;
        if (_planet == null)
            _planet = FindFirstObjectByType<pointterrain>();
        if (_inventory == null)
            _inventory = FindFirstObjectByType<EasyInv>();
        if (_cursorlogic == null && _playercontroller != null)
            _cursorlogic = _playercontroller.cursorlogic;
        if (_catalog != null)
            _catalog.SetActive(false);
        if (_closebutton == null && _catalog != null)
            _closebutton = findclosebutton();
        if (_selectbutton != null)
        {
            _selectbutton.onClick.RemoveListener(confirmselection);
            _selectbutton.onClick.AddListener(confirmselection);
            _selectbutton.interactable = false;
        }
        if (_closebutton != null)
        {
            _closebutton.onClick.RemoveListener(closecatalogbutton);
            _closebutton.onClick.AddListener(closecatalogbutton);
        }
        if (_liquidbutton != null)
        {
            _liquidbutton.onClick.RemoveListener(showliquid);
            _liquidbutton.onClick.AddListener(showliquid);
        }
        if (_electricitybutton != null)
        {
            _electricitybutton.onClick.RemoveListener(showelectricity);
            _electricitybutton.onClick.AddListener(showelectricity);
        }
        if (_wallsbutton != null)
        {
            _wallsbutton.onClick.RemoveListener(showwalls);
            _wallsbutton.onClick.AddListener(showwalls);
        }
        if (_neardysystem == null)
            _neardysystem = FindFirstObjectByType<neardysystem>();
        loadresourcenames();
        if (_playerinput != null && _playerinput.actions != null)
        {
            _pickupaction = _playerinput.actions.FindAction("PickUp", false);
            if (_pickupaction == null)
            {
                Debug.LogError("[BUILD DEBUG] Action 'PickUp' was not found in PlayerInput. The physical E key will still be checked directly.", this);
            }
            else
            {
                if (!_pickupaction.enabled)
                    _pickupaction.Enable();
                Debug.Log($"[BUILD DEBUG] PickUp action found. Enabled={_pickupaction.enabled}; bindings={_pickupaction.bindings.Count}.", this);
            }
        }
        else
        {
            Debug.LogError("[BUILD DEBUG] PlayerInput is not assigned and was not found through PlayerController. Only the physical E key will be checked.", this);
        }
    }
    private void Update()
    {
        if (_catalogopen)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                closecatalog();
                return;
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                closecatalog();
                return;
            }
            return;
        }
        if (_placementactive)
        {
            updaterotation();
            updatemovingghost();
            updateaimedblueprintrequirements();
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (_aimedblueprint != null)
                    cancelplacedblueprint(_aimedblueprint);
                else
                    cancelplacement();
                return;
            }
            if (Time.frameCount > _placementstartframe && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                placeblueprint();
            updateblueprintbuilding();
            return;
        }
        updateaimedblueprintrequirements();
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (_aimedblueprint != null)
            {
                cancelplacedblueprint(_aimedblueprint);
                return;
            }
            if (hasbuildingplan())
            {
                opencatalog();
                return;
            }
        }
        updateblueprintbuilding();
    }
    private bool hasbuildingplan()
    {
        if (_inventory == null)
            return false;
        itemz _item = _inventory.getmainhanditem();
        return _item != null && _item.definition != null && _item.definition.id == _buildingplanid;
    }
    private void opencatalog()
    {
        if (_catalog == null || _catalogopen)
            return;
        _catalogopen = true;
        _selectedcard = null;
        _selectedbuilding = null;
        setselectbutton(false);
        _catalog.SetActive(true);
        setcatalogcursor(true);
        showliquid();
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
        if (_selectedcard != null)
            _selectedcard.setselected(false);
        _selectedcard = null;
        _selectedbuilding = null;
        setselectbutton(false);
        for (int _i = _buildingcontent.childCount - 1; _i >= 0; _i--)
            Destroy(_buildingcontent.GetChild(_i).gameObject);
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
        if (_selectedcard != null)
            _selectedcard.setselected(false);
        _selectedcard = null;
        _catalogopen = false;
        if (_catalog != null)
            _catalog.SetActive(false);
        setselectbutton(false);
        setcatalogcursor(false);
        _ghostrotation = 0f;
        createMovingGhost();
        _placementactive = _movingghost != null;
        _placementstartframe = Time.frameCount;
    }
    public buildingdefinition getselectedbuilding()
    {
        return _selectedbuilding;
    }
    public void closecatalogbutton()
    {
        closecatalog();
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
        if (_value)
        {
            if (_cursorlogic != null)
            {
                _cursorlogicwasenabled = _cursorlogic.enabled;
                _cursorlogic.uiMode = false;
                _cursorlogic.enabled = false;
            }
            setplayermenu(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        setplayermenu(false);
        if (_cursorlogic != null)
        {
            _cursorlogic.uiMode = false;
            _cursorlogic.enabled = _cursorlogicwasenabled;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void setplayermenu(bool _value)
    {
        if (_playerinput == null && _playercontroller != null)
            _playerinput = _playercontroller.playerInput;
        if (_value)
        {
            _savedplayerfields.Clear();
            _savedactionstates.Clear();
            if (_playercontroller != null)
            {
                FieldInfo[] _fields = _playercontroller.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int _i = 0; _i < _fields.Length; _i++)
                {
                    FieldInfo _field = _fields[_i];
                    if (_field.IsInitOnly)
                        continue;
                    string _name = _field.Name.ToLowerInvariant();
                    if (_field.FieldType == typeof(float) && _name.Contains("sensitivity"))
                    {
                        _savedplayerfields[_field] = _field.GetValue(_playercontroller);
                        _field.SetValue(_playercontroller, _catalogsensitivity);
                    }
                    else if (_field.FieldType == typeof(Vector3) && (_name.Contains("currentmove") || _name.Contains("movevelocity")))
                    {
                        _field.SetValue(_playercontroller, Vector3.zero);
                    }
                    else if (_field.FieldType == typeof(float) && _name.Contains("currentspeed"))
                    {
                        _field.SetValue(_playercontroller, 0f);
                    }
                }
            }
            if (_playerinput != null && _playerinput.actions != null)
            {
                for (int _i = 0; _i < _movementactionnames.Length; _i++)
                {
                    InputAction _action = _playerinput.actions.FindAction(_movementactionnames[_i], false);
                    if (_action == null)
                        continue;
                    _savedactionstates[_action] = _action.enabled;
                    if (_action.enabled)
                        _action.Disable();
                }
            }
            return;
        }
        if (_playercontroller != null)
        {
            foreach (KeyValuePair<FieldInfo, object> _entry in _savedplayerfields)
            {
                if (_entry.Key == null)
                    continue;
                _entry.Key.SetValue(_playercontroller, _entry.Value);
            }
        }
        _savedplayerfields.Clear();
        foreach (KeyValuePair<InputAction, bool> _entry in _savedactionstates)
        {
            if (_entry.Key == null)
                continue;
            if (_entry.Value && !_entry.Key.enabled)
                _entry.Key.Enable();
            else if (!_entry.Value && _entry.Key.enabled)
                _entry.Key.Disable();
        }
        _savedactionstates.Clear();
    }
    private void createMovingGhost()
    {
        destroymovingghost();
        if (_selectedbuilding == null || _selectedbuilding._prefab == null || _camera == null)
            return;
        _movingghost = Instantiate(_selectedbuilding._prefab);
        _movingghost.name = "building_moving_ghost_" + _selectedbuilding._id;
        _movingghost.transform.rotation = Quaternion.identity;
        calculatelocalbounds(_movingghost);
        Vector3 _up = getplanetup(_camera.transform.position + _camera.transform.forward * 3f);
        Vector3 _forward = Vector3.ProjectOnPlane(_camera.transform.forward, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(_camera.transform.right, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(Vector3.forward, _up);
        _forward.Normalize();
        Quaternion _rotation = Quaternion.LookRotation(_forward, _up);
        Vector3 _bottom = _camera.transform.position + _camera.transform.forward * 3f;
        Vector3 _position = _bottom - _rotation * Vector3.Scale(_localbottomcenter, _movingghost.transform.localScale);
        _movingghost.transform.SetPositionAndRotation(_position, _rotation);
        setupghostobject(_movingghost, _validcolor, false);
        _movingghost.SetActive(true);
        _previewhasground = false;
        _previewvalid = false;
    }
    private void updatemovingghost()
    {
        if (_movingghost == null || _selectedbuilding == null || _camera == null)
            return;
        Ray _ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(_ray, out RaycastHit _hit, _placementdistance, _groundmask, QueryTriggerInteraction.Ignore))
        {
            _previewhasground = false;
            _previewvalid = false;
            setghostcolor(_movingghost, _invalidcolor);
            return;
        }
        Vector3 _up = getplanetup(_hit.point);
        if (_planet == null)
            _up = _hit.normal.normalized;
        Vector3 _forward = Vector3.ProjectOnPlane(_camera.transform.forward, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(_camera.transform.right, _up);
        if (_forward.sqrMagnitude < 0.001f)
            _forward = Vector3.ProjectOnPlane(Vector3.forward, _up);
        _forward.Normalize();
        Quaternion _surfaceRotation = Quaternion.LookRotation(_forward, _up);
        Quaternion _rotation = Quaternion.AngleAxis(_ghostrotation, _up) * _surfaceRotation;
        Vector3 _position = getgroundposition(_hit.point, _up, _rotation);
        _previewvalid = checksurface(_hit.point, _up, _position, _rotation);
        _previewhasground = true;
        _previewtargetposition = _position;
        _previewtargetrotation = _rotation;
        _movingghost.transform.position = Vector3.Lerp(_movingghost.transform.position, _position, _positionsmooth * Time.deltaTime);
        _movingghost.transform.rotation = Quaternion.Slerp(_movingghost.transform.rotation, _rotation, _rotationsmooth * Time.deltaTime);
        setghostcolor(_movingghost, _previewvalid ? _validcolor : _invalidcolor);
    }
    private Vector3 getgroundposition(Vector3 _hitpoint, Vector3 _up, Quaternion _rotation)
    {
        Vector3 _scale = _movingghost.transform.localScale;
        Vector3 _bottom = _hitpoint - _rotation * Vector3.Scale(_localbottomcenter, _scale);
        Vector3[] _samples = getfootprintsamples(_bottom, _rotation, _scale);
        float _highestoffset = 0f;
        for (int _i = 0; _i < _samples.Length; _i++)
        {
            Vector3 _origin = _samples[_i] + _up * _rayheight;
            if (!Physics.Raycast(_origin, -_up, out RaycastHit _surfacehit, _rayheight * 2f, _groundmask, QueryTriggerInteraction.Ignore))
                continue;
            float _offset = Vector3.Dot(_surfacehit.point - _hitpoint, _up);
            if (_offset > _highestoffset)
                _highestoffset = _offset;
        }
        return _bottom + _up * _highestoffset;
    }
    private Vector3[] getfootprintsamples(Vector3 _rootposition, Quaternion _rotation, Vector3 _scale)
    {
        float _bottomy = _localboundscenter.y - _localboundsextents.y;
        Vector3 _center = new Vector3(_localboundscenter.x, _bottomy, _localboundscenter.z);
        Vector3 _min = new Vector3(_localboundscenter.x - _localboundsextents.x, _bottomy, _localboundscenter.z - _localboundsextents.z);
        Vector3 _max = new Vector3(_localboundscenter.x + _localboundsextents.x, _bottomy, _localboundscenter.z + _localboundsextents.z);
        Vector3[] _locals = { _center, new Vector3(_min.x, _bottomy, _min.z), new Vector3(_min.x, _bottomy, _max.z), new Vector3(_max.x, _bottomy, _min.z), new Vector3(_max.x, _bottomy, _max.z) };
        Vector3[] _world = new Vector3[_locals.Length];
        for (int _i = 0; _i < _locals.Length; _i++)
            _world[_i] = _rootposition + _rotation * Vector3.Scale(_locals[_i], _scale);
        return _world;
    }
    private bool checksurface(Vector3 _centerhit, Vector3 _up, Vector3 _rootposition, Quaternion _rotation)
    {
        Vector3[] _samples = getfootprintsamples(_rootposition, _rotation, _movingghost.transform.localScale);
        for (int _i = 0; _i < _samples.Length; _i++)
        {
            Vector3 _origin = _samples[_i] + _up * _rayheight;
            if (!Physics.Raycast(_origin, -_up, out RaycastHit _hit, _rayheight * 2f, _groundmask, QueryTriggerInteraction.Ignore))
                return false;
            if (Vector3.Angle(_hit.normal, _up) > _maxslopeangle)
                return false;
            float _heightdifference = Vector3.Dot(_hit.point - _centerhit, _up);
            if (Mathf.Abs(_heightdifference) > _maxheightdifference)
                return false;
        }
        return !checkoverlap(_rootposition, _rotation);
    }
    private bool checkoverlap(Vector3 _rootposition, Quaternion _rotation)
    {
        if (_movingghost == null)
            return false;
        Vector3 _scale = _movingghost.transform.localScale;
        Vector3 _center = _rootposition + _rotation * Vector3.Scale(_localboundscenter, _scale);
        Vector3 _extents = Vector3.Scale(_localboundsextents, _scale) + Vector3.one * 0.025f;
        Collider[] _hits = Physics.OverlapBox(_center, _extents, _rotation, ~0, QueryTriggerInteraction.Collide);
        for (int _i = 0; _i < _hits.Length; _i++)
        {
            Collider _collider = _hits[_i];
            if (_collider == null || !_collider.enabled)
                continue;
            if (_movingghost != null && _collider.transform.IsChildOf(_movingghost.transform))
                continue;
            if ((_groundmask.value & (1 << _collider.gameObject.layer)) != 0)
                continue;
            if (_playercontroller != null && (_collider.transform == _playercontroller.transform || _collider.transform.IsChildOf(_playercontroller.transform)))
                continue;
            buildingghost _marker = _collider.GetComponentInParent<buildingghost>();
            if (_marker != null)
                return true;
            if (_collider.isTrigger)
                continue;
            return true;
        }
        return false;
    }
    private Vector3 getplanetup(Vector3 _position)
    {
        if (_planet != null)
        {
            Vector3 _up = _planet.getup(_position);
            if (_up.sqrMagnitude > 0.001f)
                return _up.normalized;
        }
        return Vector3.up;
    }
    private void placeblueprint()
    {
        if (_movingghost == null || _selectedbuilding == null || !_previewhasground || !_previewvalid)
            return;
        Vector3 _position = _previewtargetposition;
        Quaternion _rotation = _previewtargetrotation;
        if (checkoverlap(_position, _rotation))
            return;
        GameObject _blueprint = Instantiate(_selectedbuilding._prefab, _position, _rotation);
        _blueprint.name = "building_blueprint_" + _selectedbuilding._id;
        setupghostobject(_blueprint, _bluecolor, true);
        buildingghost _marker = _blueprint.GetComponent<buildingghost>();
        if (_marker == null)
            _marker = _blueprint.AddComponent<buildingghost>();
        _marker.enabled = true;
        _marker.initialize(_selectedbuilding, this);
        _placedghosts.Add(_marker);
    }
    private void setupghostobject(GameObject _object, Color _color, bool _placed)
    {
        if (_object == null)
            return;
        MonoBehaviour[] _scripts = _object.GetComponentsInChildren<MonoBehaviour>(true);
        for (int _i = 0; _i < _scripts.Length; _i++)
        {
            if (_scripts[_i] is buildingghost)
                continue;
            _scripts[_i].enabled = false;
        }
        Collider[] _colliders = _object.GetComponentsInChildren<Collider>(true);
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            if (_placed)
            {
                _colliders[_i].isTrigger = true;
                _colliders[_i].enabled = true;
            }
            else
                _colliders[_i].enabled = false;
        }
        Rigidbody[] _rigidbodies = _object.GetComponentsInChildren<Rigidbody>(true);
        for (int _i = 0; _i < _rigidbodies.Length; _i++)
        {
            _rigidbodies[_i].isKinematic = true;
            _rigidbodies[_i].useGravity = false;
            _rigidbodies[_i].detectCollisions = _placed;
        }
        applyghostmaterials(_object, _color);
    }
    private void applyghostmaterials(GameObject _object, Color _color)
    {
        if (_object == null)
            return;
        List<Material> _created = new List<Material>();
        Renderer[] _renderers = _object.GetComponentsInChildren<Renderer>(true);
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            Material[] _source = _renderers[_i].sharedMaterials;
            Material[] _materials = new Material[_source.Length];
            for (int _j = 0; _j < _source.Length; _j++)
            {
                if (_source[_j] == null)
                    continue;
                Material _material = new Material(_source[_j]);
                settransparent(_material, _color);
                _materials[_j] = _material;
                _created.Add(_material);
            }
            _renderers[_i].materials = _materials;
        }
        _ghostmaterials[_object] = _created;
    }
    private void settransparent(Material _material, Color _color)
    {
        if (_material == null)
            return;
        if (_material.HasProperty("_Surface"))
        {
            _material.SetFloat("_Surface", 1f);
            _material.SetFloat("_Blend", 0f);
            _material.SetFloat("_AlphaClip", 0f);
            _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_ZWrite", 0);
            _material.DisableKeyword("_ALPHATEST_ON");
            _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        if (_material.HasProperty("_Mode"))
        {
            _material.SetFloat("_Mode", 2f);
            _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_ZWrite", 0);
            _material.DisableKeyword("_ALPHATEST_ON");
            _material.EnableKeyword("_ALPHABLEND_ON");
            _material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (_material.HasProperty("_BaseColor"))
            _material.SetColor("_BaseColor", _color);
        if (_material.HasProperty("_Color"))
            _material.SetColor("_Color", _color);
        if (_material.HasProperty("_TintColor"))
            _material.SetColor("_TintColor", _color);
    }
    private void setghostcolor(GameObject _object, Color _color)
    {
        if (_object == null || !_ghostmaterials.TryGetValue(_object, out List<Material> _materials))
            return;
        for (int _i = 0; _i < _materials.Count; _i++)
        {
            Material _material = _materials[_i];
            if (_material == null)
                continue;
            if (_material.HasProperty("_BaseColor"))
                _material.SetColor("_BaseColor", _color);
            if (_material.HasProperty("_Color"))
                _material.SetColor("_Color", _color);
            if (_material.HasProperty("_TintColor"))
                _material.SetColor("_TintColor", _color);
        }
    }
    private void calculatelocalbounds(GameObject _object)
    {
        if (_object == null)
            return;
        Renderer[] _renderers = _object.GetComponentsInChildren<Renderer>(true);
        Collider[] _colliders = _object.GetComponentsInChildren<Collider>(true);
        Bounds _worldbounds = new Bounds(_object.transform.position, Vector3.zero);
        bool _hasbounds = false;
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            if (!_renderers[_i].enabled)
                continue;
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
            if (!_colliders[_i].enabled)
                continue;
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
            _localboundsextents = _selectedbuilding._size * 0.5f;
            _localbottomcenter = new Vector3(0f, -_selectedbuilding._size.y * 0.5f, 0f);
            return;
        }
        Vector3[] _corners = getboundscorners(_worldbounds);
        Bounds _localbounds = new Bounds(_object.transform.InverseTransformPoint(_corners[0]), Vector3.zero);
        for (int _i = 1; _i < _corners.Length; _i++)
            _localbounds.Encapsulate(_object.transform.InverseTransformPoint(_corners[_i]));
        _localboundscenter = _localbounds.center;
        _localboundsextents = _localbounds.extents;
        _localbottomcenter = new Vector3(_localboundscenter.x, _localbounds.min.y, _localboundscenter.z);
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
    private void updaterotation()
    {
        if (Mouse.current == null)
            return;
        float _scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(_scroll) < 0.01f)
            return;
        _ghostrotation = Mathf.Repeat(_ghostrotation + (_scroll > 0f ? _rotationstep : -_rotationstep), 360f);
    }
    private void destroyghostmaterials(GameObject _object)
    {
        if (_object == null)
            return;
        if (_ghostmaterials.TryGetValue(_object, out List<Material> _materials))
        {
            for (int _i = 0; _i < _materials.Count; _i++)
            {
                if (_materials[_i] != null)
                    Destroy(_materials[_i]);
            }
            _ghostmaterials.Remove(_object);
        }
    }
    private void destroymovingghost()
    {
        if (_movingghost != null)
        {
            destroyghostmaterials(_movingghost);
            Destroy(_movingghost);
            _movingghost = null;
        }
        _previewhasground = false;
        _previewvalid = false;
    }
    private void cancelplacement()
    {
        destroymovingghost();
        _placementactive = false;
        _selectedbuilding = null;
        _ghostrotation = 0f;
    }
    private Button findclosebutton()
    {
        Button[] _buttons = _catalog.GetComponentsInChildren<Button>(true);
        for (int _i = 0; _i < _buttons.Length; _i++)
        {
            string _name = _buttons[_i].name.ToLowerInvariant();
            if (_name.Contains("close") || _name.Contains("cross-mark") || _name.Contains("cross") || _name == "x")
                return _buttons[_i];
        }
        return null;
    }
    private void OnDisable()
    {
        if (_catalogopen)
        {
            _catalogopen = false;
            setcatalogcursor(false);
        }
        if (_movingghost != null)
            destroymovingghost();
        _placementactive = false;
    }
    private void OnDestroy()
    {
        foreach (KeyValuePair<GameObject, List<Material>> _entry in _ghostmaterials)
        {
            if (_entry.Value == null)
                continue;
            for (int _i = 0; _i < _entry.Value.Count; _i++)
            {
                if (_entry.Value[_i] != null)
                    Destroy(_entry.Value[_i]);
            }
        }
        _ghostmaterials.Clear();
    }
    private void updateblueprintbuilding()
    {
        Keyboard _keyboard = Keyboard.current;
        bool _physicale = _keyboard != null && _keyboard.eKey.isPressed;
        bool _actionfound = _pickupaction != null;
        bool _actionenabled = _actionfound && _pickupaction.enabled;
        bool _actionheld = _actionenabled && _pickupaction.IsPressed();
        bool _eheld = _physicale || _actionheld;
        bool _inputchanged = _eheld != _lastdebuge;
        _constructiondebuginput = $"E: physical={_physicale}, PickUp-found={_actionfound}, PickUp-enabled={_actionenabled}, PickUp-pressed={_actionheld}; placed blueprints={_placedghosts.Count}";

        if (!_eheld)
        {
            _constructiontarget = null;
            _constructiondebugstatus = "Waiting: aim at a blue blueprint and hold E.";
            if (_inputchanged)
                logconstructiondebug(true);
            _lastdebuge = _eheld;
            return;
        }

        if (_camera == null)
        {
            _constructiontarget = null;
            _constructiondebugstatus = "ERROR: Camera is not assigned and Camera.main was not found.";
            logconstructiondebug(_inputchanged);
            _lastdebuge = _eheld;
            return;
        }

        _constructiontarget = _aimedblueprint != null ? _aimedblueprint : findblueprinttarget();
        if (_constructiontarget == null)
        {
            _constructiondebugstatus = "BLUEPRINT NOT FOUND by raycast. " + _constructiondebugrayinfo;
            logconstructiondebug(_inputchanged);
            _lastdebuge = _eheld;
            return;
        }

        Vector3 _playerposition = _playercontroller != null ? _playercontroller.transform.position : _camera.transform.position;
        float _distance = Vector3.Distance(_playerposition, _constructiontarget.transform.position);
        if (_distance > _builddistance)
        {
            _constructiondebugstatus = $"Blueprint found but too far away: {_distance:F2} m; limit {_builddistance:F2} m.";
            logconstructiondebug(_inputchanged);
            _lastdebuge = _eheld;
            return;
        }

        buildingdefinition _definition = _constructiontarget.getbuilding();
        if (_definition == null)
        {
            _constructiondebugstatus = "ERROR: the found blueprint has no buildingdefinition.";
            logconstructiondebug(true);
            _lastdebuge = _eheld;
            return;
        }

        refreshnearbyresources();
        Dictionary<int, int> _required = getrequiredresources(_definition);
        List<string> _missingresources = new List<string>();
        foreach (KeyValuePair<int, int> _entry in _required)
        {
            int _have = gettotalresource(_entry.Key);
            if (_have < _entry.Value)
                _missingresources.Add($"{getresourceitemname(_entry.Key)}: have {_have}, need {_entry.Value}");
        }

        _constructiondebugrequired = Mathf.Max(_definition._buildtime, 0.01f);
        _constructiondebugprogress = _constructiontarget.getprogress();

        if (_missingresources.Count > 0)
        {
            _constructiondebugstatus = $"Blueprint '{_definition.name}' found at {_distance:F2} m, but resources are missing: {string.Join("; ", _missingresources)}.";
            logconstructiondebug(_inputchanged);
            _lastdebuge = _eheld;
            return;
        }

        if (_definition._prefab == null)
        {
            _constructiondebugstatus = $"ERROR: _prefab is not assigned on buildingdefinition '{_definition.name}'.";
            logconstructiondebug(true);
            _lastdebuge = _eheld;
            return;
        }

        _constructiontarget.addprogress(Time.deltaTime);
        _constructiondebugprogress = _constructiontarget.getprogress();
        _constructiondebugstatus = $"BUILDING: '{_definition.name}'; distance {_distance:F2}/{_builddistance:F2} m; progress {_constructiondebugprogress:F2}/{_constructiondebugrequired:F2} sec.; resources available.";

        if (_constructiondebugprogress >= _constructiondebugrequired)
        {
            _constructiondebugstatus = $"PROGRESS COMPLETE for '{_definition.name}'. Creating the finished object.";
            logconstructiondebug(true);
            finishblueprint(_constructiontarget);
        }
        else
        {
            logconstructiondebug(_inputchanged);
        }

        _lastdebuge = _eheld;
    }
    private buildingghost findblueprinttarget(bool _requirebuilddistance = true)
    {
        if (_camera == null)
        {
            _constructiondebugrayinfo = "Camera is missing.";
            return null;
        }

        Ray _ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] _hits = Physics.RaycastAll(_ray, _placementdistance, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(_hits, (a, b) => a.distance.CompareTo(b.distance));

        string _nearesthit = "nothing hit by the ray";
        if (_hits.Length > 0 && _hits[0].collider != null)
        {
            Collider _firstcollider = _hits[0].collider;
            _nearesthit = $"'{_firstcollider.name}', layer={LayerMask.LayerToName(_firstcollider.gameObject.layer)}, distance={_hits[0].distance:F2} m";
        }

        int _markersontheir = 0;
        int _markersregistered = 0;
        string _lastrejectreason = "no collider with buildingghost under the ray";

        for (int _i = 0; _i < _hits.Length; _i++)
        {
            if (_hits[_i].collider == null)
                continue;

            buildingghost _marker = _hits[_i].collider.GetComponentInParent<buildingghost>();
            if (_marker == null)
                continue;

            _markersontheir++;
            if (!_placedghosts.Contains(_marker))
            {
                _lastrejectreason = $"buildingghost '{_marker.name}' was found but is not in _placedghosts";
                continue;
            }

            _markersregistered++;
            Vector3 _playerposition = _playercontroller != null ? _playercontroller.transform.position : _camera.transform.position;
            float _playerdistance = Vector3.Distance(_playerposition, _marker.transform.position);
            if (_playerdistance > _builddistance)
            {
                _lastrejectreason = $"blueprint '{_marker.name}' is beyond the limit: {_playerdistance:F2} m > {_builddistance:F2} m";
                if (_requirebuilddistance)
                    continue;
            }

            _constructiondebugrayinfo = $"Blueprint '{_marker.name}' found; player distance {_playerdistance:F2} m; ray hit distance {_hits[_i].distance:F2} m.";
            return _marker;
        }

        _constructiondebugrayinfo = $"RaycastAll hits={_hits.Length}; nearest object: {_nearesthit}; buildingghosts on ray={_markersontheir}; registered blueprints={_markersregistered}; total registered={_placedghosts.Count}; reason: {_lastrejectreason}.";
        return null;
    }
    private void loadresourcenames()
    {
        _resourcenames.Clear();
        item_data_general[] _items = Resources.LoadAll<item_data_general>("Items");
        for (int _i = 0; _i < _items.Length; _i++)
        {
            if (_items[_i] == null)
                continue;
            string _name = _items[_i].name;
            if (string.IsNullOrWhiteSpace(_name))
                _name = "Item ID " + _items[_i].id;
            _resourcenames[_items[_i].id] = _name;
        }
    }
    private string getresourceitemname(int _itemid)
    {
        if (_resourcenames.TryGetValue(_itemid, out string _name))
            return _name;
        return "Item ID " + _itemid;
    }
    private void updateaimedblueprintrequirements()
    {
        if (_camera == null)
        {
            clearrequirementtarget();
            return;
        }

        buildingghost _target = findblueprinttarget(false);
        if (_target != _aimedblueprint)
        {
            _aimedblueprint = _target;
            _nextrequirementsrefresh = 0f;
            if (_aimedblueprint == null)
            {
                _aimedbuildingname = string.Empty;
                _aimedrequiredresources.Clear();
                _aimedavailableresources.Clear();
                return;
            }
        }

        if (_aimedblueprint == null || Time.unscaledTime < _nextrequirementsrefresh)
            return;

        _nextrequirementsrefresh = Time.unscaledTime + Mathf.Max(_requirementsrefreshinterval, 0.05f);
        buildingdefinition _definition = _aimedblueprint.getbuilding();
        _aimedrequiredresources.Clear();
        _aimedavailableresources.Clear();
        if (_definition == null)
        {
            _aimedbuildingname = "Unknown blueprint";
            return;
        }

        _aimedbuildingname = _definition.name;
        refreshnearbyresources();
        Dictionary<int, int> _required = getrequiredresources(_definition);
        foreach (KeyValuePair<int, int> _entry in _required)
        {
            _aimedrequiredresources[_entry.Key] = _entry.Value;
            _aimedavailableresources[_entry.Key] = gettotalresource(_entry.Key);
        }
    }
    private void clearrequirementtarget()
    {
        _aimedblueprint = null;
        _aimedbuildingname = string.Empty;
        _aimedrequiredresources.Clear();
        _aimedavailableresources.Clear();
        _nextrequirementsrefresh = 0f;
    }
    private void cancelplacedblueprint(buildingghost _blueprint)
    {
        if (_blueprint == null || !_placedghosts.Contains(_blueprint))
            return;

        string _blueprintname = _blueprint.name;
        GameObject _object = _blueprint.gameObject;
        _placedghosts.Remove(_blueprint);
        if (_constructiontarget == _blueprint)
            _constructiontarget = null;
        if (_aimedblueprint == _blueprint)
            clearrequirementtarget();
        destroyghostmaterials(_object);
        Destroy(_object);
        _constructiondebugstatus = $"Blueprint '{_blueprintname}' cancelled with right mouse button. No resources were consumed.";
        logconstructiondebug(true);
    }
    private Dictionary<int, int> getrequiredresources(buildingdefinition _building)
    {
        Dictionary<int, int> _required = new Dictionary<int, int>();
        if (_building == null || _building._cost == null)
            return _required;
        for (int _i = 0; _i < _building._cost.Length; _i++)
        {
            buildingcost _cost = _building._cost[_i];
            if (_cost == null || _cost._amount <= 0)
                continue;
            if (_required.ContainsKey(_cost._itemid))
                _required[_cost._itemid] += _cost._amount;
            else
                _required.Add(_cost._itemid, _cost._amount);
        }
        return _required;
    }
    private void refreshnearbyresources()
    {
        if (_neardysystem != null)
            _neardysystem.Scan();
    }
    private int gettotalresource(int _itemid)
    {
        int _amount = _inventory != null ? _inventory.GetTotalItemAmount(_itemid) : 0;
        if (_neardysystem != null)
            _amount += _neardysystem.getnearbyitemamount(_itemid);
        return _amount;
    }
    private bool hasallresources(buildingdefinition _building)
    {
        refreshnearbyresources();
        Dictionary<int, int> _required = getrequiredresources(_building);
        foreach (KeyValuePair<int, int> _entry in _required)
        {
            if (gettotalresource(_entry.Key) < _entry.Value)
                return false;
        }
        return true;
    }
    private bool consumeresources(buildingdefinition _building)
    {
        if (!hasallresources(_building))
            return false;
        Dictionary<int, int> _required = getrequiredresources(_building);
        foreach (KeyValuePair<int, int> _entry in _required)
        {
            int _remaining = _entry.Value;
            int _inventoryamount = _inventory != null ? _inventory.GetTotalItemAmount(_entry.Key) : 0;
            int _frominventory = Mathf.Min(_inventoryamount, _remaining);
            if (_frominventory > 0)
            {
                if (!_inventory.RemoveItemAmount(_entry.Key, _frominventory))
                    return false;
                _remaining -= _frominventory;
            }
            if (_remaining > 0)
            {
                if (_neardysystem == null || !_neardysystem.removenearbyitemamount(_entry.Key, _remaining))
                    return false;
            }
        }
        if (_inventory != null)
            _inventory.RefreshUI();
        if (_neardysystem != null)
            _neardysystem.Scan();
        return true;
    }
    private void finishblueprint(buildingghost _blueprint)
    {
        if (_blueprint == null)
            return;
        buildingdefinition _definition = _blueprint.getbuilding();
        if (_definition == null)
        {
            _constructiondebugstatus = "FINISH ERROR: blueprint has no buildingdefinition.";
            logconstructiondebug(true);
            return;
        }
        if (_definition._prefab == null)
        {
            _constructiondebugstatus = $"FINISH ERROR: _prefab is not assigned on '{_definition.name}'.";
            logconstructiondebug(true);
            return;
        }
        if (!consumeresources(_definition))
        {
            _constructiondebugstatus = $"Could not consume resources for '{_definition.name}'. Progress was reset.";
            Debug.LogError("[BUILD DEBUG] " + _constructiondebugstatus, this);
            _blueprint.resetprogress();
            _constructiontarget = null;
            return;
        }
        Vector3 _position = _blueprint.transform.position;
        Quaternion _rotation = _blueprint.transform.rotation;
        GameObject _blueprintobject = _blueprint.gameObject;
        _placedghosts.Remove(_blueprint);
        if (_aimedblueprint == _blueprint)
            clearrequirementtarget();
        destroyghostmaterials(_blueprintobject);
        Destroy(_blueprintobject);
        Instantiate(_definition._prefab, _position, _rotation);
        _constructiondebugstatus = $"COMPLETE: object '{_definition.name}' created and resources consumed.";
        logconstructiondebug(true);
        _constructiontarget = null;
    }
    private void logconstructiondebug(bool _force)
    {
        if (!_debugconstruction)
            return;
        if (!_force && Time.unscaledTime < _nextconstructiondebuglog)
            return;
        _nextconstructiondebuglog = Time.unscaledTime + Mathf.Max(0.1f, _debugloginterval);
        Debug.Log($"[BUILD DEBUG] {_constructiondebugstatus}\nINPUT: {_constructiondebuginput}\nRAY: {_constructiondebugrayinfo}\nPROGRESS: {_constructiondebugprogress:F2}/{_constructiondebugrequired:F2} sec.", this);
    }

    private void OnGUI()
    {
        if (_debugconstruction && _showconstructiondebughud)
        {
            if (_constructiondebugstyle == null)
            {
                _constructiondebugstyle = new GUIStyle(GUI.skin.label);
                _constructiondebugstyle.alignment = TextAnchor.UpperLeft;
                _constructiondebugstyle.fontSize = 14;
                _constructiondebugstyle.wordWrap = true;
                _constructiondebugstyle.normal.textColor = Color.white;
                _constructiondebugstyle.richText = true;
            }

            string _text = "BUILD DEBUG\n" +
                           _constructiondebugstatus + "\n" +
                           _constructiondebuginput + "\n" +
                           "Ray: " + _constructiondebugrayinfo + "\n" +
                           $"Progress: {_constructiondebugprogress:F2}/{_constructiondebugrequired:F2} sec.";

            GUI.Label(new Rect(10f, 10f, 650f, 145f), _text, _constructiondebugstyle);
        }

        if (!_showrequirements || _aimedblueprint == null)
            return;

        if (_requirementstitlestyle == null)
        {
            _requirementstitlestyle = new GUIStyle(GUI.skin.label);
            _requirementstitlestyle.fontSize = 15;
            _requirementstitlestyle.fontStyle = FontStyle.Bold;
            _requirementstitlestyle.alignment = TextAnchor.UpperLeft;
            _requirementstitlestyle.normal.textColor = Color.white;
        }

        if (_requirementslinestyle == null)
        {
            _requirementslinestyle = new GUIStyle(GUI.skin.label);
            _requirementslinestyle.fontSize = 14;
            _requirementslinestyle.richText = true;
            _requirementslinestyle.alignment = TextAnchor.UpperLeft;
            _requirementslinestyle.normal.textColor = Color.white;
        }

        float _width = Mathf.Max(240f, _requirementswidth);
        float _x = Screen.width * 0.5f + 24f;
        float _y = Screen.height * 0.5f + 18f;
        GUI.Label(new Rect(_x, _y, _width, 24f), "Required resources: " + _aimedbuildingname, _requirementstitlestyle);

        if (_aimedrequiredresources.Count == 0)
        {
            GUI.Label(new Rect(_x, _y + 24f, _width, 23f), "No resources required.", _requirementslinestyle);
            return;
        }

        int _row = 0;
        foreach (KeyValuePair<int, int> _entry in _aimedrequiredresources)
        {
            int _have = _aimedavailableresources.TryGetValue(_entry.Key, out int _available) ? _available : 0;
            bool _enough = _have >= _entry.Value;
            string _color = _enough ? "#78FF8A" : "#FF7777";
            string _line = $"<color={_color}>{getresourceitemname(_entry.Key)}: {_have}/{_entry.Value}</color>";
            GUI.Label(new Rect(_x, _y + 24f + _row * 22f, _width, 22f), _line, _requirementslinestyle);
            _row++;
        }
    }

}
