using UnityEngine;
using UnityEngine.EventSystems;

public class ChatBubbleController : MonoBehaviour
{
    public void OnBubbleButtonPointerUp(PointerEventData eventData)
    {
        Debug.Log("bubble up");
    }

    public void OnBubbleButtonPointerDown(PointerEventData evetnData)
    {
        Debug.Log("bubble down");
    }
}
