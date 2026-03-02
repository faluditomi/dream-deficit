using UnityEngine;
using UnityEngine.EventSystems;

public class ChatLogController : MonoBehaviour
{
    public void Close(PointerEventData eventData)
    {
        Destroy(gameObject);
    }
}
