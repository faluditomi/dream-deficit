[System.Serializable]
public class ChatLogEntry
{
    public string chatLogPath;
    public bool isBonus;
}

[System.Serializable]
public class ResolvedChatLogEntry
{
    public ChatLog chatLog;
    public bool isBonus;
    public bool isUnlocked;
}
