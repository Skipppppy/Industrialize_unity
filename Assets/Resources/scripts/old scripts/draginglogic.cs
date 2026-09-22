using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static itemdataslot;

public class draginglogic : MonoBehaviour
{
    public Image draghand;
    public Image iconSprite;
    public bool dragging;
    public CursorManager ui;
    public playerinventoryhandsmanager hands;
    public inventoryhandsui sui;
    public itemsprefabs_ database;
    public Transform throwPoint;
    public itemdataslot data;
    public int did;
    public int damount;
    public bool ddurability;
    public int ddurability_;
    public int dwidth = 1;
    public int dheight = 1;
    public float cellSize = 64f;
    public float rotatespeeddrag = 180f;
    void Start()
    {
        iconSprite.gameObject.SetActive(false);
    }
    void Update()
    {
        iconSprite.transform.position = Input.mousePosition;
        if (!ui.uiMode && dragging)
        {
            DropItem();
            nullofdrag();
            return;
        }
        if (ui.uiMode)
        {
            if (itemdataslot.currentSlot != null && itemdataslot.currentSlot.slote == itemdataslot.slot.hand)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (itemdataslot.currentSlot.slotType == itemdataslot.SlotType.righthand && hands.righthandid != 0)
                    {
                        Sprite sprite =
                            Resources.Load<Sprite>(
                                "Items/" + hands.righthandid);
                        StartDrag(sprite);
                        hands.righthandempty = true;
                        did = hands.righthandid;
                        hands.righthandid = 0;
                        damount = hands.amountright;
                        hands.amountright = 0;
                        ddurability = hands.durabilityright;
                        hands.durabilityright = false;
                        ddurability_ = hands.durabilityitemright;
                        hands.durabilityitemright = 0;
                        sui.ClearUIRight();
                    }

                    if (itemdataslot.currentSlot.slotType == itemdataslot.SlotType.lefthand && hands.lefthandid != 0)
                    {
                        Sprite sprite =
                            Resources.Load<Sprite>(
                                "Items/" + hands.lefthandid);
                        StartDrag(sprite);
                        hands.lefthandempty = true;
                        did = hands.lefthandid;
                        hands.lefthandid = 0;
                        damount = hands.amountleft;
                        hands.amountleft = 0;
                        ddurability = hands.durabilityleft;
                        hands.durabilityleft = false;
                        ddurability_ = hands.durabilityitemleft;
                        hands.durabilityitemleft = 0;
                        sui.ClearUILeft();
                    }
                }
            }
            else if (itemdataslot.currentSlot != null && itemdataslot.currentSlot.slote == itemdataslot.slot.consumable)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var slot = itemdataslot.currentSlot;
                    did = slot.id;
                    damount = slot.amount;
                    ddurability = slot.durability;
                    ddurability_ = slot.durability_;
                    slot.id = 0;
                    slot.amount = 0;
                    slot.durability = false;
                    slot.durability_ = 0;
                    slot.updateui();
                    Sprite sprite =
                                Resources.Load<Sprite>(
                                    "Items/" + did);
                    StartDrag(sprite);
                }
            }
            if (did != 0 && dragging)
            {
                if (Input.GetKey(KeyCode.R))
                {
                    iconSprite.transform.Rotate(0, 0, rotatespeeddrag * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.T))
                {
                    iconSprite.transform.Rotate(0, 0, -rotatespeeddrag * Time.deltaTime);
                }
            }

            if (Input.GetMouseButtonUp(0) && dragging)
            {
                StopDrag();
            }
        }
    }

    public void StartDrag(Sprite itemSprite)
    {
        foreach (var item in database.items)
        {
            if (item.id == hands.righthandid)
            {
                dwidth = item.width;
                dheight = item.height;
            }
        }
        if (itemSprite == null)
        {
            return;
        }
        dragging = true;
        iconSprite.sprite = itemSprite;
        iconSprite.gameObject.SetActive(true);
        UpdateDragSize();
    }

    public void StopDrag()
    {
        if (itemdataslot.currentSlot == null)
        {
            DropItem();
            nullofdrag();
        }
        else
        {
            if (itemdataslot.currentSlot.slote == itemdataslot.slot.hand)
            {
                if (itemdataslot.currentSlot.slotType == itemdataslot.SlotType.righthand && hands.righthandid == 0)
                {
                    righthandfill();
                }
                else if (itemdataslot.currentSlot.slotType == itemdataslot.SlotType.lefthand && hands.lefthandid == 0)
                {
                    lefthandfill();
                }
                else if (itemdataslot.currentSlot.slotType == itemdataslot.SlotType.righthand && hands.righthandid != 0)
                {
                    if (hands.lefthandid == 0)
                    {
                        lefthandfill();
                    }
                    else
                    {
                        DropItem();
                        nullofdrag();
                    }
                }
                else if (itemdataslot.currentSlot.slotType == itemdataslot.SlotType.lefthand && hands.lefthandid != 0)
                {
                    if (hands.righthandid == 0)
                    {
                        righthandfill();
                    }
                    else
                    {
                        DropItem();
                        nullofdrag();

                    }
                }
            }
            if (itemdataslot.currentSlot.slote == itemdataslot.slot.consumable)
            {
                var slot = itemdataslot.currentSlot;

                itemsprefabs itemData = null;

                foreach (var item in database.items)
                {
                    if (item.id == did)
                    {
                        itemData = item;
                        break;
                    }
                }

                if (itemData == null)
                {
                    return;
                }

                bool canPlace =
                    itemData.itemTypeEnum == slot.requiredItemType &&
                    itemData.typeconsumablee == slot.requiredConsumableType;

                if (!canPlace || slot.id != 0)
                {
                    DropItem();
                    nullofdrag();
                    return;
                }
                else if (canPlace && slot.id == 0)
                {
                    fuckingcellfill();
                }
            }
        }
    }
    public void DropItem()
    {
        foreach (var item in database.items)
        {
            if (item.id == did)
            {
                Debug.Log(item.prefab.name);
                GameObject obj = Instantiate(item.prefab, throwPoint.position, throwPoint.rotation);

                Rigidbody rb = obj.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.collisionDetectionMode =
                        CollisionDetectionMode.Continuous;
                    Camera cam = Camera.main;
                    Ray ray = cam.ViewportPointToRay(
                        new Vector3(0.5f, 0.5f, 0f)
                    );
                    Vector3 throwDirection = ray.direction.normalized;
                    float throwForce = 8f;
                    rb.linearVelocity = throwDirection * throwForce;
                }
            }
        }
    }
    public void UpdateDragSize()
    {
        RectTransform rect =
            iconSprite.GetComponent<RectTransform>();

        rect.sizeDelta = new Vector2(
            dwidth * cellSize,
            dheight * cellSize
        );
    }
    public void righthandfill()
    {
        hands.righthandempty = false;
        hands.righthandid = did;
        hands.amountright = damount;
        hands.durabilityright = ddurability;
        hands.durabilityitemright = ddurability_;
        sui.UpdateUI();
        nullofdrag();
    }

    public void lefthandfill()
    {
        hands.lefthandempty = false;
        hands.lefthandid = did;
        hands.amountleft = damount;
        hands.durabilityleft = ddurability;
        hands.durabilityitemleft = ddurability_;
        sui.UpdateUI();
        nullofdrag();
    }

    public void nullofdrag()
    {
        did = 0;
        damount = 0;
        ddurability = false;
        ddurability_ = 0;
        dragging = false;
        iconSprite.gameObject.SetActive(false);
        iconSprite.sprite = null;
    }

    public void fuckingcellfill()
    {
        var slot = itemdataslot.currentSlot;
        Debug.Log("CURRENT SLOT: " + itemdataslot.currentSlot.name);
        slot.id = did;
        slot.amount = damount;
        slot.durability = ddurability;
        slot.durability_ = ddurability_;
        slot.updateui();
        nullofdrag();
    }

    public void draghandicon()
    {
        if (dragging)
        {
            Sprite draghand = Resources.Load<Sprite>("Prefabs/UI" + "hand_tyaga");
        }
    }
}
