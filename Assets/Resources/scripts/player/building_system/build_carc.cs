using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class buildingcard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RawImage _model;
    [SerializeField] private Image _selectimage;
    [SerializeField] private float _rotationspeed = 60f;
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
    private readonly Color _normalcolor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private readonly Color _selectedcolor = new Color(1f, 1f, 1f, 1f);
    private void Awake()
    {
        _background = GetComponent<Image>();
        _button = GetComponent<Button>();
        _background.enabled = true;
        _background.raycastTarget = true;
        _background.color = _normalcolor;
        _button.targetGraphic = _background;
        _button.transition = Selectable.Transition.None;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(oncardclick);
        if (_model == null)
        {
            Transform _modeltransform = transform.Find("model");
            if (_modeltransform != null)
                _model = _modeltransform.GetComponent<RawImage>();
        }
        if (_model != null)
        {
            _model.raycastTarget = false;
            _model.color = Color.white;
        }
        if (_selectimage == null)
        {
            Transform _selecttransform = transform.Find("select");
            if (_selecttransform != null)
                _selectimage = _selecttransform.GetComponent<Image>();
        }
        if (_selectimage != null)
        {
            _selectimage.raycastTarget = false;
            Color _color = _selectimage.color;
            _color.a = 0f;
            _selectimage.color = _color;
        }
    }
    public void initialize(buildingdefinition _buildingdefinition, buildingmanager _buildingmanager)
    {
        _building = _buildingdefinition;
        _manager = _buildingmanager;
        _selected = false;
        _hovered = false;
        if (_background != null)
        {
            _background.enabled = true;
            _background.raycastTarget = true;
            _background.color = _normalcolor;
        }
        if (_selectimage != null)
        {
            Color _color = _selectimage.color;
            _color.a = 0f;
            _selectimage.color = _color;
        }
        createpreview();
        updatelook();
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
        _previewcamera.transform.localPosition = new Vector3(0f, 0f, -_size * 8f);
        _previewcamera.transform.LookAt(_previewroot.transform.position);
        _previewcamera.clearFlags = CameraClearFlags.SolidColor;
        _previewcamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _previewcamera.cullingMask = 1 << _previewlayer;
        _previewcamera.fieldOfView = 30f;
        _previewcamera.nearClipPlane = 0.01f;
        _previewcamera.farClipPlane = Mathf.Max(100f, _size * 30f);
        _previewcamera.allowHDR = false;
        _previewcamera.allowMSAA = false;
        _previewlight = new GameObject("building_card_light").AddComponent<Light>();
        _previewlight.transform.SetParent(_previewroot.transform, false);
        _previewlight.type = LightType.Point;
        _previewlight.intensity = 4f;
        _previewlight.range = Mathf.Max(10f, _size * 10f);
        _previewlight.transform.localPosition = new Vector3(_size * 2f, _size * 2f, -_size * 2f);
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
        if (_previewmodel != null)
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
            Color _color = _selectimage.color;
            _color.a = _selected ? 1f : 0f;
            _selectimage.color = _color;
        }
        updatelook();
    }
    private void updatelook()
    {
        if (_background == null)
            return;
        _background.color = _selected || _hovered ? _selectedcolor : _normalcolor;
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
