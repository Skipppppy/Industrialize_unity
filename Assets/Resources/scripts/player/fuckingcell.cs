using UnityEngine;
using UnityEngine.EventSystems;

public class slotindex : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private EasyInvUI inventoryUI;
    [SerializeField] public int slotIndex;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        inventoryUI.StartSlotDrag(slotIndex);
    }
}