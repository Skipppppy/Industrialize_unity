using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class neardysystemrow : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image icon;

    private itemworld world;
    private neardysystem system;

    public void Setup(itemworld _world, neardysystem _system)
    {
        world = _world;
        system = _system;

        if (icon == null)
            return;

        if (world == null)
            return;

        if (world.item != null && world.item.definition != null)
        {
            icon.sprite = world.item.definition.icon;
            icon.enabled = true;
            return;
        }

        if (world.definition != null)
        {
            icon.sprite = world.definition.icon;
            icon.enabled = true;
            return;
        }

        icon.sprite = null;
        icon.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (world == null)
            return;

        if (system == null)
            return;

        system.StartDrag(world);
    }
}