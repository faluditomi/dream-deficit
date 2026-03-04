using UnityEngine;
using UnityEngine.EventSystems;

public class ChatBubbleController : MonoBehaviour
{
    public void PressBubble(PointerEventData evetnData)
    {
        Debug.Log("bubble down");
    }

    public void ReleaseBubble(PointerEventData eventData)
    {
        Debug.Log("bubble up");
    }
}
