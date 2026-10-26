using System.Collections.Generic;
using UnityEngine;

public class NotificationStateManager : Singleton<NotificationStateManager>, IRunSavable, IRunLoadable
{
    private ChatClientController chatClientController;
    private ChatClientStateSave currentChatClientStateSave;

    public void SaveToRunData(RunData runData)
    {
        if(chatClientController == null) chatClientController = FindFirstObjectByType<ChatClientController>();
        if(chatClientController == null) return;
        ChatClientStateSave chatClientStateSave = new ChatClientStateSave();

        for(int i = chatClientController.userListContent.childCount - 1; i >= 0; i--)
        {
            Transform chatUserTransform = chatClientController.userListContent.GetChild(i);
            ChatClientUserEntryController chatClientUserEntryController = chatUserTransform.GetComponent<ChatClientUserEntryController>();
            MessageNotificationController messageNotificationController = chatUserTransform.GetComponentInChildren<MessageNotificationController>();
            ChatUserState chatUserState = new ChatUserState();
            chatUserState.chatUserName = chatClientUserEntryController.myChatUser.name;
            chatUserState.isLastMessageUnread = chatClientUserEntryController.isUnopened;
            chatUserState.unreadMessageCount = messageNotificationController.unreadMessageCount;
            chatClientStateSave.reversedOrderChatUserStates.Add(chatUserState);
        }

        runData.chatClientStateSave = currentChatClientStateSave = chatClientStateSave;
    }

    public void LoadFromRunData(RunData runData)
    {
        // Deep copy the run data, so we don't change the save data directly by accident
        currentChatClientStateSave = JsonUtility.FromJson<ChatClientStateSave>(JsonUtility.ToJson(runData.chatClientStateSave));
    }

    public void ResetNotificationState()
    {
        if(chatClientController == null) chatClientController = FindFirstObjectByType<ChatClientController>();
        if(chatClientController == null || currentChatClientStateSave == null) return;
        chatClientController.ResetNotificationState(currentChatClientStateSave);
    }
}
