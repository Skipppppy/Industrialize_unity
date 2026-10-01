using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class buildingmanager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EasyInv _inventory;
    [SerializeField] private PlayerInput _playerinput;
    [SerializeField] private CursorManager _cursorlogic;
    [SerializeField] private Camera _camera;
    [SerializeField] private pointterrain _planet;
    [Header("Building Plan")]
    [SerializeField] private int _buildingplanid;
    [Header("Buildings")]
    [SerializeField] private List<buildingdefinition> _buildings = new List<buildingdefinition>();
    [Header("Catalog")]
    [SerializeField] private GameObject _catalog;
    [SerializeField] private Transform _categorycontent;
    [SerializeField] private Button _categorybuttonprefab;
    [SerializeField] private Transform _buildingcontent;
    [SerializeField] private Button _buildingbuttonprefab;
    [SerializeField] private Button _closebutton;
    [Header("Building Info")]
    [SerializeField] private GameObject _infopanel;
    [SerializeField] private Image _buildingicon;
    [SerializeField] private TMP_Text _buildingname;
    [SerializeField] private TMP_Text _buildingdescription;
    [SerializeField] private TMP_Text _buildingcost;
    [SerializeField] private TMP_Text _buildingtime;
    [Header("Placement")]
    [SerializeField] private LayerMask _groundmask;
    [SerializeField] private float _placementdistance = 8f;
    [SerializeField] private float _builddistance = 4f;
    [SerializeField] private float _resourcecheckradius = 3f;
    [SerializeField] private float _maxslopeangle = 8f;
    [SerializeField] private float _maxheightdifference = 0.2f;
    [SerializeField] private float _rayheight = 4f;
    [SerializeField] private float _rotationstep = 90f;
    [Header("Ghost")]
    [SerializeField] private Material _ghostmaterial;
    [SerializeField] private Color _ghostcolor = new Color(0.1f, 0.45f, 1f, 0.35f);
    [SerializeField] private Color _validcolor = new Color(0.1f, 1f, 0.2f, 0.45f);
    [SerializeField] private Color _invalidcolor = new Color(1f, 0.1f, 0.1f, 0.45f);
    [SerializeField] private Color _resourcecolor = new Color(1f, 0.65f, 0.1f, 0.45f);
    private InputAction _lookaction;
    private buildingdefinition _selectedbuilding;
    private GameObject _ghost;
    private Material _runtimeghostmaterial;
    private string _selectedcategory;
    private bool _catalogopen;
    private bool _placementmode;
    private bool _building;
    private float _buildprogress;
    private void Awake()
    {
        if (_playerinput != null)
        {
            _lookaction = _playerinput.actions["Look"];
        }
        if (_catalog != null)
        {
            _catalog.SetActive(false);
        }
        if (_infopanel != null)
        {
            _infopanel.SetActive(false);
        }
        if (_closebutton != null)
        {
            _closebutton.onClick.AddListener(cancelbuilding);
        }
    }
    private void Update()
    {
        if (_inventory == null || _camera == null) return;
        if (_catalogopen || _placementmode)
        {
            if (!hasbuildingplan())
            {
                cancelbuilding();
                return;
            }
        }
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !_building)
        {
            if (_catalogopen || _placementmode)
            {
                cancelbuilding();
            }
            else if (hasbuildingplan())
            {
                opencatalog();
            }
        }
        if (_placementmode && !_building)
        {
            updateghost();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                trybeginbuilding();
            }
        }
        if (_building)
        {
            updatebuilding();
        }
    }
    private bool hasbuildingplan()
    {
        itemz _item = _inventory.getmainhanditem();
        if (_item == null || _item.definition == null) return false;
        return _item.definition.id == _buildingplanid;
    }
    private void opencatalog()
    {
        _catalogopen = true;
        _placementmode = false;
        buildcategories();
        setcategory("");
        if (_catalog != null)
        {
            _catalog.SetActive(true);
        }
        if (_infopanel != null)
        {
            _infopanel.SetActive(false);
        }
        setbuildui(true);
    }
    private void buildcategories()
    {
        if (_categorycontent == null || _categorybuttonprefab == null) return;
        for (int _i = _categorycontent.childCount - 1; _i >= 0; _i--)
        {
            Destroy(_categorycontent.GetChild(_i).gameObject);
        }
        HashSet<string> _categories = new HashSet<string>();
        for (int _i = 0; _i < _buildings.Count; _i++)
        {
            if (_buildings[_i] == null) continue;
            _categories.Add(_buildings[_i]._category);
        }
        foreach (string _category in _categories)
        {
            string _categorycopy = _category;
            Button _button = Instantiate(_categorybuttonprefab, _categorycontent);
            TMP_Text _text = _button.GetComponentInChildren<TMP_Text>();
            if (_text != null)
            {
                _text.text = _categorycopy;
            }
            _button.onClick.AddListener(() =>
            {
                setcategory(_categorycopy);
            });
        }
    }
    private void setcategory(string _category)
    {
        _selectedcategory = _category;
        if (_buildingcontent == null || _buildingbuttonprefab == null) return;
        for (int _i = _buildingcontent.childCount - 1; _i >= 0; _i--)
        {
            Destroy(_buildingcontent.GetChild(_i).gameObject);
        }
        for (int _i = 0; _i < _buildings.Count; _i++)
        {
            buildingdefinition _building = _buildings[_i];
            if (_building == null) continue;
            if (_selectedcategory != "" && _building._category != _selectedcategory) continue;
            buildingdefinition _buildingcopy = _building;
            Button _button = Instantiate(_buildingbuttonprefab, _buildingcontent);
            Image _icon = _button.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text _text = _button.GetComponentInChildren<TMP_Text>();
            if (_icon != null)
            {
                _icon.sprite = _buildingcopy._icon;
            }
            if (_text != null)
            {
                _text.text = _buildingcopy._name;
            }
            _button.onClick.AddListener(() =>
            {
                selectbuilding(_buildingcopy);
            });
        }
    }
    private void selectbuilding(buildingdefinition _building)
    {
        _selectedbuilding = _building;
        _catalogopen = false;
        if (_catalog != null)
        {
            _catalog.SetActive(false);
        }
        showbuildinginfo(_building);
        createghost();
        _placementmode = true;
        setbuildui(true);
    }
    private void showbuildinginfo(buildingdefinition _building)
    {
        if (_infopanel == null) return;
        _infopanel.SetActive(true);
        if (_buildingicon != null)
        {
            _buildingicon.sprite = _building._icon;
        }
        if (_buildingname != null)
        {
            _buildingname.text = _building._name;
        }
        if (_buildingdescription != null)
        {
            _buildingdescription.text = _building._description;
        }
        if (_buildingtime != null)
        {
            _buildingtime.text = "Время строительства: " + _building._buildtime.ToString("0.0") + " сек.";
        }
        if (_buildingcost != null)
        {
            _buildingcost.text = getcosttext(_building);
        }
    }
    private string getcosttext(buildingdefinition _building)
    {
        string _text = "";
        for (int _i = 0; _i < _building._cost.Length; _i++)
        {
            buildingcost _cost = _building._cost[_i];
            int _have = gettotalresource(_cost._itemid);
            if (_i > 0)
            {
                _text += "\n";
            }
            _text += "ID " + _cost._itemid + ": " + _have + " / " + _cost._amount;
        }
        return _text;
    }
    private void createghost()
    {
        destroyghost();
        if (_selectedbuilding == null || _selectedbuilding._prefab == null) return;
        _ghost = Instantiate(_selectedbuilding._prefab);
        _ghost.name = "ghost_" + _selectedbuilding._id;
        Collider[] _colliders = _ghost.GetComponentsInChildren<Collider>();
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            _colliders[_i].isTrigger = true;
            _colliders[_i].enabled = true;
        }
        Rigidbody[] _rigidbodies = _ghost.GetComponentsInChildren<Rigidbody>();
        for (int _i = 0; _i < _rigidbodies.Length; _i++)
        {
            _rigidbodies[_i].isKinematic = true;
            _rigidbodies[_i].useGravity = false;
        }
        Renderer[] _renderers = _ghost.GetComponentsInChildren<Renderer>();
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            Material[] _materials = new Material[_renderers[_i].sharedMaterials.Length];
            for (int _j = 0; _j < _materials.Length; _j++)
            {
                _materials[_j] = getghostmaterial();
            }
            _renderers[_i].materials = _materials;
        }
    }
    private Material getghostmaterial()
    {
        if (_ghostmaterial != null) return _ghostmaterial;
        if (_runtimeghostmaterial != null) return _runtimeghostmaterial;
        Shader _shader = Shader.Find("Standard");
        _runtimeghostmaterial = new Material(_shader);
        _runtimeghostmaterial.SetFloat("_Mode", 2f);
        _runtimeghostmaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _runtimeghostmaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _runtimeghostmaterial.SetInt("_ZWrite", 0);
        _runtimeghostmaterial.EnableKeyword("_ALPHABLEND_ON");
        _runtimeghostmaterial.renderQueue = 3000;
        _runtimeghostmaterial.color = _ghostcolor;
        return _runtimeghostmaterial;
    }
    private void updateghost()
    {
        if (_ghost == null || _selectedbuilding == null) return;
        Ray _ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(_ray, out RaycastHit _hit, _placementdistance, _groundmask, QueryTriggerInteraction.Ignore))
        {
            _ghost.SetActive(false);
            return;
        }
        Vector3 _up = _hit.normal.normalized;
        Vector3 _forward = Vector3.ProjectOnPlane(_camera.transform.forward, _up);
        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward = Vector3.ProjectOnPlane(Vector3.forward, _up);
        }
        _forward.Normalize();
        Vector3 _reference = Vector3.ProjectOnPlane(Vector3.forward, _up);
        if (_reference.sqrMagnitude < 0.001f)
        {
            _reference = Vector3.ProjectOnPlane(Vector3.right, _up);
        }
        _reference.Normalize();
        float _angle = Vector3.SignedAngle(_reference, _forward, _up);
        float _snapped = Mathf.Round(_angle / _rotationstep) * _rotationstep;
        Quaternion _rotation = Quaternion.AngleAxis(_snapped, _up) * Quaternion.LookRotation(_reference, _up);
        Vector3 _position = _hit.point + _up * (_selectedbuilding._size.y * 0.5f);
        bool _flat = checksurface(_hit.point, _up, _rotation);
        bool _resources = hasallresources(_selectedbuilding);
        _ghost.transform.SetPositionAndRotation(_position, _rotation);
        _ghost.SetActive(true);
        if (!_flat)
        {
            setghostcolor(_invalidcolor);
        }
        else if (!_resources)
        {
            setghostcolor(_resourcecolor);
        }
        else
        {
            setghostcolor(_validcolor);
        }
    }
    private bool checksurface(Vector3 _center, Vector3 _up, Quaternion _rotation)
    {
        float _halfX = _selectedbuilding._size.x * 0.5f;
        float _halfZ = _selectedbuilding._size.z * 0.5f;
        Vector3[] _samples = new Vector3[5];
        _samples[0] = _center;
        _samples[1] = _center + _rotation * new Vector3(_halfX, 0f, _halfZ);
        _samples[2] = _center + _rotation * new Vector3(-_halfX, 0f, _halfZ);
        _samples[3] = _center + _rotation * new Vector3(_halfX, 0f, -_halfZ);
        _samples[4] = _center + _rotation * new Vector3(-_halfX, 0f, -_halfZ);
        Vector3 _centerpoint = Vector3.zero;
        bool _havecenter = false;
        for (int _i = 0; _i < _samples.Length; _i++)
        {
            Vector3 _origin = _samples[_i] + _up * _rayheight;
            if (!Physics.Raycast(_origin, -_up, out RaycastHit _hit, _rayheight * 2f, _groundmask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            if (!_havecenter)
            {
                _centerpoint = _hit.point;
                _havecenter = true;
            }
            float _slope = Vector3.Angle(_hit.normal, _up);
            if (_slope > _maxslopeangle)
            {
                return false;
            }
            float _heightdifference = Vector3.Dot(_hit.point - _centerpoint, _up);
            if (Mathf.Abs(_heightdifference) > _maxheightdifference)
            {
                return false;
            }
        }
        return true;
    }
    private bool hasallresources(buildingdefinition _building)
    {
        for (int _i = 0; _i < _building._cost.Length; _i++)
        {
            buildingcost _cost = _building._cost[_i];
            if (gettotalresource(_cost._itemid) < _cost._amount)
            {
                return false;
            }
        }
        return true;
    }
    private int gettotalresource(int _itemid)
    {
        int _amount = _inventory.GetTotalItemAmount(_itemid);
        Collider[] _colliders = Physics.OverlapSphere(
        transform.position,
        _resourcecheckradius
        );
        HashSet<itemworld> _items = new HashSet<itemworld>();
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            itemworld _world = _colliders[_i].GetComponentInParent<itemworld>();
            if (_world == null || _world.item == null || _world.item.definition == null) continue;
            if (_world.item.definition.id != _itemid) continue;
            _items.Add(_world);
        }
        foreach (itemworld _world in _items)
        {
            _amount += _world.item.amount;
        }
        return _amount;
    }
    private bool isaimingatghost()
    {
        if (_ghost == null || !_ghost.activeSelf) return false;
        Vector2 _mouse = Mouse.current.position.ReadValue();
        Ray _ray = _camera.ScreenPointToRay(_mouse);
        RaycastHit[] _hits = Physics.RaycastAll(
        _ray,
        _placementdistance,
        ~0,
        QueryTriggerInteraction.Collide
        );
        float _nearest = float.MaxValue;
        Transform _nearesttransform = null;
        for (int _i = 0; _i < _hits.Length; _i++)
        {
            if (_hits[_i].distance < _nearest)
            {
                _nearest = _hits[_i].distance;
                _nearesttransform = _hits[_i].collider.transform;
            }
        }
        if (_nearesttransform == null) return false;
        return _nearesttransform == _ghost.transform || _nearesttransform.IsChildOf(_ghost.transform);
    }
    private bool isplacementvalid()
    {
        if (_ghost == null || !_ghost.activeSelf || _selectedbuilding == null) return false;
        if (Vector3.Distance(transform.position, _ghost.transform.position) > _builddistance) return false;
        Vector3 _up = _ghost.transform.up;
        Vector3 _base = _ghost.transform.position - _up * (_selectedbuilding._size.y * 0.5f);
        if (!checksurface(_base, _up, _ghost.transform.rotation)) return false;
        if (!hasallresources(_selectedbuilding)) return false;
        if (!isaimingatghost()) return false;
        return true;
    }
    private void trybeginbuilding()
    {
        if (!isplacementvalid()) return;
        consumeresources(_selectedbuilding);
        _building = true;
        _buildprogress = 0f;
        _placementmode = false;
        setbuildui(false);
        if (_infopanel != null)
        {
            _infopanel.SetActive(false);
        }
    }
    private void updatebuilding()
    {
        if (_selectedbuilding == null)
        {
            cancelbuilding();
            return;
        }
        _buildprogress += Time.deltaTime;
        if (_buildprogress >= _selectedbuilding._buildtime)
        {
            finishbuilding();
        }
    }
    private void consumeresources(buildingdefinition _building)
    {
        for (int _i = 0; _i < _building._cost.Length; _i++)
        {
            buildingcost _cost = _building._cost[_i];
            int _remaining = _cost._amount;
            int _inventoryamount = _inventory.GetTotalItemAmount(_cost._itemid);
            int _frominventory = Mathf.Min(_inventoryamount, _remaining);
            if (_frominventory > 0)
            {
                _inventory.RemoveItemAmount(
                _cost._itemid,
                _frominventory
                );
                _remaining -= _frominventory;
            }
            if (_remaining <= 0) continue;
            Collider[] _colliders = Physics.OverlapSphere(
            transform.position,
            _resourcecheckradius
            );
            HashSet<itemworld> _items = new HashSet<itemworld>();
            for (int _j = 0; _j < _colliders.Length; _j++)
            {
                itemworld _world = _colliders[_j].GetComponentInParent<itemworld>();
                if (_world == null || _world.item == null || _world.item.definition == null) continue;
                if (_world.item.definition.id != _cost._itemid) continue;
                _items.Add(_world);
            }
            foreach (itemworld _world in _items)
            {
                if (_remaining <= 0) break;
                int _remove = Mathf.Min(_world.item.amount, _remaining);
                _world.item.amount -= _remove;
                _remaining -= _remove;
                if (_world.item.amount <= 0)
                {
                    Destroy(_world.gameObject);
                }
            }
        }
    }
    private void finishbuilding()
    {
        if (_selectedbuilding == null || _selectedbuilding._prefab == null)
        {
            cancelbuilding();
            return;
        }
        Instantiate(
        _selectedbuilding._prefab,
        _ghost.transform.position,
        _ghost.transform.rotation
        );
        destroyghost();
        _selectedbuilding = null;
        _building = false;
        _buildprogress = 0f;
        if (_infopanel != null)
        {
            _infopanel.SetActive(false);
        }
        setbuildui(false);
    }
    private void cancelbuilding()
    {
        if (_building) return;
        destroyghost();
        _selectedbuilding = null;
        _catalogopen = false;
        _placementmode = false;
        _buildprogress = 0f;
        if (_catalog != null)
        {
            _catalog.SetActive(false);
        }
        if (_infopanel != null)
        {
            _infopanel.SetActive(false);
        }
        setbuildui(false);
    }
    private void destroyghost()
    {
        if (_ghost != null)
        {
            Destroy(_ghost);
            _ghost = null;
        }
    }
    private void setghostcolor(Color _color)
    {
        if (_ghost == null) return;
        Renderer[] _renderers = _ghost.GetComponentsInChildren<Renderer>();
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            Material[] _materials = _renderers[_i].materials;
            for (int _j = 0; _j < _materials.Length; _j++)
            {
                _materials[_j].color = _color;
            }
        }
    }
    private void setbuildui(bool _value)
    {
        if (_cursorlogic != null)
        {
            _cursorlogic.uiMode = _value;
        }
        if (_lookaction != null)
        {
            if (_value)
            {
                _lookaction.Disable();
            }
            else
            {
                _lookaction.Enable();
            }
        }
        Cursor.visible = _value;
        Cursor.lockState = _value ? CursorLockMode.None : CursorLockMode.Locked;
    }
}