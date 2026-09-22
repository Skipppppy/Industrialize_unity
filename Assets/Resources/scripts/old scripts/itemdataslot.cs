using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Analytics.IAnalytic;

public class itemdataslot : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public enum SlotType
    {
        righthand,
        lefthand,
        leftbackpack,
        rightbackpack,
        centralbackpack,
        right_ear,
        left_ear,
        head,
        leftbelt,
        rightbelt,
        centralbelt,
        leftleg,
        rightleg,
        shield
    }

    public enum slot
    {
        consumable,
        hand
    }

    public itemsprefabs.itemtypeenum requiredItemType;
    public itemsprefabs.typeconsumable requiredConsumableType;

    public slot slote;
    public SlotType slotType;
    public Image cell;

    public int id;
    public int amount;
    public bool durability;
    public int durability_;

    public playerinventoryhandsmanager hand;

    public static itemdataslot currentSlot;

    void Update()
    {
        //if (slotType == SlotType.righthand && hand.righthandempty == false)
        //{
        //    id = hand.righthandid;
        //    amount = hand.amountright;
        //    durability = hand.durabilityright;
        //    durability_ = hand.durabilityitemright;
        //}

        //if (slotType == SlotType.lefthand &&
        //    hand.lefthandempty == false)
        //{
        //  id = hand.lefthandid;
        //    amount = hand.amountleft;
        //    durability = hand.durabilityleft;
        //    durability_ = hand.durabilityitemleft;
        //}
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        currentSlot = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentSlot == this)
        {
            currentSlot = null;
        }
    }

    public void updateui()
    {
        if (id != 0)
        {
            Sprite sprite = Resources.Load<Sprite>("Items/" + id);
            Debug.Log("UPDATE UI CALL id = " + id);
            cell.sprite = sprite;
            cell.enabled = true;
            cell.color = Color.white;
        }
        else
        {
            cell.enabled = false;
            Debug.Log("UPDATE UI CALL id = " + id);
        }
    }
}