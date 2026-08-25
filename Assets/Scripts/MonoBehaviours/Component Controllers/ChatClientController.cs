using System.Collections.Generic;
using UnityEngine;

public class ChatClientController : BaseWindowController, ILoadable
{
    private ChatLogController myChatLogController;
    private GameObject chatUserEntryPrefab;
    private Transform content;
    private Transform chatWindowHolder;

    private void Awake()
    {
        myChatLogController = GetComponentInChildren<ChatLogController>();
        chatUserEntryPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatClientUserEntry);
        content = transform.Find(Constants.GameObjectNames.UserListWindow).Find(Constants.GameObjectNames.Viewport).Find(Constants.GameObjectNames.Content);
        chatWindowHolder = transform.Find(Constants.GameObjectNames.ChatWindowHolder);
        SetupTopBar(Constants.WindowAndFileNames.ChatClient);
        GetComponent<TopBarHandler>().Close();
    }

    public void LoadFromDayData(DayData dayData)
    {
        List<ChatUser> activeEntries = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetActiveChatClientUsers();

        foreach(Transform child in content)
        {
            Destroy(child.gameObject);
        }
        
        foreach(ChatUser chatUser in activeEntries)
        {
            ChatClientUserEntryController entryInstance = Instantiate(chatUserEntryPrefab, content).GetComponent<ChatClientUserEntryController>();
            ChatLog chatLog = AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatUser.ToString().ToLower());
            string lastMessage = chatLog.messages.Count > 0 ? chatLog.messages[chatLog.messages.Count - 1].message : "";
            entryInstance.Setup(chatUser, chatLog, lastMessage, chatWindowHolder);
        }

        // TODO: chat user list needs to be handled with user entry prefabs
        // TODO: we initialise the entries first, and the entries initialise their own chat log windows
            // TODO: we just have to make sure there is a non-top bar option for initialisation
            //       and that the window appears in the right place as a child of the ChatClient
            
        // TODO: we gotta make the supervisor be the last user we instantiate or 
        //       at least make sure the supervisor is on top of the list and they're log is the one that's open by default (gotta change GDD)
    }

    // TODO: upon user selection, the chat log controller should switch to the selected user's chat log
    // TODO: aggregated notification counter has to be handled
    // TODO: get rid of the supervisor button
}
