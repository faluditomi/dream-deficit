using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChatLogManager : Singleton<ChatLogManager>, IDayLoadable, IDaySavable
{
    private ChatClientController chatClientController;
    private GameObject chatLogPrefab;
    private Transform windowContainer;
    private Dictionary<ChatLog, ChatLogController> chatLogControllerCache = new Dictionary<ChatLog, ChatLogController>();
    private List<ChatLog> unlockedChatLogs = new List<ChatLog>();

    protected override void Awake()
    {
        base.Awake();
        chatLogPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatLog);
    }

    private Transform ResolveWindowContainer()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        foreach(GameObject root in activeScene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if(canvas != null) return canvas.transform.Find(Constants.GameObjectNames.WindowContainer);
        }

        Transform fallback = FindFirstObjectByType<Canvas>(FindObjectsInactive.Exclude).transform.Find(Constants.GameObjectNames.WindowContainer);
        return fallback != null ? fallback : null;
    }

    public void ClearChatLogControllers()
    {
        foreach(ChatLogController chatLogController in chatLogControllerCache.Values)
        {
            if(chatLogController != null) Destroy(chatLogController.gameObject);
        }

        chatLogControllerCache.Clear();
    }

    public void LoadFromDayData(DayData dayData)
    {
        ClearChatLogControllers();
        unlockedChatLogs.Clear();

        dayData.unlockedChatLogNames.ForEach(chatLogName =>
        {
            ChatLog chatLog = AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatLogName);
            if(chatLog != null) unlockedChatLogs.Add(chatLog);
        });
    }

    public void SaveToDayData(DayData dayData)
    {
        List<string> unlockedChatLogNames = new List<string>();
        unlockedChatLogs.ForEach(chatLog => unlockedChatLogNames.Add(chatLog.name));
        dayData.unlockedChatLogNames = unlockedChatLogNames;
    }

    public ChatLogController InstantiateChatLog(ChatLog chatLog, Transform initialiser, bool needsTopBar = true, Transform parent = null)
    {
        if(chatLog == null)
        {
            Debug.LogError("ChatLogManager: Cannot instantiate a chat log without a valid ChatLog.");
            return null;
        }

        if(chatLogControllerCache.TryGetValue(chatLog, out ChatLogController cached))
        {
            if(cached != null) return cached;

            // a destroyed controller is a cache miss, so drop the stale key and instantiate a fresh one
            chatLogControllerCache.Remove(chatLog);
        }

        Transform container = parent != null ? parent : ResolveWindowContainer();
        ChatLogController chatLogController = Instantiate(chatLogPrefab, container).GetComponent<ChatLogController>();
        chatLogController.Setup(chatLog, needsTopBar);
        if(needsTopBar) chatLogController.GetComponent<TopBarHandler>().Close();
        chatLogControllerCache.Add(chatLog, chatLogController);
        chatLogController.OnDestroyEvent += () =>
        {
            // Destroy is deferred to the end of the frame. A controller cleared for a new day must not
            // evict the controller that replaced it under the same ChatLog key during that frame.
            if(chatLogControllerCache.TryGetValue(chatLog, out ChatLogController cachedController) && cachedController == chatLogController)
            {
                chatLogControllerCache.Remove(chatLog);
            }
        };

        MessageNotificationController entryNotification = initialiser.GetComponentInChildren<MessageNotificationController>();
        if(entryNotification != null) entryNotification.Setup(new List<ChatLogController>() { chatLogController });

        return chatLogController;
    }

    public void UnlockChatLog(ChatLog chatLog)
    {
        if(!unlockedChatLogs.Contains(chatLog)) unlockedChatLogs.Add(chatLog);
    }

    public ChatLogController GetChatLogControllerByLogName(string chatLogName)
    {
        ChatLog chatLog = AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatLogName);
        if(chatLog == null) return null;
        return GetChatLogController(chatLog);
    }

    public ChatLogController GetChatLogController(ChatLog chatLog)
    {
        return chatLogControllerCache.TryGetValue(chatLog, out ChatLogController cached) && cached != null ? cached : null;
    }

    public bool IsChatLogInFocus(ChatLogController chatLogController)
    {
        return UIFocusManager.Instance.focusedWindow == chatLogController 
            || (UIFocusManager.Instance.focusedWindow == GetChatClientController() 
            && GetChatClientController().myChatLogController == chatLogController);
    }

    private ChatClientController GetChatClientController()
    {
        if(chatClientController == null) chatClientController = FindFirstObjectByType<ChatClientController>();
        return chatClientController;
    }
}
