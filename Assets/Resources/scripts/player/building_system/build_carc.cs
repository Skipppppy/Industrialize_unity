using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class buildingcard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconimage;
    [SerializeField] private Image _selectimage;
    [SerializeField] private Transform _model;
    [SerializeField] private float _rotationspeed = 60f;
    [SerializeField] private float _selectedalpha = 1f;
    [SerializeField] private float _unselectedalpha = 0f;
    private buildingdefinition _building;
    private buildingmanager _manager;
    private bool _selected;
    public void initialize(buildingdefinition _buildingdefinition, buildingmanager _buildingmanager)
    {
        _building = _buildingdefinition;
        _manager = _buildingmanager;
        if (_iconimage == null)
        {
            Transform _icontransform = transform.Find("icon");
            if (_icontransform != null)
            {
                _iconimage = _icontransform.GetComponent<Image>();
            }
        }
        if (_selectimage == null)
        {
            Transform _selecttransform = transform.Find("select");
            if (_selecttransform != null)
            {
                _selectimage = _selecttransform.GetComponent<Image>();
            }
        }
        Graphic _graphic = GetComponent<Graphic>();
        if (_graphic != null)
        {
            _graphic.raycastTarget = true;
        }
        if (_iconimage != null)
        {
            _iconimage.sprite = _building._icon;
            _iconimage.enabled = _building._icon != null;
            _iconimage.raycastTarget = false;
        }
        if (_selectimage != null)
        {
            _selectimage.raycastTarget = false;
        }
        gameObject.SetActive(true);
        setselected(false);
    }
    private void Update()
    {
        if (!_selected || _model == null) return;
        _model.Rotate(
        Vector3.up,
        _rotationspeed * Time.deltaTime,
        Space.Self
        );
    }
    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData.button != PointerEventData.InputButton.Left) return;
        if (_manager == null) return;
        _manager.selectcard(this, _building);
    }
    public void setselected(bool _value)
    {
        _selected = _value;
        if (_selectimage == null) return;
        Color _color = _selectimage.color;
        _color.a = _selected ? _selectedalpha : _unselectedalpha;
        _selectimage.color = _color;
    }
}