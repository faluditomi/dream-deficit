using System.Collections.Generic;

[System.Serializable]
public class RunData
{
    public int currentDayNumber = 1;
    public List<string> chatSignals = new List<string>();
    public List<LogChatState> chatLogs = new List<LogChatState>();
}
