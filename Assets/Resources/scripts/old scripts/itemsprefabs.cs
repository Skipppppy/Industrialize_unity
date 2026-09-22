using System.Collections.Generic;
using UnityEngine;
using static itemdata;

[System.Serializable]
public class itemsprefabs
{
    [SerializeField] public itemtypeenum itemTypeEnum;
    [SerializeField] public typeconsumable typeconsumablee;

    public enum typeconsumable
    {
        backpack,
        none
    }

    public enum itemtypeenum
    {
        consumable,
        material,
        none
    }

    public string itemName;
    public int id;
    public GameObject prefab;
    public int height;
    public int width;
    public List<ItemVariant> variants = new();
}