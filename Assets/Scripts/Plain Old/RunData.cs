using System.Collections.Generic;

[System.Serializable]
public class RunData
{
    public int currentDayNumber = 1;
    public List<string> chatSignals = new List<string>();
    public List<LogChatState> chatLogs = new List<LogChatState>();
    public Dictionary<ChatUser, DataDoubleProfile> dataDoubleProfiles = new Dictionary<ChatUser, DataDoubleProfile>();
    public DaySaveState daySaveState = DaySaveState.DayStart;
}

public enum DaySaveState
{
    DreamStart = 0,
    DayStart = 1,
    WorkEnd = 2
}
