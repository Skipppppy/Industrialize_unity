using UnityEngine;
using static item_data_general;
public class itemworld : MonoBehaviour
{
    public item_data_general definition;
    [SerializeField] private ItemTag[] tags;
    public ItemTag[] Tags => tags;
    [HideInInspector] public itemz item;
    private void Awake()
    {
        if (item == null)
        {
            item = new itemz
            {
                definition = definition,
                amount = 1,
                durability = 100f
            };
        }
        if (item.definition == null)
            item.definition = definition;
    }
    public bool canCutTree()
    {
        return item != null && item.canCutTree;
    }
    public float getToolDamage()
    {
        if (item == null)
            return 0f;
        return item.toolDamage;
    }
}