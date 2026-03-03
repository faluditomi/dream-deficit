using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogUI : MonoBehaviour
{
    private ChatLogController chatLogController;
    private PointerHandler closePointerHandler;
    
    public ChatLog chatLog;
    private GameObject chatBubblePrefab;
    private Transform content;

    private void Start()
    {
        // NOTE: setup might have to return a bool later like in ChatBubbleUI
        Setup();
    }

    // TODO: separate this into Setup() and FindElements() like in ChatBubbleUI
    private async void Setup()
    {
        chatLogController = GetComponent<ChatLogController>();
        content = GetComponentInChildren<ContentSizeFitter>().transform;
        chatBubblePrefab = await AddressableController.Instance().RetrieveAddressable<GameObject>(Constants.Addressable.ChatBubble);
        Transform topBar = transform.Find(Constants.GameObjectNames.TopBar);
        topBar.AddComponent<DragHandler>().objectToDrag = transform;
        closePointerHandler = topBar.Find(Constants.GameObjectNames.CloseButton).AddComponent<PointerHandler>();

        closePointerHandler.OnPointerClickEvent += chatLogController.Close;

        if(chatLogController  == null || chatLog == null || content == null )
        {
            Debug.LogError("Setup of ChatLog failed. A necessary component wasn't found during setup.");
            return;
        }

        foreach(ChatBubble chatBubble in chatLog.messages)
        {
            ChatBubbleUI chatBubbleInstance = Instantiate(chatBubblePrefab, content).GetComponent<ChatBubbleUI>();
            chatBubbleInstance.Setup(chatBubble);
        }
    }

    private void OnDestroy()
    {
        if(!closePointerHandler || !chatLogController)
        {
            closePointerHandler.OnPointerUpEvent -= chatLogController.Close;
        }
    }
}
