using UnityEngine;
using UnityEngine.EventSystems;

public class ChatLogController : MonoBehaviour
{
    private GameObject chatBubblePrefab;

    public async void Setup(Transform content, ChatLog chatLog)
    {
        chatBubblePrefab = await AddressableController.Instance().RetrieveAddressable<GameObject>(Constants.Addressable.ChatBubble);

        foreach(ChatBubble chatBubble in chatLog.messages)
        {
            ChatBubbleUI chatBubbleInstance = Instantiate(chatBubblePrefab, content).GetComponent<ChatBubbleUI>();
            chatBubbleInstance.Setup(chatBubble);
        }
    }

    public void Close(PointerEventData eventData)
    {
        Destroy(gameObject);
    }
}
