using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform objectToDrag;
    private Vector2 offset;

    private bool isDragging = false;

    public void Setup(Transform objectToDrag)
    {
        this.objectToDrag = objectToDrag;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var hit = eventData.pointerCurrentRaycast.gameObject;
        if(hit == null || hit.transform != transform) return;
        isDragging = true;
        offset = (Vector2)objectToDrag.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!isDragging) return;
        objectToDrag.position = eventData.position + offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
}
