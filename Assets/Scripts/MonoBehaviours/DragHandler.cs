using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public Transform objectToDrag;
    private Vector2 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        offset = (Vector2)objectToDrag.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        objectToDrag.position = eventData.position + offset;
    }
}
