using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class item_data_general : ScriptableObject
{
    [SerializeField] public itemtypeenum itemTypeEnum;

    public enum ItemTag
    {
        Backpack,
        Head,
        Belt,
        Shield,
        Leg,
        Ear
    }

    public enum itemtypeenum
    {
        consumable,
        material,
        tool,
        none
    }

    public enum tooltypeenum
    {
        none,
        axe,
        saw,
        pickaxe,
        shovel,
        hammer,
        knife
    }

    public int id;
    public string itemName;
    public string itemDescription;
    public Sprite icon;
    public GameObject prefab;
    public float weight;
    public int maxStack;
    public Vector2Int size;
    public List<ItemVariant> variants = new();

    [Header("Tool")]
    public tooltypeenum toolType = tooltypeenum.none;
    public float toolDamage = 0f;
    public bool canCutTree = false;

    [SerializeField] private ItemTag[] tags;

    public ItemTag[] Tags => tags;
}