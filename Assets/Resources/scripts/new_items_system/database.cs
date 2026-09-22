using UnityEngine;
using System.Collections.Generic;

public class database : MonoBehaviour
{
    public static database Instance;

    private Dictionary<int, item_data_general> items = new();

    private void Awake()
    {
        Instance = this;

        item_data_general[] allItems =
            Resources.LoadAll<item_data_general>("Items");

        foreach (var item in allItems)
        {
            items.Add(item.id, item);
        }
    }

    public item_data_general Get(int id)
    {
        return items[id];
    }
}