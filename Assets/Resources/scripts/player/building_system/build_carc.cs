
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class buildingcard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Preview")]
    [SerializeField] private RawImage _model;
    [SerializeField] private float _rotationspeed = 60f;
    [SerializeField] private float _previewlightintensity = 1f;
    [SerializeField] private float _previewdistance = 8f;
    [SerializeField] private float _previewfov = 30f;
    [Header("Background Brightness")]
    [SerializeField, Range(0f, 1f)] private float _backgroundunselectedbrightness = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _backgroundselectedbrightness = 0.75f;
    [Header("Select Overlay Brightness")]
    [SerializeField] private Image _selectimage;
    [SerializeField, Range(0f, 1f)] private float _selectunselectedbrightness = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _selectselectedbrightness = 0.75f;
    [SerializeField, Range(0f, 1f)] private float _selectunselectedalpha = 0f;
    [SerializeField, Range(0f, 1f)] private float _selecthoveralpha = 0.15f;
    [SerializeField, Range(0f, 1f)] private float _selectselectedalpha = 0.25f;
    private Image _background;
    private Button _button;
    private buildingdefinition _building;
    private buildingmanager _manager;
    private GameObject _previewroot;
    private GameObject _previewmodel;
    private Camera _previewcamera;
    private Light _previewlight;
    private RenderTexture _rendertexture;
    private bool _selected;
    private bool _hovered;
    private int _previewlayer = 31;
    private void Awake()
    {
        _background = GetComponent<Image>();
        _button = GetComponent<Button>();
        _background.enabled = true;
        _background.raycastTarget = true;
        _button.targetGraphic = _background;
        _button.transition = Selectable.Transition.None;
        _button.onClick.RemoveListener(oncardclick);
        _button.onClick.AddListener(oncardclick);
        if (_model == null)
        {
            Transform _modeltransform = findchild(transform, "model");
            if (_modeltransform != null)
                _model = _modeltransform.GetComponent<RawImage>();
        }
        if (_selectimage == null)
        {
            Transform _selecttransform = findchild(transform, "select");
            if (_selecttransform != null)
                _selectimage = _selecttransform.GetComponent<Image>();
        }
        if (_model != null)
        {
            _model.raycastTarget = false;
            _model.color = Color.white;
        }
        if (_selectimage != null)
            _selectimage.raycastTarget = false;
        updatelook();
    }
    public void initialize(buildingdefinition _buildingdefinition, buildingmanager _buildingmanager)
    {
        _building = _buildingdefinition;
        _manager = _buildingmanager;
        _selected = false;
        _hovered = false;
        createpreview();
        updatelook();
    }
    private Transform findchild(Transform _root, string _name)
    {
        Transform[] _children = _root.GetComponentsInChildren<Transform>(true);
        for (int _i = 0; _i < _children.Length; _i++)
        {
            if (string.Equals(_children[_i].name, _name, System.StringComparison.OrdinalIgnoreCase))
                return _children[_i];
        }
        return null;
    }
    private void createpreview()
    {
        cleanuppreview();
        if (_building == null || _building._prefab == null || _model == null)
            return;
        _previewroot = new GameObject("building_card_preview");
        _previewroot.transform.position = new Vector3(Random.Range(10000f, 20000f), 10000f, Random.Range(10000f, 20000f));
        _previewmodel = Instantiate(_building._prefab, _previewroot.transform);
        _previewmodel.transform.localPosition = Vector3.zero;
        _previewmodel.transform.localRotation = Quaternion.identity;
        setlayer(_previewmodel, _previewlayer);
        disablepreviewcomponents(_previewmodel);
        Renderer[] _renderers = _previewmodel.GetComponentsInChildren<Renderer>(true);
        if (_renderers.Length == 0)
            return;
        Bounds _bounds = _renderers[0].bounds;
        for (int _i = 1; _i < _renderers.Length; _i++)
            _bounds.Encapsulate(_renderers[_i].bounds);
        Vector3 _difference = _bounds.center - _previewroot.transform.position;
        _previewmodel.transform.position -= _difference;
        _renderers = _previewmodel.GetComponentsInChildren<Renderer>(true);
        _bounds = _renderers[0].bounds;
        for (int _i = 1; _i < _renderers.Length; _i++)
            _bounds.Encapsulate(_renderers[_i].bounds);
        float _size = Mathf.Max(_bounds.extents.x, Mathf.Max(_bounds.extents.y, _bounds.extents.z));
        _size = Mathf.Max(_size, 0.1f);
        _previewcamera = new GameObject("building_card_camera").AddComponent<Camera>();
        _previewcamera.transform.SetParent(_previewroot.transform, false);
        _previewcamera.transform.localPosition = new Vector3(0f, 0f, -_size * _previewdistance);
        _previewcamera.transform.LookAt(_previewroot.transform.position);
        _previewcamera.clearFlags = CameraClearFlags.SolidColor;
        _previewcamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _previewcamera.cullingMask = 1 << _previewlayer;
        _previewcamera.fieldOfView = _previewfov;
        _previewcamera.nearClipPlane = 0.01f;
        _previewcamera.farClipPlane = Mathf.Max(100f, _size * 30f);
        _previewcamera.allowHDR = false;
        _previewcamera.allowMSAA = false;
        _previewlight = new GameObject("building_card_light").AddComponent<Light>();
        _previewlight.transform.SetParent(_previewroot.transform, false);
        _previewlight.type = LightType.Point;
        _previewlight.intensity = _previewlightintensity;
        _previewlight.range = Mathf.Max(10f, _size * 8f);
        _previewlight.transform.localPosition = new Vector3(2f, 2f, -2f);
        _rendertexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        _rendertexture.Create();
        _previewcamera.targetTexture = _rendertexture;
        _model.texture = _rendertexture;
    }
    private void disablepreviewcomponents(GameObject _object)
    {
        MonoBehaviour[] _scripts = _object.GetComponentsInChildren<MonoBehaviour>(true);
        for (int _i = 0; _i < _scripts.Length; _i++)
            _scripts[_i].enabled = false;
        Collider[] _colliders = _object.GetComponentsInChildren<Collider>(true);
        for (int _i = 0; _i < _colliders.Length; _i++)
            _colliders[_i].enabled = false;
        Rigidbody[] _rigidbodies = _object.GetComponentsInChildren<Rigidbody>(true);
        for (int _i = 0; _i < _rigidbodies.Length; _i++)
        {
            _rigidbodies[_i].isKinematic = true;
            _rigidbodies[_i].useGravity = false;
            _rigidbodies[_i].detectCollisions = false;
        }
        Animator[] _animators = _object.GetComponentsInChildren<Animator>(true);
        for (int _i = 0; _i < _animators.Length; _i++)
            _animators[_i].enabled = false;
        ParticleSystem[] _particles = _object.GetComponentsInChildren<ParticleSystem>(true);
        for (int _i = 0; _i < _particles.Length; _i++)
            _particles[_i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    private void setlayer(GameObject _object, int _layer)
    {
        _object.layer = _layer;
        Transform[] _transforms = _object.GetComponentsInChildren<Transform>(true);
        for (int _i = 0; _i < _transforms.Length; _i++)
            _transforms[_i].gameObject.layer = _layer;
    }
    private void Update()
    {
        if (!_selected || _previewmodel == null)
            return;
        _previewmodel.transform.Rotate(Vector3.up, _rotationspeed * Time.unscaledDeltaTime, Space.World);
    }
    private void oncardclick()
    {
        if (_manager == null)
            return;
        _manager.selectcard(this, _building);
    }
    public void OnPointerEnter(PointerEventData _eventdata)
    {
        _hovered = true;
        updatelook();
    }
    public void OnPointerExit(PointerEventData _eventdata)
    {
        _hovered = false;
        updatelook();
    }
    public void setselected(bool _value)
    {
        _selected = _value;
        if (_selectimage != null)
        {
            _selectimage.gameObject.SetActive(true);
        }
        updatelook();
    }
    private void updatelook()
    {
        bool _highlighted = _selected || _hovered;
        if (_background != null)
        {
            float _brightness = _highlighted ? _backgroundselectedbrightness : _backgroundunselectedbrightness;
            _background.color = new Color(_brightness, _brightness, _brightness, 1f);
        }
        if (_selectimage != null)
        {
            float _brightness = _highlighted ? _selectselectedbrightness : _selectunselectedbrightness;
            float _alpha = _selected ? _selectselectedalpha : _hovered ? _selecthoveralpha : _selectunselectedalpha;
            _selectimage.color = new Color(_brightness, _brightness, _brightness, _alpha);
        }
    }
    public buildingdefinition getbuilding()
    {
        return _building;
    }
    private void cleanuppreview()
    {
        if (_previewcamera != null)
        {
            _previewcamera.targetTexture = null;
            Destroy(_previewcamera.gameObject);
            _previewcamera = null;
        }
        if (_previewlight != null)
        {
            Destroy(_previewlight.gameObject);
            _previewlight = null;
        }
        if (_previewmodel != null)
        {
            Destroy(_previewmodel);
            _previewmodel = null;
        }
        if (_previewroot != null)
        {
            Destroy(_previewroot);
            _previewroot = null;
        }
        if (_rendertexture != null)
        {
            _rendertexture.Release();
            Destroy(_rendertexture);
            _rendertexture = null;
        }
        if (_model != null)
            _model.texture = null;
    }
    private void OnDestroy()
    {
        cleanuppreview();
    }
}