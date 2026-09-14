using System.Reflection;
using UnityEngine;

[System.Serializable]
public class FlagData
{
    // Serializable fields (stored)
    public string flagTypeName;
    public string chatLogPath;
    public string nodeGuid;
    public int startIndex;
    public int endIndex;
    public int dayNumber;
    public float accuracy;

    // Resolved properties (computed on demand)
    public FlagType ResolvedFlagType => ResolveFlagType();
    public ChatLog ResolvedChatLog => ResolveChatLog();
    public ChatBubble ResolvedChatBubble => ResolveChatBubble();

    public FlagData
    (
        FlagType flagType,
        ChatLog chatLog,
        int startIndex,
        int endIndex,
        int dayNumber,
        float accuracy,
        string nodeGuid
    )
    {
        flagTypeName = flagType?.name ?? string.Empty;
        chatLogPath = chatLog?.logName ?? string.Empty;
        this.nodeGuid = nodeGuid;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.dayNumber = dayNumber;
        this.accuracy = accuracy;
    }

    // Parameterless constructor for deserialization
    public FlagData() { }

    private FlagType ResolveFlagType()
    {
        if(string.IsNullOrEmpty(flagTypeName)) return null;
        var flagTypeFields = typeof(Flags).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach(var field in flagTypeFields)
        {
            if(field.FieldType == typeof(FlagType))
            {
                FlagType flagType = (FlagType)field.GetValue(null);

                if(flagType != null && flagType.name == flagTypeName)
                {
                    return flagType;
                }
            }
        }

        Debug.LogWarning($"FlagType '{flagTypeName}' not found.");
        return null;
    }

    private ChatLog ResolveChatLog()
    {
        if(string.IsNullOrEmpty(chatLogPath)) return null;
        return AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePrefixes.ChatLog + chatLogPath);
    }

    private ChatBubble ResolveChatBubble()
    {
        if(string.IsNullOrEmpty(nodeGuid)) return null;
        ChatLog chatLog = ResolveChatLog();
        if(chatLog == null) return null;
        ConversationNodeData node = chatLog.GetNode(nodeGuid);
        if(node != null) return node.bubble;
        Debug.LogWarning($"FlagData: node '{nodeGuid}' not found in any conversation graph of chat log '{chatLogPath}'.");
        return null;
    }
}
