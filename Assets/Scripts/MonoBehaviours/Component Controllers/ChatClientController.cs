using System.Collections.Generic;
using UnityEngine;

public class ChatClientController : BaseWindowController, IDayLoadable
{
    public ChatLogController myChatLogController { get; set; }
    public Transform userListContent { get; private set; }
    private Dictionary<ChatUser, ChatClientUserEntryController> userEntryControllers = new Dictionary<ChatUser, ChatClientUserEntryController>();
    private GameObject chatUserEntryPrefab;
    private Transform chatWindowHolder;
    private MessageNotificationController myInitialiserMessageNotificationController;

    private void Awake()
    {
        chatUserEntryPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatClientUserEntry);
        userListContent = transform.Find(Constants.GameObjectNames.UserListWindow).Find(Constants.GameObjectNames.Viewport).Find(Constants.GameObjectNames.Content);
        chatWindowHolder = transform.Find(Constants.GameObjectNames.ChatWindowHolder);
        SetupBaseWindow(Constants.WindowAndFileNames.ChatClient);
        GetComponent<TopBarHandler>().Close();
        OnGainedFocusEvent += (focusedWindow, unreadMessages) => { if(myChatLogController != null) myChatLogController.OnGainedFocus(); };
    }

    public void LoadFromDayData(DayData dayData)
    {
        List<ChatUser> activeEntries = dayData.GetActiveChatClientUsers();
        userEntryControllers.Clear();
        foreach(Transform child in userListContent) Destroy(child.gameObject);
        ChatClientUserEntryController supervisorEntryInstance = null;
        myChatLogController = null;
        List<ChatLogController> chatLogControllers = new List<ChatLogController>();

        foreach(ChatUser chatUser in activeEntries)
        {
            ChatLog chatLog = AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatUser.name.ToString().ToLower());
            
            if(chatLog == null)
            {
                Debug.LogError($"ChatClientController: could not resolve ChatLog for chat user '{chatUser.name}'. Skipping entry.");
                continue;
            }

            ChatClientUserEntryController entryInstance = Instantiate(chatUserEntryPrefab, userListContent).GetComponent<ChatClientUserEntryController>();
            userEntryControllers.Add(chatUser, entryInstance);
            if(chatUser.name == Constants.ChatUser.Phoebe.ToString().ToLower()) supervisorEntryInstance = entryInstance;
            string lastMessage = GetLastDeliveredMessage(chatLog);
            entryInstance.Setup(chatUser, chatLog, lastMessage, chatWindowHolder);
            ChatLogController entryChatLogController = entryInstance.myChatLogController;
            if(entryChatLogController != null && !chatLogControllers.Contains(entryChatLogController)) chatLogControllers.Add(entryChatLogController);
            if(myChatLogController == null) myChatLogController = ChatLogManager.Instance.GetChatLogController(chatLog);
        }

        if(myInitialiserMessageNotificationController != null) myInitialiserMessageNotificationController.Setup(chatLogControllers);
        if(myChatLogController != null) myChatLogController.Open();
    }

    public void ResetNotificationState(ChatClientStateSave chatClientStateSave)
    {
        int globalChatNotificationCounter = 0;

        foreach(ChatUserState chatUserState in chatClientStateSave.reversedOrderChatUserStates)
        {
            if(string.IsNullOrEmpty(chatUserState.chatUserName)) continue;
            ChatUser chatUser = AddressableManager.Instance.RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + chatUserState.chatUserName.ToLower());
            if(!userEntryControllers.ContainsKey(chatUser)) continue;
            ChatClientUserEntryController userEntryController = userEntryControllers[chatUser];
            userEntryController.transform.SetAsFirstSibling();
            if(chatUserState.isLastMessageUnread) userEntryController.MarkCurrentMessageAsUnopened();
            userEntryController.transform.GetComponentInChildren<MessageNotificationController>().SetNotificationCount(chatUserState.unreadMessageCount);
            globalChatNotificationCounter += chatUserState.unreadMessageCount;
        }

        if(myInitialiserMessageNotificationController != null) myInitialiserMessageNotificationController.SetNotificationCount(globalChatNotificationCounter);
    }

    public override void WindowSpecificSetup(Transform initialiser)
    {
        if(initialiser == null) return;
        myInitialiserMessageNotificationController = initialiser.GetComponentInChildren<MessageNotificationController>();
    }

    private string GetLastDeliveredMessage(ChatLog chatLog)
    {
        if(chatLog == null) return string.Empty;
        ConversationRunner runner = ConversationManager.Instance.GetRunnerForLog(chatLog);

        // most recent played history record, resolved through the log's graphs
        if(runner != null && runner.history != null && runner.history.Count > 0)
        {
            PlayedBubbleRecord lastRecord = runner.history[runner.history.Count - 1];
            ChatBubble bubble = chatLog.ResolvePlayedBubble(lastRecord);

            if(bubble != null && !string.IsNullOrEmpty(bubble.message))
            {
                return bubble.message;
            }
        }

        ChatBubble seedBubble = chatLog.GetLastSeedBubble();
        
        return seedBubble != null && !string.IsNullOrEmpty(seedBubble.message)
            ? seedBubble.message
            : string.Empty;
    }
}
