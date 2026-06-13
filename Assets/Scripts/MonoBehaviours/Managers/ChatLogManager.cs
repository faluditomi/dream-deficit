using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogManager : Singleton<ChatLogManager>
{
    private GameObject chatLogPrefab;
    private Transform windowContainer;
    private Dictionary<ChatLog, ChatLogController> chatLogControllerCache = new Dictionary<ChatLog, ChatLogController>();

    protected override void Awake()
    {
        base.Awake();
        chatLogPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatLog);
        windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
    }

    public ChatLogController InstantiateChatLog(ChatLog chatLog, Button openButton)
    {
        if(chatLogControllerCache.ContainsKey(chatLog))
        {
            return chatLogControllerCache[chatLog];    
        }
        else
        {
            ChatLogController chatLogController = Instantiate(chatLogPrefab, windowContainer).GetComponent<ChatLogController>();
            chatLogController.Setup(chatLog);
            chatLogController.GetComponent<TopBarHandler>().Close();
            openButton.onClick.AddListener(() => {
                if(!chatLogController.GetIsOpen())
                {
                    chatLogController.Open();
                }
            });
            chatLogControllerCache.Add(chatLog, chatLogController);
            chatLogController.OnDestroyEvent += () => chatLogControllerCache.Remove(chatLog);
            return chatLogController;
        }
    }

    public ChatLogController GetChatLogController(ChatLog chatLog)
    {
        return chatLogControllerCache.ContainsKey(chatLog) ? chatLogControllerCache[chatLog] : null;
    }
}
