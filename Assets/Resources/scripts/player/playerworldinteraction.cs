using UnityEngine;
using UnityEngine.InputSystem;
public class playerinteractor : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private EasyInv _inventory;
    [SerializeField] private float _distance = 4f;
    [SerializeField] private LayerMask _interactmask;
    private void Awake()
    {
        if (_camera == null)
            _camera = GetComponentInChildren<Camera>(true);
        if (_inventory == null)
            _inventory = GetComponent<EasyInv>();
    }
    private void Update()
    {
        if (Mouse.current == null)
            return;
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;
        interact();
    }
    private void interact()
    {
        if (_camera == null || _inventory == null)
            return;
        itemz _item = _inventory.getmainhanditem();
        if (_item == null || _item.definition == null)
            return;
        if (!_item.canCutTree)
            return;
        Ray _ray = new Ray(
            _camera.transform.position,
            _camera.transform.forward
        );
        if (!Physics.Raycast(
            _ray,
            out RaycastHit _hit,
            _distance,
            _interactmask,
            QueryTriggerInteraction.Ignore
        ))
            return;
        tree _tree = _hit.collider.GetComponentInParent<tree>();
        if (_tree == null)
            return;
        _tree.damage(
            _item.toolDamage,
            _hit.point
        );
    }
}