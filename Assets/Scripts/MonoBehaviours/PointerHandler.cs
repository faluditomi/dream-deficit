using UnityEngine;
using UnityEngine.EventSystems;

public class PointerHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public event System.Action<PointerEventData> OnPointerDownEvent;
    public event System.Action<PointerEventData> OnPointerUpEvent;
    public event System.Action<PointerEventData> OnPointerClickEvent;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(OnPointerClickEvent != null) OnPointerClickEvent.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(OnPointerUpEvent != null) OnPointerUpEvent.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(OnPointerDownEvent != null) OnPointerDownEvent.Invoke(eventData);
    }
}
