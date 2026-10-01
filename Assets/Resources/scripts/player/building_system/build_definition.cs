using UnityEngine;
[CreateAssetMenu(fileName = "building_", menuName = "Building/Building Definition")]
public class buildingdefinition : ScriptableObject
{
    public int _id;
    public string _name;
    [TextArea] public string _description;
    public Sprite _icon;
    public string _category;
    public GameObject _prefab;
    public Vector3 _size = Vector3.one;
    public float _buildtime = 3f;
    public buildingcost[] _cost;
}