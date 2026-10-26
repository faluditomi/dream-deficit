using System.Collections.Generic;

[System.Serializable]
public class RunData
{
    public int currentDayNumber = 1;
    public List<string> chatSignals = new List<string>();
    public List<LogChatState> chatLogs = new List<LogChatState>();
    public List<DataDoubleProfileEntry> dataDoubleProfiles = new List<DataDoubleProfileEntry>();
    public DaySaveState daySaveState = DaySaveState.DayStart;
    public ChatClientStateSave chatClientStateSave;
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
}

// Serialization DTOs for the runtime DataDoubleProfile/DataDouble models. Those models hold dictionaries,
// ScriptableObject keys and no parameterless constructors, so they cannot be serialized by Unity/JsonUtility.
// DoublesManager converts between these DTOs and the runtime models.

[System.Serializable]
public class DataDoubleProfileEntry
{
    // asset name, resolved through Constants.AddressablePrefixes.ChatUser on load
    public string chatUserName;
    public DataDoubleSave global = new DataDoubleSave();
    public List<DataDoubleDaySave> daily = new List<DataDoubleDaySave>();
}

[System.Serializable]
public class DataDoubleDaySave
{
    public int day;
    public DataDoubleSave dataDouble = new DataDoubleSave();
}

[System.Serializable]
public class DataDoubleSave
{
    public bool isModeratorProfile;
    public bool isClientProfile;
    public List<MarkerEntry> markers = new List<MarkerEntry>();
}

[System.Serializable]
public class MarkerEntry
{
    // asset name, resolved through Constants.AddressablePrefixes.DoubleMarker on load
    public string markerName;
    // kept for inspection and as a fallback if markers are ever resolved without the asset
    public DoubleMarkerType markerType;
    public int baseline;
    public int score;
}
