using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class EasyInv : MonoBehaviour
{
    [SerializeField] private itemz[] hands = new itemz[2];
    [SerializeField] private itemz[] inventoryItems = new itemz[10];
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private CursorManager ui;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupDistance = 5f;
    [SerializeField] private LayerMask It;
    [SerializeField] private Image[] handImages = new Image[2];
    [SerializeField] private Image[] cellImages = new Image[2];
    [SerializeField] private Image[] inventoryImages = new Image[10];
    [SerializeField] private Image[] inventoryCells = new Image[10];
    [SerializeField] private Color emptyCellColor = new Color(1f, 1f, 1f, 0.5f);
    public float maxPickupWeight = 2500f;
    private InputAction pickupAction;
    private InputAction dropAction;
    private InputAction changeHandAction;
    private int activeHand = 1;
    public int ActiveHand => activeHand;
    private void Awake()
    {
        pickupAction = playerInput.actions["PickUp"];
        dropAction = playerInput.actions["DropUp"];
        changeHandAction = playerInput.actions["SwapUp"];
    }
    private void Start()
    {
        Refresh();
    }
    private void OnEnable()
    {
        pickupAction.Enable();
        dropAction.Enable();
        changeHandAction.Enable();
        pickupAction.performed += OnPickup;
        dropAction.performed += OnDrop;
        changeHandAction.performed += OnChangeHand;
    }
    private void OnDisable()
    {
        pickupAction.performed -= OnPickup;
        dropAction.performed -= OnDrop;
        changeHandAction.performed -= OnChangeHand;
        pickupAction.Disable();
        dropAction.Disable();
        changeHandAction.Disable();
    }
    public bool AddItem(itemz item)
    {
        if (item == null || item.definition == null)
            return false;
        if (hands[activeHand] == null)
        {
            hands[activeHand] = item;
            Refresh();
            return true;
        }
        int otherHand = 1 - activeHand;
        if (hands[otherHand] == null)
        {
            hands[otherHand] = item;
            Refresh();
            return true;
        }
        return false;
    }
    public itemz getmainhanditem()
    {
        if (hands == null || hands.Length == 0)
            return null;
        int _index = activeHand;
        if (_index < 0 || _index >= hands.Length)
            _index = 0;
        return hands[_index];
    }
    public bool AddItemToInventory(itemz item)
    {
        if (item == null || item.definition == null)
            return false;
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
            {
                inventoryItems[i] = item;
                Refresh();
                return true;
            }
        }
        return false;
    }
    public bool ReturnItemToInventory(int slotIndex, itemz item)
    {
        if (item == null || item.definition == null)
            return false;
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length)
            return false;
        if (inventoryItems[slotIndex] != null)
            return false;
        inventoryItems[slotIndex] = item;
        Refresh();
        return true;
    }
    public itemz GetItem(int handIndex)
    {
        if (handIndex < 0 || handIndex >= hands.Length)
            return null;
        return hands[handIndex];
    }
    public itemz GetInventoryItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length)
            return null;
        return inventoryItems[slotIndex];
    }
    public itemz TakeItem(int handIndex)
    {
        if (handIndex < 0 || handIndex >= hands.Length)
            return null;
        itemz item = hands[handIndex];
        if (item == null)
            return null;
        hands[handIndex] = null;
        Refresh();
        return item;
    }
    public itemz TakeInventoryItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventoryItems.Length)
            return null;
        itemz item = inventoryItems[slotIndex];
        if (item == null)
            return null;
        inventoryItems[slotIndex] = null;
        Refresh();
        return item;
    }
    public void ReturnItemToHand(int handIndex, itemz item)
    {
        if (item == null || item.definition == null)
            return;
        if (handIndex < 0 || handIndex >= hands.Length)
            return;
        if (hands[handIndex] != null)
            return;
        hands[handIndex] = item;
        Refresh();
    }

    public bool SwapInventoryItemWithHand(int inventorySlot, int handIndex, itemz item)
    {
        if (item == null || item.definition == null)
            return false;
        if (inventorySlot < 0 || inventorySlot >= inventoryItems.Length)
            return false;
        if (handIndex < 0 || handIndex >= hands.Length)
            return false;
        if (inventoryItems[inventorySlot] != null)
            return false;
        itemz handItem = hands[handIndex];
        hands[handIndex] = item;
        inventoryItems[inventorySlot] = handItem;
        Refresh();
        return true;
    }
    public bool HasFreeHand()
    {
        return hands[0] == null || hands[1] == null;
    }
    public bool HasFreeInventorySlot()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
                return true;
        }
        return false;
    }
    private void OnChangeHand(InputAction.CallbackContext context)
    {
        if (ui.uiMode)
            return;
        activeHand = 1 - activeHand;
        Debug.Log("Активная рука: " + activeHand);
        Refresh();
    }
    private void OnPickup(InputAction.CallbackContext context)
    {
        if (ui.uiMode)
            return;
        Ray ray = new Ray(
        playerCamera.transform.position,
        playerCamera.transform.forward
        );
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance, It))
            return;
        itemworld world = hit.collider.GetComponent<itemworld>();
        if (world == null)
            return;
        if (world.definition == null && world.item == null)
            return;
        float weight = world.item != null
        ? world.item.definition.weight
        : world.definition.weight;
        if (weight > maxPickupWeight)
            return;
        PickUp(world);
    }
    private void OnDrop(InputAction.CallbackContext context)
    {
        if (ui.uiMode)
            return;
        Drop(activeHand);
    }
    public bool PickUp(itemworld world)
    {
        if (world == null)
            return false;
        itemz item;
        if (world.item == null)
        {
            item = new itemz();
            item.definition = world.definition;
            item.amount = 1;
            item.durability = 100f;
        }
        else
        {
            item = world.item;
        }
        if (item == null || item.definition == null)
            return false;
        if (!AddItem(item))
            return false;
        Destroy(world.gameObject);
        return true;
    }
    public void Drop(int handIndex)
    {
        itemz item = TakeItem(handIndex);
        if (item == null || item.definition == null)
            return;
        Vector3 spawnPos =
        playerCamera.transform.position +
        playerCamera.transform.forward * 1.2f;
        GameObject obj = Instantiate(
        item.definition.prefab,
        spawnPos,
        Quaternion.identity
        );
        itemworld world = obj.GetComponent<itemworld>();
        if (world != null)
            world.item = item;
    }
    public int GetTotalItemAmount(int _itemid)
    {
        int _amount = 0;
        for (int _i = 0; _i < hands.Length; _i++)
        {
            itemz _item = hands[_i];
            if (_item == null || _item.definition == null)
                continue;
            if (_item.definition.id != _itemid)
                continue;
            _amount += _item.amount;
        }
        for (int _i = 0; _i < inventoryItems.Length; _i++)
        {
            itemz _item = inventoryItems[_i];
            if (_item == null || _item.definition == null)
                continue;
            if (_item.definition.id != _itemid)
                continue;
            _amount += _item.amount;
        }
        return _amount;
    }
    public bool RemoveItemAmount(int _itemid, int _amounttoremove)
    {
        if (_amounttoremove <= 0)
            return true;
        if (GetTotalItemAmount(_itemid) < _amounttoremove)
            return false;
        int _remaining = _amounttoremove;
        for (int _i = 0; _i < hands.Length; _i++)
        {
            if (_remaining <= 0)
                break;
            itemz _item = hands[_i];
            if (_item == null || _item.definition == null)
                continue;
            if (_item.definition.id != _itemid)
                continue;
            int _remove = Mathf.Min(_item.amount, _remaining);
            _item.amount -= _remove;
            _remaining -= _remove;
            if (_item.amount <= 0)
            {
                hands[_i] = null;
            }
        }
        for (int _i = 0; _i < inventoryItems.Length; _i++)
        {
            if (_remaining <= 0)
                break;
            itemz _item = inventoryItems[_i];
            if (_item == null || _item.definition == null)
                continue;
            if (_item.definition.id != _itemid)
                continue;
            int _remove = Mathf.Min(_item.amount, _remaining);
            _item.amount -= _remove;
            _remaining -= _remove;
            if (_item.amount <= 0)
            {
                inventoryItems[_i] = null;
            }
        }
        Refresh();
        return _remaining <= 0;
    }
    private void Refresh()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            if (handImages[i] == null || cellImages[i] == null)
                continue;
            if (hands[i] == null || hands[i].definition == null)
            {
                handImages[i].enabled = false;
                handImages[i].sprite = null;
                cellImages[i].color = emptyCellColor;
            }
            else
            {
                handImages[i].enabled = true;
                handImages[i].sprite = hands[i].definition.icon;
                cellImages[i].color = Color.white;
            }
        }
        int count = Mathf.Min(
        inventoryItems.Length,
        inventoryImages.Length,
        inventoryCells.Length
        );
        for (int i = 0; i < count; i++)
        {
            if (inventoryImages[i] == null || inventoryCells[i] == null)
                continue;
            if (inventoryItems[i] == null || inventoryItems[i].definition == null)
            {
                inventoryImages[i].enabled = false;
                inventoryImages[i].sprite = null;
                inventoryCells[i].color = emptyCellColor;
            }
            else
            {
                inventoryImages[i].enabled = true;
                inventoryImages[i].sprite = inventoryItems[i].definition.icon;
                inventoryCells[i].color = Color.white;
            }
        }
    }
    public void RefreshUI()
    {
        Refresh();
    }
    public void DropItem(itemz item)
    {
        if (item == null || item.definition == null)
            return;
        Vector3 spawnPos =
        playerCamera.transform.position +
        playerCamera.transform.forward * 1.2f;
        GameObject obj = Instantiate(
        item.definition.prefab,
        spawnPos,
        Quaternion.identity
        );
        itemworld world = obj.GetComponent<itemworld>();
        if (world != null)
            world.item = item;
    }
    public int gettotalitemamount(int _itemid)
    {
        int _amount = 0;

        for (int _i = 0; _i < hands.Length; _i++)
        {
            if (hands[_i] == null || hands[_i].definition == null)
            {
                continue;
            }

            if (hands[_i].definition.id == _itemid)
            {
                _amount += hands[_i].amount;
            }
        }

        for (int _i = 0; _i < inventoryItems.Length; _i++)
        {
            if (inventoryItems[_i] == null || inventoryItems[_i].definition == null)
            {
                continue;
            }

            if (inventoryItems[_i].definition.id == _itemid)
            {
                _amount += inventoryItems[_i].amount;
            }
        }

        return _amount;
    }

    public bool removeitemamount(int _itemid, int _amount)
    {
        if (_amount <= 0)
        {
            return true;
        }

        if (gettotalitemamount(_itemid) < _amount)
        {
            return false;
        }

        int _remaining = _amount;

        for (int _i = 0; _i < hands.Length && _remaining > 0; _i++)
        {
            itemz _item = hands[_i];

            if (_item == null || _item.definition == null)
            {
                continue;
            }

            if (_item.definition.id != _itemid)
            {
                continue;
            }

            int _take = Mathf.Min(
            _item.amount,
            _remaining
            );

            _item.amount -= _take;
            _remaining -= _take;

            if (_item.amount <= 0)
            {
                hands[_i] = null;
            }
        }

        for (int _i = 0; _i < inventoryItems.Length && _remaining > 0; _i++)
        {
            itemz _item = inventoryItems[_i];

            if (_item == null || _item.definition == null)
            {
                continue;
            }

            if (_item.definition.id != _itemid)
            {
                continue;
            }

            int _take = Mathf.Min(
            _item.amount,
            _remaining
            );

            _item.amount -= _take;
            _remaining -= _take;

            if (_item.amount <= 0)
            {
                inventoryItems[_i] = null;
            }
        }

        Refresh();

        return _remaining <= 0;
    }
}