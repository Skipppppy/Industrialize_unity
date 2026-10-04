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
    [SerializeField] private float _placementdistance = 8f;
    [SerializeField] private float _builddistance = 4f;
    [SerializeField] private float _resourcecheckradius = 3f;
    [SerializeField] private float _maxslopeangle = 8f;
    [SerializeField] private float _maxheightdifference = 0.2f;
    [SerializeField] private float _rayheight = 4f;
    [Header("Rotation")]
    [SerializeField] private float _rotation15 = 15f;
    [SerializeField] private float _rotation1 = 1f;
    [Header("Ghost")]
    [SerializeField] private Material _ghostmaterial;
    [SerializeField] private Color _ghostcolor = new Color(0.1f, 0.45f, 1f, 0.45f);
    [SerializeField] private Color _validcolor = new Color(0.1f, 1f, 0.2f, 0.45f);
    [SerializeField] private Color _invalidcolor = new Color(1f, 0.1f, 0.1f, 0.45f);
    private buildingcard _selectedcard;
    private buildingdefinition _selectedbuilding;
    private GameObject _ghost;
    private bool _catalogopen;
    private bool _placementmode;
    private bool _building;
    private bool _repeatbuild;
    private float _buildprogress;
    private float _ghostrotation;
    private buildingcategory _selectedcategory;
    private bool _cursorwasenabled;
    private Vector3 _lastghostposition;
    private Quaternion _lastghostrotation;
    private bool _hasghostposition;
    private void Awake()
    {
        if (_playercontroller == null && _playerinput != null)
        {
            _playercontroller = _playerinput.GetComponent<PlayerController>();
        }
        if (_catalog != null)
        {
            _catalog.SetActive(false);
        }
        if (_selectbutton != null)
        {
            _selectbutton.interactable = false;
            _selectbutton.onClick.AddListener(confirmselection);
        }
        if (_closebutton != null)
        {
            _closebutton.onClick.AddListener(closecatalog);
        }
        if (_liquidbutton != null)
        {
            _liquidbutton.onClick.AddListener(showliquid);
        }
        if (_electricitybutton != null)
        {
            _electricitybutton.onClick.AddListener(showelectricity);
        }
        if (_wallsbutton != null)
        {
            _wallsbutton.onClick.AddListener(showwalls);
        }
    }
    private void Update()
    {
        if (_inventory == null || _camera == null) return;
        if (_catalogopen && !_placementmode && !_building)
        {
            return;
        }
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (_placementmode && !_building)
            {
                cancelplacement();
                return;
            }
            if (_catalogopen && !_building)
            {
                closecatalog();
                return;
            }
            if (!_catalogopen && !_placementmode && !_building && hasbuildingplan())
            {
                opencatalog();
                return;
            }
        }
        if (_placementmode && !_building)
        {
            updaterotation();
            updateghost();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                trybeginbuilding(false);
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                bool _ctrl =
                Keyboard.current != null &&
                (Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed);
                if (_ctrl)
                {
                    trybeginbuilding(true);
                }
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
        if (_item == null || _item.definition == null)
        {
            return false;
        }
        return _item.definition.id == _buildingplanid;
    }
    private void opencatalog()
    {
        _catalogopen = true;
        _placementmode = false;
        _building = false;
        _selectedbuilding = null;
        _selectedcard = null;
        setselectbutton(false);
        if (_catalog != null)
        {
            _catalog.SetActive(true);
        }
        setbuildinguimode(true);
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
        _selectedcategory = _category;
        _selectedbuilding = null;
        _selectedcard = null;
        setselectbutton(false);
        if (_buildingcontent == null || _buildingcardprefab == null)
        {
            Debug.LogWarning("BuildingManager: Building Content или Building Card Prefab не назначены.");
            return;
        }
        for (int _i = _buildingcontent.childCount - 1; _i >= 0; _i--)
        {
            Destroy(_buildingcontent.GetChild(_i).gameObject);
        }
        for (int _i = 0; _i < _buildings.Count; _i++)
        {
            buildingdefinition _definition = _buildings[_i];
            if (_definition == null)
            {
                continue;
            }
            if (_definition._category != _selectedcategory)
            {
                continue;
            }
            buildingcard _card = Instantiate(
            _buildingcardprefab,
            _buildingcontent
            );
            _card.gameObject.SetActive(true);
            _card.initialize(
            _definition,
            this
            );
        }
    }
    public void selectcard(
    buildingcard _card,
    buildingdefinition _building)
    {
        if (_selectedcard != null)
        {
            _selectedcard.setselected(false);
        }
        _selectedcard = _card;
        _selectedbuilding = _building;
        _selectedcard.setselected(true);
        setselectbutton(true);
    }
    private void setselectbutton(bool _enabled)
    {
        if (_selectbutton != null)
        {
            _selectbutton.interactable = _enabled;
        }
    }
    private void confirmselection()
    {
        if (_selectedbuilding == null)
        {
            return;
        }
        _catalogopen = false;
        _placementmode = true;
        _building = false;
        _repeatbuild = false;
        _buildprogress = 0f;
        _ghostrotation = 0f;
        if (_catalog != null)
        {
            _catalog.SetActive(false);
        }
        createghost();
        setbuildinguimode(false);
    }
    private void closecatalog()
    {
        if (_building)
        {
            return;
        }
        _catalogopen = false;
        _selectedcard = null;
        _selectedbuilding = null;
        setselectbutton(false);
        if (_catalog != null)
        {
            _catalog.SetActive(false);
        }
        setbuildinguimode(false);
    }
    private void createghost()
    {
        destroyghost();
        if (_selectedbuilding == null ||
        _selectedbuilding._prefab == null)
        {
            return;
        }
        _ghost = Instantiate(
        _selectedbuilding._prefab
        );
        _ghost.name = "ghost_" + _selectedbuilding._id;
        Collider[] _colliders =
        _ghost.GetComponentsInChildren<Collider>();
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            _colliders[_i].isTrigger = true;
            _colliders[_i].enabled = true;
        }
        Rigidbody[] _rigidbodies =
        _ghost.GetComponentsInChildren<Rigidbody>();
        for (int _i = 0; _i < _rigidbodies.Length; _i++)
        {
            _rigidbodies[_i].isKinematic = true;
            _rigidbodies[_i].useGravity = false;
        }
        Renderer[] _renderers =
        _ghost.GetComponentsInChildren<Renderer>();
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            Material[] _materials = _renderers[_i].materials;
            for (int _j = 0; _j < _materials.Length; _j++)
            {
                _materials[_j] = createghostmaterial(
                _materials[_j]
                );
            }
            _renderers[_i].materials = _materials;
        }
        _ghost.SetActive(true);
        _lastghostposition = transform.position +
        transform.forward * 2f;
        _lastghostrotation = transform.rotation;
        _hasghostposition = false;
    }
    private Material createghostmaterial(Material _source)
    {
        Material _material;
        if (_ghostmaterial != null)
        {
            _material = new Material(_ghostmaterial);
        }
        else
        {
            _material = new Material(_source);
        }
        setmaterialcolor(_material, _ghostcolor);
        return _material;
    }
    private void setmaterialcolor(
    Material _material,
    Color _color)
    {
        if (_material == null) return;
        if (_material.HasProperty("_BaseColor"))
        {
            _material.SetColor(
            "_BaseColor",
            _color
            );
        }
        if (_material.HasProperty("_Color"))
        {
            _material.SetColor(
            "_Color",
            _color
            );
        }
        if (_material.HasProperty("_Surface"))
        {
            _material.SetFloat(
            "_Surface",
            1f
            );
            _material.SetFloat(
            "_Blend",
            0f
            );
            _material.SetFloat(
            "_SrcBlend",
            5f
            );
            _material.SetFloat(
            "_DstBlend",
            10f
            );
            _material.SetFloat(
            "_ZWrite",
            0f
            );
            _material.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
            );
            _material.renderQueue = 3000;
        }
        if (_material.HasProperty("_Mode"))
        {
            _material.SetFloat(
            "_Mode",
            2f
            );
            _material.SetInt(
            "_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.SrcAlpha
            );
            _material.SetInt(
            "_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );
            _material.SetInt(
            "_ZWrite",
            0
            );
            _material.EnableKeyword(
            "_ALPHABLEND_ON"
            );
            _material.renderQueue = 3000;
        }
    }
    private void updaterotation()
    {
        if (_ghost == null || Mouse.current == null)
        {
            return;
        }
        float _scroll =
        Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(_scroll) < 0.01f)
        {
            return;
        }
        bool _r =
        Keyboard.current != null &&
        Keyboard.current.rKey.isPressed;
        float _step = _r ? _rotation1 : _rotation15;
        if (_scroll > 0f)
        {
            _ghostrotation += _step;
        }
        else
        {
            _ghostrotation -= _step;
        }
    }
    private void updateghost()
    {
        if (_ghost == null ||
        _selectedbuilding == null)
        {
            return;
        }
        Ray _ray =
        _camera.ViewportPointToRay(
        new Vector3(0.5f, 0.5f, 0f)
        );
        if (!Physics.Raycast(
        _ray,
        out RaycastHit _hit,
        _placementdistance,
        _groundmask,
        QueryTriggerInteraction.Ignore))
        {
            if (_hasghostposition)
            {
                _ghost.transform.SetPositionAndRotation(
                _lastghostposition,
                _lastghostrotation
                );
            }
            _ghost.SetActive(true);
            setghostcolor(_invalidcolor);
            return;
        }
        Vector3 _up =
        _planet != null
        ? _planet.getup(_hit.point)
        : _hit.normal.normalized;
        Vector3 _forward =
        Vector3.ProjectOnPlane(
        _camera.transform.forward,
        _up
        );
        if (_forward.sqrMagnitude < 0.001f)
        {
            _forward =
            Vector3.ProjectOnPlane(
            Vector3.forward,
            _up
            );
        }
        _forward.Normalize();
        Quaternion _surfaceRotation =
        Quaternion.LookRotation(
        _forward,
        _up
        );
        Quaternion _rotation =
        _surfaceRotation *
        Quaternion.AngleAxis(
        _ghostrotation,
        _up
        );
        Vector3 _position =
        _hit.point +
        _up *
        (_selectedbuilding._size.y * 0.5f);
        bool _valid =
        checksurface(
        _hit.point,
        _up,
        _rotation
        );
        _ghost.transform.SetPositionAndRotation(
        _position,
        _rotation
        );
        _lastghostposition = _position;
        _lastghostrotation = _rotation;
        _hasghostposition = true;
        _ghost.SetActive(true);
        setghostcolor(
        _valid ?
        _validcolor :
        _invalidcolor
        );
    }
    private bool checksurface(
    Vector3 _center,
    Vector3 _up,
    Quaternion _rotation)
    {
        float _halfX =
        _selectedbuilding._size.x * 0.5f;
        float _halfZ =
        _selectedbuilding._size.z * 0.5f;
        Vector3[] _samples =
        {
_center,
_center+
_rotation*
new Vector3(_halfX,0f,_halfZ),
_center+
_rotation*
new Vector3(-_halfX,0f,_halfZ),
_center+
_rotation*
new Vector3(_halfX,0f,-_halfZ),
_center+
_rotation*
new Vector3(-_halfX,0f,-_halfZ)
};
        Vector3 _centerpoint = Vector3.zero;
        bool _havecenter = false;
        for (int _i = 0; _i < _samples.Length; _i++)
        {
            Vector3 _origin =
            _samples[_i] +
            _up *
            _rayheight;
            if (!Physics.Raycast(
            _origin,
            -_up,
            out RaycastHit _hit,
            _rayheight * 2f,
            _groundmask,
            QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            if (!_havecenter)
            {
                _centerpoint = _hit.point;
                _havecenter = true;
            }
            float _slope =
            Vector3.Angle(
            _hit.normal,
            _up
            );
            if (_slope > _maxslopeangle)
            {
                return false;
            }
            float _heightdifference =
            Vector3.Dot(
            _hit.point - _centerpoint,
            _up
            );
            if (Mathf.Abs(_heightdifference) >
            _maxheightdifference)
            {
                return false;
            }
        }
        return true;
    }
    private bool isaimingatghost()
    {
        if (_ghost == null ||
        !_ghost.activeSelf)
        {
            return false;
        }
        Ray _ray =
        _camera.ViewportPointToRay(
        new Vector3(0.5f, 0.5f, 0f)
        );
        RaycastHit[] _hits =
        Physics.RaycastAll(
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
                _nearesttransform =
                _hits[_i].collider.transform;
            }
        }
        if (_nearesttransform == null)
        {
            return false;
        }
        return
        _nearesttransform == _ghost.transform ||
        _nearesttransform.IsChildOf(
        _ghost.transform
        );
    }
    private bool isplacementvalid()
    {
        if (_ghost == null ||
        !_ghost.activeSelf ||
        _selectedbuilding == null)
        {
            return false;
        }
        if (Vector3.Distance(
        transform.position,
        _ghost.transform.position) >
        _builddistance)
        {
            return false;
        }
        Vector3 _up = _ghost.transform.up;
        Vector3 _base =
        _ghost.transform.position -
        _up *
        (_selectedbuilding._size.y * 0.5f);
        if (!checksurface(
        _base,
        _up,
        _ghost.transform.rotation))
        {
            return false;
        }
        if (!isaimingatghost())
        {
            return false;
        }
        return true;
    }
    private int getavailableamount(int _itemid)
    {
        int _amount =
        _inventory.GetTotalItemAmount(
        _itemid
        );
        Collider[] _colliders =
        Physics.OverlapSphere(
        transform.position,
        _resourcecheckradius
        );
        HashSet<itemworld> _items =
        new HashSet<itemworld>();
        for (int _i = 0; _i < _colliders.Length; _i++)
        {
            itemworld _world =
            _colliders[_i].GetComponentInParent<itemworld>();
            if (_world == null ||
            _world.item == null ||
            _world.item.definition == null)
            {
                continue;
            }
            if (_world.item.definition.id != _itemid)
            {
                continue;
            }
            _items.Add(_world);
        }
        foreach (itemworld _world in _items)
        {
            _amount += _world.item.amount;
        }
        return _amount;
    }
    private bool hasallresources(
    buildingdefinition _building)
    {
        for (int _i = 0; _i < _building._cost.Length; _i++)
        {
            buildingcost _cost =
            _building._cost[_i];
            if (getavailableamount(
            _cost._itemid
            ) <
            _cost._amount)
            {
                return false;
            }
        }
        return true;
    }
    private void trybeginbuilding(
    bool _keepghost)
    {
        if (!isplacementvalid())
        {
            return;
        }
        if (!hasallresources(
        _selectedbuilding))
        {
            return;
        }
        consumeresources(
        _selectedbuilding
        );
        _building = true;
        _repeatbuild = _keepghost;
        _buildprogress = 0f;
        setbuildinguimode(false);
        setghostcolor(_validcolor);
    }
    private void consumeresources(
    buildingdefinition _building)
    {
        for (int _i = 0; _i < _building._cost.Length; _i++)
        {
            buildingcost _cost =
            _building._cost[_i];
            int _remaining =
            _cost._amount;
            int _frominventory =
            Mathf.Min(
            _inventory.GetTotalItemAmount(
            _cost._itemid
            ),
            _remaining
            );
            if (_frominventory > 0)
            {
                _inventory.RemoveItemAmount(
                _cost._itemid,
                _frominventory
                );
                _remaining -= _frominventory;
            }
            if (_remaining <= 0)
            {
                continue;
            }
            Collider[] _colliders =
            Physics.OverlapSphere(
            transform.position,
            _resourcecheckradius
            );
            HashSet<itemworld> _items =
            new HashSet<itemworld>();
            for (int _j = 0; _j < _colliders.Length; _j++)
            {
                itemworld _world =
                _colliders[_j].GetComponentInParent<itemworld>();
                if (_world == null ||
                _world.item == null ||
                _world.item.definition == null)
                {
                    continue;
                }
                if (_world.item.definition.id != _cost._itemid)
                {
                    continue;
                }
                _items.Add(_world);
            }
            foreach (itemworld _world in _items)
            {
                if (_remaining <= 0)
                {
                    break;
                }
                int _remove =
                Mathf.Min(
                _world.item.amount,
                _remaining
                );
                _world.item.amount -= _remove;
                _remaining -= _remove;
                if (_world.item.amount <= 0)
                {
                    Destroy(
                    _world.gameObject
                    );
                }
            }
        }
    }
    private void updatebuilding()
    {
        if (_selectedbuilding == null)
        {
            _building = false;
            return;
        }
        _buildprogress +=
        Time.deltaTime;
        if (_buildprogress >=
        _selectedbuilding._buildtime)
        {
            finishbuilding();
        }
    }
    private void finishbuilding()
    {
        if (_selectedbuilding == null ||
        _selectedbuilding._prefab == null)
        {
            cancelplacement();
            return;
        }
        Instantiate(
        _selectedbuilding._prefab,
        _ghost.transform.position,
        _ghost.transform.rotation
        );
        _building = false;
        _buildprogress = 0f;
        if (_repeatbuild)
        {
            _repeatbuild = false;
            _placementmode = true;
            _ghost.SetActive(true);
            setbuildinguimode(false);
            return;
        }
        destroyghost();
        _selectedbuilding = null;
        _placementmode = false;
        setselectbutton(false);
        setbuildinguimode(false);
    }
    private void cancelplacement()
    {
        if (_building)
        {
            return;
        }
        destroyghost();
        _selectedbuilding = null;
        _selectedcard = null;
        _placementmode = false;
        _catalogopen = false;
        _buildprogress = 0f;
        _repeatbuild = false;
        if (_catalog != null)
        {
            _catalog.SetActive(false);
        }
        setselectbutton(false);
        setbuildinguimode(false);
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
        if (_ghost == null)
        {
            return;
        }
        Renderer[] _renderers =
        _ghost.GetComponentsInChildren<Renderer>();
        for (int _i = 0; _i < _renderers.Length; _i++)
        {
            Material[] _materials =
            _renderers[_i].materials;
            for (int _j = 0; _j < _materials.Length; _j++)
            {
                setmaterialcolor(
                _materials[_j],
                _color
                );
            }
        }
    }
    private void setbuildinguimode(bool _value)
    {
        if (_playercontroller == null && _playerinput != null)
        {
            _playercontroller =
            _playerinput.GetComponent<PlayerController>();
        }
        if (_value)
        {
            if (_cursorlogic != null)
            {
                _cursorwasenabled = _cursorlogic.enabled;
                _cursorlogic.enabled = false;
                _cursorlogic.uiMode = false;
            }
            if (_playercontroller != null)
            {
                _playercontroller.setbuildinguimode(true);
            }
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            if (_playercontroller != null)
            {
                _playercontroller.setbuildinguimode(false);
            }
            if (_cursorlogic != null)
            {
                _cursorlogic.enabled = _cursorwasenabled;
                _cursorlogic.uiMode = false;
            }
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}