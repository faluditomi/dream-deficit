using UnityEngine;
using UnityEngine.EventSystems;

public class ChatLogController : MonoBehaviour
{
    private ChatLog chatLog;
    private GameObject chatBubblePrefab;

    public async void Setup(Transform content, ChatLog chatLog)
    {
        this.chatLog = chatLog;
        chatBubblePrefab = await AddressableController.Instance().RetrieveAddressable<GameObject>(Constants.Addressable.ChatBubble);

        foreach(ChatBubble chatBubble in chatLog.messages)
        {
            ChatBubbleUI chatBubbleInstance = Instantiate(chatBubblePrefab, content).GetComponent<ChatBubbleUI>();
            chatBubbleInstance.Setup(chatBubble);
        }

        chatLog.isOpen = true;
    }

    public void Close(PointerEventData eventData)
    {
        chatLog.isOpen = false;
        Destroy(gameObject);
    }
}
