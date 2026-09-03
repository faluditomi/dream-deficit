using System.Collections.Generic;
using UnityEngine;

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

    public ChatLogController InstantiateChatLog(ChatLog chatLog, Transform initialiser, bool needsTopBar = true, Transform parent = null)
    {
        if(chatLog == null)
        {
            Debug.LogError("ChatLogManager: Cannot instantiate a chat log without a valid ChatLog.");
            return null;
        }

        if(chatLogControllerCache.ContainsKey(chatLog))
        {
            return chatLogControllerCache[chatLog];    
        }
        else
        {
            Transform container = parent != null ? parent : windowContainer;
            ChatLogController chatLogController = Instantiate(chatLogPrefab, container).GetComponent<ChatLogController>();
            chatLogController.Setup(chatLog, needsTopBar);
            if(needsTopBar) chatLogController.GetComponent<TopBarHandler>().Close();
            chatLogControllerCache.Add(chatLog, chatLogController);
            chatLogController.OnDestroyEvent += () => chatLogControllerCache.Remove(chatLog);

            if(initialiser.GetComponentInChildren<MessageNotificationController>() != null)
            {
                initialiser.GetComponentInChildren<MessageNotificationController>().Setup(chatLogController);
            }

            return chatLogController;
        }
    }

    public ChatLogController GetChatLogControllerByLogName(string chatLogName)
    {
        ChatLog chatLog = AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatLogName);
        if(chatLog == null) return null;
        return GetChatLogController(chatLog);
    }

    public ChatLogController GetChatLogController(ChatLog chatLog)
    {
        return chatLogControllerCache.ContainsKey(chatLog) ? chatLogControllerCache[chatLog] : null;
    }
}
