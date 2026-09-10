using System.Reflection;
using UnityEngine;

[System.Serializable]
public class MarkerData
{
    // Serializable fields (stored)
    public string markerTypeName;
    public string chatLogPath;
    public string nodeGuid;
    public int startIndex;
    public int endIndex;
    public int dayNumber;
    public float accuracy;

    // Resolved properties (computed on demand)
    public MarkerType ResolvedMarkerType => ResolveMarkerType();
    public ChatLog ResolvedChatLog => ResolveChatLog();
    public ChatBubble ResolvedChatBubble => ResolveChatBubble();

    public MarkerData
    (
        MarkerType markerType,
        ChatLog chatLog,
        int startIndex,
        int endIndex,
        int dayNumber,
        float accuracy,
        string nodeGuid
    )
    {
        markerTypeName = markerType?.name ?? string.Empty;
        chatLogPath = chatLog?.logName ?? string.Empty;
        this.nodeGuid = nodeGuid;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.dayNumber = dayNumber;
        this.accuracy = accuracy;
    }

    // Parameterless constructor for deserialization
    public MarkerData() { }

    private MarkerType ResolveMarkerType()
    {
        if(string.IsNullOrEmpty(markerTypeName)) return null;
        var markerTypeFields = typeof(Markers).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach(var field in markerTypeFields)
        {
            if(field.FieldType == typeof(MarkerType))
            {
                MarkerType markerType = (MarkerType)field.GetValue(null);

                if(markerType != null && markerType.name == markerTypeName)
                {
                    return markerType;
                }
            }
        }

        Debug.LogWarning($"MarkerType '{markerTypeName}' not found.");
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
        Debug.LogWarning($"MarkerData: node '{nodeGuid}' not found in any conversation graph of chat log '{chatLogPath}'.");
        return null;
    }
}
