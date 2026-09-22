using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragWindow : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RectTransform window;
    public Canvas canvas;

    private Vector2 pointerOffset;

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransform parent = window.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        pointerOffset = window.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform parent = window.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        Vector2 targetPos = localPoint + pointerOffset;

        window.anchoredPosition = ClampToParent(targetPos, parent);
    }

    private Vector2 ClampToParent(Vector2 pos, RectTransform parent)
    {
        window.anchoredPosition = pos;

        Vector3[] w = new Vector3[4];
        Vector3[] p = new Vector3[4];

        window.GetWorldCorners(w);
        parent.GetWorldCorners(p);

        Vector3 offset = Vector3.zero;
        if (w[0].x < p[0].x)
            offset.x = p[0].x - w[0].x;
        if (w[2].x > p[2].x)
            offset.x = p[2].x - w[2].x;
        if (w[0].y < p[0].y)
            offset.y = p[0].y - w[0].y;
        if (w[2].y > p[2].y)
            offset.y = p[2].y - w[2].y;

        window.anchoredPosition += new Vector2(offset.x, offset.y);

        return window.anchoredPosition;
    }
}