[System.Serializable]
public class ChatLogEntry
{
    public string logName;
    public bool isBonus;
}

[System.Serializable]
public class ResolvedChatLogEntry
{
    public ChatLog chatLog;
    public bool isBonus;
    public bool isUnlocked;
}
