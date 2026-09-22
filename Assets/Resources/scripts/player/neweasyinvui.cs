using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class neweasyinvui : MonoBehaviour
{
    public CursorManager ui;
    public Image draghand;
    public Image dragicon;
    public bool dragging;
    [SerializeField] private InputAction LBM;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private EasyInv inv;
    public slotindex slot;
    public int handIndex;
    private itemz draggedItem;
    private int draggedItemId;
    private int draggedHandIndex;
    public void Start()
    {
        dragicon.gameObject.SetActive(false);
        handIndex = handcheck();
    }
    private void Awake()
    {
        LBM = playerInput.actions["drag"];
    }
    private void OnEnable() => LBM.Enable();
    private void OnDisable() => LBM.Disable();
    void Update()
    {
        dragicon.transform.position = Input.mousePosition;
        draghand.transform.position = Input.mousePosition;
    }

    private void drag(InputAction.CallbackContext context)
    {
        if (!ui.uiMode)
        {
            return;
        }
        startdrag();
    }
    private int handcheck()
    {
        if (EventSystem.current == null || Mouse.current == null)
            return -1;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            slotindex slot = result.gameObject.GetComponent<slotindex>();

            if (slot != null)
                return slot.slotIndex;
        }

        return -1;
    }
    public void startdrag()
    {
        draggedHandIndex = handcheck();

        if (draggedHandIndex == -1)
            return;

        draggedItem = inv.TakeItem(draggedHandIndex);

        if (draggedItem == null)
            return;

        draggedItemId = draggedItem.definition.id;

        draghand.sprite = Resources.Load<Sprite>("prefabs/UI/hand_tyaga");
    }
}
