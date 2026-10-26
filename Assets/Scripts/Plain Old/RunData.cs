using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RunData
{
    public int currentDayNumber = 1;
    public List<string> chatSignals = new List<string>();
    public List<LogChatState> chatLogs = new List<LogChatState>();
    public Dictionary<string, DataDoubleProfile> dataDoubleProfiles = new Dictionary<string, DataDoubleProfile>();
    public DaySaveState daySaveState = DaySaveState.DayStart;
    public ChatClientStateSave chatClientStateSave;
    public Dictionary<ChatUser, DataDoubleProfile> ResolvedDataDoubleProfiles => GetDataDoubleProfiles();

    private Dictionary<ChatUser, DataDoubleProfile> GetDataDoubleProfiles()
    {
        if(dataDoubleProfiles == null || dataDoubleProfiles.Count <= 0) return null;
        Dictionary<ChatUser, DataDoubleProfile> resolvedDataDoulbeProfiles = new Dictionary<ChatUser, DataDoubleProfile>();

        foreach(KeyValuePair<string, DataDoubleProfile> dataDoubleProfile in dataDoubleProfiles)
        {
            if(string.IsNullOrEmpty(dataDoubleProfile.Key)) continue;
            ChatUser chatUser = AddressableManager.Instance.RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + dataDoubleProfile.Key.ToLower());
            resolvedDataDoulbeProfiles.Add(chatUser, dataDoubleProfile.Value);
        }

        return resolvedDataDoulbeProfiles;
    }
}

public enum DaySaveState
{
    DreamStart = 0,
    DayStart = 1,
    WorkEnd = 2
}

[System.Serializable]
public class ChatClientStateSave
{
    public List<ChatUserState> reversedOrderChatUserStates = new List<ChatUserState>();
}

[System.Serializable]
public class ChatUserState
{
    public string chatUserName;
    public bool isLastMessageUnread;
    public int unreadMessageCount;
    public ChatUser ResolvedChatUser => GetChatUser();

    private ChatUser GetChatUser()
    {
        if(string.IsNullOrEmpty(chatUserName)) return null;
        ChatUser chatUser = AddressableManager.Instance.RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + chatUserName.ToLower());
        return chatUser;
    }
}
