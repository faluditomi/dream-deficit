using System.Collections.Generic;
using UnityEngine;

public class ChatClientController : BaseWindowController, ILoadable
{
    [HideInInspector] public ChatLogController myChatLogController;
    private GameObject chatUserEntryPrefab;
    private Transform content;
    private Transform chatWindowHolder;

    private void Awake()
    {
        myChatLogController = GetComponentInChildren<ChatLogController>();
        chatUserEntryPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatClientUserEntry);
        content = transform.Find(Constants.GameObjectNames.UserListWindow).Find(Constants.GameObjectNames.Viewport).Find(Constants.GameObjectNames.Content);
        chatWindowHolder = transform.Find(Constants.GameObjectNames.ChatWindowHolder);
        SetupBaseWindow(Constants.WindowAndFileNames.ChatClient);
        GetComponent<TopBarHandler>().Close();
    }

    public void LoadFromDayData(DayData dayData)
    {
        List<ChatUser> activeEntries = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetActiveChatClientUsers();

        foreach(Transform child in content) Destroy(child.gameObject);
        
        foreach(ChatUser chatUser in activeEntries)
        {
            ChatLog chatLog = AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatUser.name.ToString().ToLower());

            if(chatLog == null)
            {
                Debug.LogError($"ChatClientController: could not resolve ChatLog for chat user '{chatUser.name}'. Skipping entry.");
                continue;
            }

            ChatClientUserEntryController entryInstance = Instantiate(chatUserEntryPrefab, content).GetComponent<ChatClientUserEntryController>();
            string lastMessage = chatLog.messages.Count > 0 ? chatLog.messages[chatLog.messages.Count - 1].message : "";
            entryInstance.Setup(chatUser, chatLog, lastMessage, chatWindowHolder);
        }

        // NOTE: this may not be the cleanest way to do it, but we have to make the default, top chat user
        if(activeEntries.Find(chatUserScriptable => chatUserScriptable.chatUser.Equals(Constants.ChatUser.Phoebe)) != null)
        {
            BringUserToTopAndOpenLog(AddressableManager
                .Instance
                .RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + Constants.ChatUser.Phoebe.ToString().ToLower()));
        }
    }

    public void BringUserToTopAndOpenLog(ChatLog chatLog)
    {
        ChatLogController chatLogController = ChatLogManager.Instance.GetChatLogControllerByLogName(chatLog.name);
        if(chatLogController == null) return;
        myChatLogController = chatLogController;
        myChatLogController.transform.SetAsLastSibling();
        myChatLogController.Open();
    }

    // TODO: aggregated notification counter has to be handled
        // TODO: if we make it so that all scripts that inherit BaseWindowController extend its Setup method instead of using Awake,
        //       then we can call the BaseWindowController's Setup from the window opener button.
        //       this would allow us to pass initialiser to the Setup method, and in the case of the ChatClientController, it could be
        //       able to set up it's own MessageNotificationController (by passing a true isChatClient parameter to the notif controller)
}
