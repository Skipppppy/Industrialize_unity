public class itemz
{
    public item_data_general definition;
    public int amount = 1;
    public float durability = 100f;

    public item_data_general.ItemTag[] Tags
    {
        get
        {
            if (definition == null)
                return null;

            return definition.Tags;
        }
    }

    public bool istool
    {
        get
        {
            return
                definition != null &&
                definition.itemTypeEnum ==
                item_data_general.itemtypeenum.tool;
        }
    }

    public bool canCutTree
    {
        get
        {
            return
                definition != null &&
                definition.canCutTree;
        }
    }

    public float toolDamage
    {
        get
        {
            if (definition == null)
                return 0f;

            return definition.toolDamage;
        }
    }

    public item_data_general.tooltypeenum toolType
    {
        get
        {
            if (definition == null)
                return item_data_general.tooltypeenum.none;

            return definition.toolType;
        }
    }
}