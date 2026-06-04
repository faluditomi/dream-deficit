using System.Reflection;
using UnityEngine;

[System.Serializable]
public class MarkerData
{
    // Serializable fields (stored)
    public string markerTypeName;
    public string chatLogPath;
    public int chatBubbleIndex;
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
        ChatBubble chatBubble, 
        int startIndex, 
        int endIndex,
        int dayNumber,
        float accuracy
    )
    {
        markerTypeName = markerType?.name ?? string.Empty;
        chatLogPath = chatLog?.logName ?? string.Empty;
        chatBubbleIndex = (chatLog != null && chatBubble != null && chatLog.messages != null) ? chatLog.messages.IndexOf(chatBubble) : -1;
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
        return AddressableManager.Instance.RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + chatLogPath);
    }

    private ChatBubble ResolveChatBubble()
    {
        ChatLog chatLog = ResolveChatLog();
        if(chatLog == null || chatLog.messages == null || chatBubbleIndex < 0 || chatBubbleIndex >= chatLog.messages.Count) return null;
        return chatLog.messages[chatBubbleIndex];
    }
}
