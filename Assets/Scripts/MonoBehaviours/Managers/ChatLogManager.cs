using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogManager : Singleton<ChatLogManager>
{
    private GameObject chatLogPrefab;
    private Transform windowContainer;
    private ChatLogController supervisorChatLogController;
    private Dictionary<ChatLog, ChatLogController> chatLogControllerCache = new Dictionary<ChatLog, ChatLogController>();

    protected override void Awake()
    {
        base.Awake();
        chatLogPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatLog);
        windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
    }

    public ChatLogController InstantiateChatLog(ChatLog chatLog, Transform initialiser)
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
            
            initialiser.GetComponent<Button>().onClick.AddListener(() => {
                chatLogController.Open();
            });

            chatLogControllerCache.Add(chatLog, chatLogController);
            chatLogController.OnDestroyEvent += () => chatLogControllerCache.Remove(chatLog);

            if(initialiser.GetComponentInChildren<MessageNotificationController>() != null)
            {
                initialiser.GetComponentInChildren<MessageNotificationController>().Setup(chatLogController);
            }

            return chatLogController;
        }
    }

    public ChatLogController GetChatLogController(ChatLog chatLog)
    {
        return chatLogControllerCache.ContainsKey(chatLog) ? chatLogControllerCache[chatLog] : null;
    }

    public ChatLogController GetSupervisorChatLogController()
    {
        if(supervisorChatLogController == null)
        {
            ChatLog supervisorChatLog = AddressableManager.Instance
                .RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + Constants.ChatLogs.Supervisor);
            supervisorChatLogController = GetChatLogController(supervisorChatLog);
        }

        return supervisorChatLogController;
    }
}
