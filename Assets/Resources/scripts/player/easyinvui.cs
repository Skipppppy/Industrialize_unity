using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EasyInvUI : MonoBehaviour
{
    public enum SlotType
    {
        Inventory,
        RightHand,
        LeftHand,
        Backpack,
        Head,
        Belt,
        Shield,
        Leg,
        Ear
    }
    [SerializeField] private ScrollRect scrollRect;
    [Serializable]
    public class Slot
    {
        public Image cell;
        public Image icon;
        public SlotType type;
        [HideInInspector] public itemz item;
    }

    [SerializeField] private EasyInv easyInv;
    [SerializeField] private CursorManager cursorManager;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slot[] slots;
    [SerializeField] private Image dragImage;
    [SerializeField] private Image draghand;
    [SerializeField] private Image dragicon;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    private int sourceInventorySlot = -1;
    private bool dragging;
    private itemz draggedItem;
    private int draggedItemId;
    private Slot sourceSlot;
    private int sourceHand = -1;
    private bool externalDragging;
    private System.Action externalSuccess;
    private System.Action externalCancel;
    private void Start()
    {
        if (dragImage != null)
            dragImage.gameObject.SetActive(false);

        if (draghand != null)
            draghand.gameObject.SetActive(false);

        if (dragicon != null)
            dragicon.gameObject.SetActive(false);

        Refresh();
    }

    private void Update()
    {
        if (cursorManager == null)
            return;

        if (!cursorManager.uiMode)
        {
            if (dragging)
                CancelDrag();

            return;
        }

        if (dragging)
            UpdateDrag();
    }

    public void StartHandDrag(int handIndex)
    {
        if (dragging)
            return;

        if (handIndex < 0 || handIndex > 1)
            return;

        itemz item = easyInv.GetItem(handIndex);

        if (item == null)
            return;

        draggedItem = easyInv.TakeItem(handIndex);

        if (draggedItem == null)
            return;

        draggedItemId = draggedItem.definition.id;
        sourceHand = handIndex;
        sourceInventorySlot = -1;
        sourceSlot = null;

        ClearHandSlot(handIndex);

        StartDragVisual();
    }

    public void StartSlotDrag(int slotIndex)
    {
        if (dragging)
            return;

        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        Slot slot = slots[slotIndex];

        if (slot == null)
            return;

        if (slot.type == SlotType.LeftHand)
        {
            StartHandDrag(0);
            return;
        }

        if (slot.type == SlotType.RightHand)
        {
            StartHandDrag(1);
            return;
        }

        if (slot.item == null)
            return;

        draggedItem = slot.item;

        if (draggedItem.definition == null)
            return;

        draggedItemId = draggedItem.definition.id;
        sourceSlot = slot;
        sourceHand = -1;
        sourceInventorySlot = -1;

        if (slot.type == SlotType.Inventory)
        {
            int inventoryIndex = GetInventorySlotIndex(slot);

            if (inventoryIndex < 0)
                return;

            itemz takenItem = easyInv.TakeInventoryItem(inventoryIndex);

            if (takenItem == null)
                return;

            draggedItem = takenItem;
            sourceInventorySlot = inventoryIndex;
        }

        slot.item = null;

        StartDragVisual();
        Refresh();
    }

    private void ClearHandSlot(int handIndex)
    {
        SlotType type = handIndex == 0
            ? SlotType.LeftHand
            : SlotType.RightHand;

        foreach (Slot slot in slots)
        {
            if (slot == null)
                continue;

            if (slot.type != type)
                continue;

            slot.item = null;

            if (slot.icon != null)
            {
                slot.icon.enabled = false;
                slot.icon.sprite = null;
            }

            if (slot.cell != null)
                slot.cell.color = emptyColor;
        }
    }

    private void StartDragVisual()
    {
        scrollRect.enabled = false;
        if (draggedItem == null)
            return;

        dragging = true;

        if (dragImage != null)
        {
            dragImage.sprite = draggedItem.definition.icon;
            dragImage.color = Color.white;
            dragImage.rectTransform.rotation = Quaternion.identity;
            dragImage.gameObject.SetActive(true);
        }

        if (dragicon != null)
        {
            dragicon.sprite = draggedItem.definition.icon;
            dragicon.color = Color.white;
            dragicon.rectTransform.rotation = Quaternion.identity;
            dragicon.gameObject.SetActive(true);
        }

        if (draghand != null)
        {
            draghand.sprite = Resources.Load<Sprite>("prefabs/UI/hand_tyaga");
            draghand.color = Color.white;
            draghand.gameObject.SetActive(true);
        }
    }

    private void UpdateDrag()
    {
        FollowMouse();
        HandleRotation();
        HighlightSlot();

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            FinishDrag();
    }

    private void FollowMouse()
    {
        if (Mouse.current == null)
            return;

        if (canvas == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        RectTransform canvasRect = canvas.transform as RectTransform;

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

        if (draghand != null)
            draghand.rectTransform.localPosition = localPosition;

        if (dragicon != null)
            dragicon.rectTransform.localPosition = localPosition;
    }

    private void HandleRotation()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll == 0f)
            return;

        float rotation = scroll > 0f ? 15f : -15f;

        if (dragicon != null)
            dragicon.rectTransform.Rotate(0f, 0f, rotation);
    }

    public Slot GetSlotUnderMouse()
    {
        if (EventSystem.current == null)
            return null;

        if (Mouse.current == null)
            return null;

        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();

        EventSystem.current.RaycastAll(pointer, results);

        foreach (RaycastResult result in results)
        {
            foreach (Slot slot in slots)
            {
                if (slot == null)
                    continue;

                if (slot.cell != null && result.gameObject == slot.cell.gameObject)
                    return slot;

                if (slot.icon != null && result.gameObject == slot.icon.gameObject)
                    return slot;
            }
        }

        return null;
    }

    private bool CanPlace(Slot slot)
    {
        if (draggedItem == null)
            return false;

        if (slot == null)
            return false;

        if (slot.item != null)
            return false;

        switch (slot.type)
        {
            case SlotType.Inventory:
                return true;

            case SlotType.RightHand:
                return true;

            case SlotType.LeftHand:
                return true;

            case SlotType.Backpack:
                return HasTag(draggedItem, item_data_general.ItemTag.Backpack);

            case SlotType.Head:
                return HasTag(draggedItem, item_data_general.ItemTag.Head);

            case SlotType.Belt:
                return HasTag(draggedItem, item_data_general.ItemTag.Belt);

            case SlotType.Shield:
                return HasTag(draggedItem, item_data_general.ItemTag.Shield);

            case SlotType.Leg:
                return HasTag(draggedItem, item_data_general.ItemTag.Leg);

            case SlotType.Ear:
                return HasTag(draggedItem, item_data_general.ItemTag.Ear);
        }

        return false;
    }

    private bool HasTag(itemz item, item_data_general.ItemTag tag)
    {
        if (item == null)
            return false;

        if (item.Tags == null)
            return false;

        foreach (item_data_general.ItemTag itemTag in item.Tags)
        {
            if (itemTag == tag)
                return true;
        }

        return false;
    }

    private void HighlightSlot()
    {
        Slot hoveredSlot = GetSlotUnderMouse();

        foreach (Slot slot in slots)
        {
            if (slot == null)
                continue;

            if (slot.cell == null)
                continue;

            slot.cell.color = slot.item != null
                ? filledColor
                : emptyColor;
        }

        if (hoveredSlot != null && hoveredSlot.cell != null)
        {
            hoveredSlot.cell.color = CanPlace(hoveredSlot)
                ? filledColor
                : invalidColor;
        }
    }

    private void FinishDrag()
    {
        scrollRect.enabled = true;

        Slot targetSlot = GetSlotUnderMouse();

        if (targetSlot == null)
        {
            DropDraggedItem();
            return;
        }

        if (!CanPlace(targetSlot))
        {
            if (externalDragging)
            {
                CancelExternalDrag();
                return;
            }

            RestoreSource();
            StopDrag();
            Refresh();
            return;
        }

        if (targetSlot.type == SlotType.LeftHand)
        {
            easyInv.ReturnItemToHand(0, draggedItem);

            if (externalDragging)
            {
                CompleteExternalDrag();
                return;
            }

            StopDrag();
            Refresh();
            return;
        }

        if (targetSlot.type == SlotType.RightHand)
        {
            easyInv.ReturnItemToHand(1, draggedItem);

            if (externalDragging)
            {
                CompleteExternalDrag();
                return;
            }

            StopDrag();
            Refresh();
            return;
        }

        if (targetSlot.type == SlotType.Inventory)
        {
            int inventorySlot = GetInventorySlotIndex(targetSlot);

            if (inventorySlot < 0)
            {
                if (externalDragging)
                {
                    CancelExternalDrag();
                    return;
                }

                RestoreSource();
                StopDrag();
                Refresh();
                return;
            }

            if (!easyInv.ReturnItemToInventory(inventorySlot, draggedItem))
            {
                if (externalDragging)
                {
                    CancelExternalDrag();
                    return;
                }

                RestoreSource();
                StopDrag();
                Refresh();
                return;
            }

            if (externalDragging)
            {
                CompleteExternalDrag();
                return;
            }

            StopDrag();
            Refresh();
            return;
        }

        targetSlot.item = draggedItem;

        if (externalDragging)
        {
            CompleteExternalDrag();
            return;
        }

        StopDrag();
        Refresh();
    }
    private void CompleteExternalDrag()
    {
        System.Action callback = externalSuccess;

        externalDragging = false;
        externalSuccess = null;
        externalCancel = null;

        StopDrag();
        Refresh();

        callback?.Invoke();
    }
    private void CancelExternalDrag()
    {
        System.Action callback = externalCancel;

        externalDragging = false;
        externalSuccess = null;
        externalCancel = null;

        StopDrag();
        Refresh();

        callback?.Invoke();
    }
    private void RestoreSource()
    {
        if (draggedItem == null)
            return;

        if (sourceHand >= 0)
        {
            easyInv.ReturnItemToHand(sourceHand, draggedItem);
            return;
        }

        if (sourceInventorySlot >= 0)
        {
            if (!easyInv.ReturnItemToInventory(sourceInventorySlot, draggedItem))
            {
                easyInv.AddItemToInventory(draggedItem);
            }

            return;
        }

        if (sourceSlot != null)
            sourceSlot.item = draggedItem;
    }

    private void CancelDrag()
    {
        if (externalDragging)
        {
            CancelExternalDrag();
            return;
        }

        RestoreSource();
        StopDrag();
        Refresh();
    }

    private void StopDrag()
    {
        dragging = false;
        draggedItem = null;
        draggedItemId = 0;
        sourceSlot = null;
        sourceHand = -1;
        sourceInventorySlot = -1;

        if (dragImage != null)
        {
            dragImage.sprite = null;
            dragImage.gameObject.SetActive(false);
        }

        if (draghand != null)
        {
            draghand.sprite = null;
            draghand.gameObject.SetActive(false);
        }

        if (dragicon != null)
        {
            dragicon.sprite = null;
            dragicon.gameObject.SetActive(false);
        }
    }

    public void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];

            if (slot == null)
                continue;

            if (slot.type == SlotType.LeftHand)
            {
                if (!(dragging && sourceHand == 0))
                    slot.item = easyInv.GetItem(0);
            }
            else if (slot.type == SlotType.RightHand)
            {
                if (!(dragging && sourceHand == 1))
                    slot.item = easyInv.GetItem(1);
            }

            if (slot.item == null)
            {
                if (slot.icon != null)
                {
                    slot.icon.enabled = false;
                    slot.icon.sprite = null;
                }

                if (slot.cell != null)
                    slot.cell.color = emptyColor;

                continue;
            }

            if (slot.icon != null)
            {
                slot.icon.enabled = true;
                slot.icon.sprite = slot.item.definition.icon;
            }

            if (slot.cell != null)
                slot.cell.color = filledColor;
        }

        easyInv.RefreshUI();
    }
    private void DropDraggedItem()
    {
        if (draggedItem == null)
            return;

        if (externalDragging)
        {
            CancelExternalDrag();
            return;
        }

        easyInv.DropItem(draggedItem);

        StopDrag();
        Refresh();
    }
    private void Drop(int handIndex)
    {
        if (draggedItem == null)
            return;

        easyInv.DropItem(draggedItem);

        StopDrag();
        Refresh();
    }
    public Slot GetSlotFromObject(GameObject obj)
    {
        if (obj == null)
            return null;

        foreach (Slot slot in slots)
        {
            if (slot == null)
                continue;

            if (slot.cell != null && obj == slot.cell.gameObject)
                return slot;

            if (slot.icon != null && obj == slot.icon.gameObject)
                return slot;
        }

        return null;
    }
    public int GetInventorySlotIndex(Slot targetSlot)
    {
        if (targetSlot == null)
            return -1;

        int index = 0;

        foreach (Slot slot in slots)
        {
            if (slot == null)
                continue;

            if (slot.type != SlotType.Inventory)
                continue;

            if (slot == targetSlot)
                return index;

            index++;
        }

        return -1;
    }
    public bool StartExternalDrag(
    itemz item,
    System.Action onSuccess,
    System.Action onCancel)
    {
        if (dragging)
            return false;

        if (item == null || item.definition == null)
            return false;

        draggedItem = item;
        draggedItemId = item.definition.id;
        sourceSlot = null;
        sourceHand = -1;

        externalDragging = true;
        externalSuccess = onSuccess;
        externalCancel = onCancel;

        StartDragVisual();

        return true;
    }
}