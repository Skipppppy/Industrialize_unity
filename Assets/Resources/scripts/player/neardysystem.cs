using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class neardysystem : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float refreshInterval = 0.15f;
    [SerializeField] private LayerMask itemLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [SerializeField] private Transform content;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image dragImage;
    [SerializeField] private EasyInv easyInv;
    [SerializeField] private EasyInvUI inventoryUI;
    [SerializeField] private CursorManager ui;
    private Vector3 draggedPosition;
    private Quaternion draggedRotation;
    private readonly Collider[] hits = new Collider[64];
    private readonly Dictionary<itemworld, GameObject> rows = new Dictionary<itemworld, GameObject>();

    public List<itemworld> nearbyItems = new List<itemworld>();

    private bool dragging;
    private itemworld draggedWorld;
    private float refreshTimer;

    private void Update()
    {
        refreshTimer += Time.deltaTime;

        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            Scan();
        }

        if (dragging)
            UpdateDrag();
    }

    public void Scan()
    {
        List<itemworld> foundItems = new List<itemworld>();

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            hits,
            itemLayer,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            Collider hit = hits[i];

            if (hit == null)
                continue;

            itemworld world = hit.GetComponentInParent<itemworld>();

            if (world == null)
                continue;

            if (world.definition == null && world.item == null)
                continue;

            if (foundItems.Contains(world))
                continue;

            Vector3 start = transform.position + Vector3.up * 1.6f;
            Vector3 end = world.transform.position;

            Vector3 direction = end - start;
            float distance = direction.magnitude;

            if (distance > 0.01f)
            {
                if (Physics.Raycast(
                    start,
                    direction.normalized,
                    distance,
                    obstacleLayer,
                    QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
            }

            foundItems.Add(world);
        }

        foundItems.Sort((a, b) =>
        {
            if (a == null)
                return 1;

            if (b == null)
                return -1;

            float distanceA = Vector3.SqrMagnitude(
                transform.position - a.transform.position
            );

            float distanceB = Vector3.SqrMagnitude(
                transform.position - b.transform.position
            );

            return distanceB.CompareTo(distanceA);
        });

        nearbyItems.Clear();
        nearbyItems.AddRange(foundItems);

        RefreshRows();
    }

    private void RefreshRows()
    {
        List<itemworld> removeItems = new List<itemworld>();

        foreach (KeyValuePair<itemworld, GameObject> pair in rows)
        {
            itemworld world = pair.Key;
            GameObject row = pair.Value;

            if (world == null ||
                !nearbyItems.Contains(world))
            {
                if (row != null)
                    Destroy(row);

                removeItems.Add(world);
            }
        }

        foreach (itemworld world in removeItems)
        {
            rows.Remove(world);
        }

        for (int i = 0; i < nearbyItems.Count; i++)
        {
            itemworld world = nearbyItems[i];

            if (world == null)
                continue;

            GameObject row;

            if (!rows.TryGetValue(world, out row))
            {
                row = Instantiate(
                    rowPrefab,
                    content,
                    false
                );

                neardysystemrow rowLogic =
                    row.GetComponent<neardysystemrow>();

                if (rowLogic == null)
                {
                    Destroy(row);
                    continue;
                }

                rowLogic.Setup(world, this);

                rows.Add(world, row);
            }

            if (row != null)
                row.transform.SetSiblingIndex(i);
        }
    }

    public void StartDrag(itemworld world)
    {
        if (world == null)
            return;

        if (inventoryUI == null)
            return;

        itemz item;

        if (world.item != null)
        {
            item = world.item;
        }
        else
        {
            if (world.definition == null)
                return;

            item = new itemz();
            item.definition = world.definition;
            item.amount = 1;
            item.durability = 100f;
        }

        if (item == null || item.definition == null)
            return;

        draggedWorld = world;

        draggedPosition = world.transform.position;
        draggedRotation = world.transform.rotation;

        bool started = inventoryUI.StartExternalDrag(
            item,
            () =>
            {
                Scan();
            },
            () =>
            {
                GameObject obj = Instantiate(
                    item.definition.prefab,
                    draggedPosition,
                    draggedRotation
                );

                itemworld restoredWorld =
                    obj.GetComponent<itemworld>();

                if (restoredWorld != null)
                    restoredWorld.item = item;

                Scan();
            }
        );

        if (!started)
            return;

        RemoveItem(draggedWorld);

        Destroy(draggedWorld.gameObject);
    }

    private itemz GetWorldItem(itemworld world)
    {
        if (world == null)
            return null;

        if (world.item != null)
            return world.item;

        if (world.definition == null)
            return null;

        itemz item = new itemz();

        item.definition = world.definition;
        item.amount = 1;
        item.durability = 100f;

        return item;
    }

    private void UpdateDrag()
    {
        if (!dragging)
            return;

        FollowMouse();

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            FinishDrag();
        }
    }

    private void FollowMouse()
    {
        if (Mouse.current == null)
            return;

        if (canvas == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera,
            out Vector2 localPosition
        );

        if (dragImage != null)
            dragImage.rectTransform.localPosition = localPosition;
    }

    private void FinishDrag()
    {
        if (!dragging)
            return;

        if (draggedWorld == null)
        {
            StopDrag();
            return;
        }

        if (inventoryUI == null)
        {
            StopDrag();
            return;
        }

        itemz item = GetWorldItem(draggedWorld);

        if (item == null || item.definition == null)
        {
            StopDrag();
            return;
        }

        EasyInvUI.Slot targetSlot =
            inventoryUI.GetSlotUnderMouse();

        if (targetSlot == null)
        {
            StopDrag();
            return;
        }

        if (targetSlot.item != null)
        {
            StopDrag();
            return;
        }

        switch (targetSlot.type)
        {
            case EasyInvUI.SlotType.LeftHand:
                {
                    if (easyInv.GetItem(0) != null)
                    {
                        StopDrag();
                        return;
                    }

                    easyInv.ReturnItemToHand(0, item);
                    break;
                }

            case EasyInvUI.SlotType.RightHand:
                {
                    if (easyInv.GetItem(1) != null)
                    {
                        StopDrag();
                        return;
                    }

                    easyInv.ReturnItemToHand(1, item);
                    break;
                }

            case EasyInvUI.SlotType.Inventory:
                {
                    int inventorySlot =
                        inventoryUI.GetInventorySlotIndex(targetSlot);

                    if (inventorySlot < 0)
                    {
                        StopDrag();
                        return;
                    }

                    if (!easyInv.ReturnItemToInventory(
                        inventorySlot,
                        item))
                    {
                        StopDrag();
                        return;
                    }

                    break;
                }

            default:
                {
                    targetSlot.item = item;
                    break;
                }
        }

        itemworld pickedWorld = draggedWorld;

        StopDrag();

        RemoveItem(pickedWorld);

        if (pickedWorld != null)
            Destroy(pickedWorld.gameObject);

        inventoryUI.Refresh();
    }

    private void StopDrag()
    {
        dragging = false;
        draggedWorld = null;

        if (dragImage != null)
        {
            dragImage.sprite = null;
            dragImage.gameObject.SetActive(false);
        }
    }

    public void RemoveItem(itemworld world)
    {
        if (world == null)
            return;

        if (rows.TryGetValue(world, out GameObject row))
        {
            if (row != null)
                Destroy(row);
        }

        rows.Remove(world);
        nearbyItems.Remove(world);
    }

    public void PickupItem(itemworld world)
    {
        if (world == null)
            return;

        if (easyInv == null)
            return;

        if (easyInv.PickUp(world))
        {
            RemoveItem(world);
        }
    }

    public int getnearbyitemamount(int _itemid)
    {
        int _amount = 0;
        for (int _i = nearbyItems.Count - 1; _i >= 0; _i--)
        {
            itemworld _world = nearbyItems[_i];
            if (_world == null)
                continue;
            if (_world.item != null && _world.item.definition != null)
            {
                if (_world.item.definition.id != _itemid)
                    continue;
                _amount += _world.item.amount;
                continue;
            }
            if (_world.definition == null)
                continue;
            if (_world.definition.id != _itemid)
                continue;
            _amount++;
        }
        return _amount;
    }

    public bool removenearbyitemamount(int _itemid, int _amounttoremove)
    {
        if (_amounttoremove <= 0)
            return true;
        if (getnearbyitemamount(_itemid) < _amounttoremove)
            return false;
        int _remaining = _amounttoremove;
        for (int _i = nearbyItems.Count - 1; _i >= 0 && _remaining > 0; _i--)
        {
            itemworld _world = nearbyItems[_i];
            if (_world == null)
                continue;
            int _worldamount;
            if (_world.item != null && _world.item.definition != null)
            {
                if (_world.item.definition.id != _itemid)
                    continue;
                _worldamount = _world.item.amount;
            }
            else
            {
                if (_world.definition == null || _world.definition.id != _itemid)
                    continue;
                _worldamount = 1;
            }
            int _remove = Mathf.Min(_worldamount, _remaining);
            if (_world.item != null && _world.item.definition != null)
            {
                _world.item.amount -= _remove;
            }
            _remaining -= _remove;
            if (_world.item == null || _world.item.amount <= 0)
            {
                RemoveItem(_world);
                if (_world != null)
                    Destroy(_world.gameObject);
            }
        }
        return _remaining <= 0;
    }

    public float getradius()
    {
        return radius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}