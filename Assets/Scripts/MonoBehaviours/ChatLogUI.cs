using UnityEngine;
using UnityEngine.UI;

public class ChatLogUI : MonoBehaviour
{
    public ChatLog chatLog;
    private GameObject chatBubblePrefab;
    private Transform content;

    private void Start()
    {
        InitialiseChatLog();
    }

    private async void InitialiseChatLog()
    {
        content = GetComponentInChildren<ContentSizeFitter>().transform;
        chatBubblePrefab = await AddressableController.Instance().RetrieveAddressable<GameObject>(Constants.Addressable.ChatBubble);

        if(chatLog == null || content == null ) return;

        foreach(ChatBubble chatBubble in chatLog.messages)
        {
            ChatBubbleUI chatBubbleInstance = Instantiate(chatBubblePrefab, content).GetComponent<ChatBubbleUI>();
            chatBubbleInstance.Setup(chatBubble);
        }
    }
}
