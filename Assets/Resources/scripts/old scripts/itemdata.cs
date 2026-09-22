using UnityEngine;

public class itemdata : MonoBehaviour
{
    [Header("Type Item(ID)")]

    [SerializeField] public int itemType;
    [SerializeField] public int amount;
    [SerializeField] public bool durability;
    [SerializeField] public float durabilityItem;

    public GameObject prefab;
    public int ItemType => itemType;
    public int Amount => amount;
    public bool Durability => durability;
    public float DurabilityItem => durabilityItem;
}