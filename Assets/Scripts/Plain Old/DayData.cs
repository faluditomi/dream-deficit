using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[System.Serializable]
public class DayData
{
    public int dayNumber;
    public List<ChatLogEntry> activeChatLogs = new List<ChatLogEntry>();
    // runtime-only unlock state — never seeded from the template
    public List<string> unlockedChatLogNames = new List<string>();
    public List<string> supervisorBubbleSequenceNames = new List<string>();
    public List<string> markerTypeNames = new List<string>();
    public List<MarkerData> markerData = new List<MarkerData>();

    public List<ResolvedChatLogEntry> GetActiveChatLogEntries()
    {
        if(activeChatLogs == null) activeChatLogs = new List<ChatLogEntry>();

        return activeChatLogs
            .Where(entry => entry != null && !string.IsNullOrEmpty(entry.logName))
            .Select(entry => new ResolvedChatLogEntry
            {
                chatLog = AddressableManager.Instance.RetrieveAddressable<ChatLog>(
                    Constants.AddressablePrefixes.ChatLog + entry.logName),
                isBonus = entry.isBonus,
                isUnlocked = IsLogUnlocked(entry.logName)
            })
            .Where(entry => entry.chatLog != null)
            .ToList();
    }

    public List<ChatLog> GetActiveChatLogs()
    {
        return GetActiveChatLogEntries()
            .Select(entry => entry.chatLog)
            .ToList();
    }

    public bool IsLogBonus(string logName)
    {
        if(activeChatLogs == null) return false;
        ChatLogEntry entry = activeChatLogs.Find(e => e != null && e.logName == logName);
        return entry != null && entry.isBonus;
    }

    public bool IsLogUnlocked(string logName)
    {
        return unlockedChatLogNames != null && unlockedChatLogNames.Contains(logName);
    }

    public bool IsLogLocked(string logName)
    {
        return IsLogBonus(logName) && !IsLogUnlocked(logName);
    }

    public void UnlockLog(string logName)
    {
        if(unlockedChatLogNames == null) unlockedChatLogNames = new List<string>();
        if(!unlockedChatLogNames.Contains(logName)) unlockedChatLogNames.Add(logName);
    }

    public List<ChatBubbleSequence> GetSupervisorSequences()
    {
        return supervisorBubbleSequenceNames
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(
                Constants.AddressablePrefixes.ChatBubbleSequence + path))
            .Where(seq => seq != null)
            .ToList();
    }

    public List<MarkerType> GetMarkerTypes()
    {
        var markerTypeFields = typeof(Markers).GetFields(
            BindingFlags.Public |
            BindingFlags.Static);

        List<MarkerType> markerTypes = new List<MarkerType>();

        foreach (var typeName in markerTypeNames)
        {
            foreach (var field in markerTypeFields)
            {
                if (field.FieldType == typeof(MarkerType))
                {
                    MarkerType markerType = (MarkerType)field.GetValue(null);
                    if (markerType != null && markerType.name == typeName)
                    {
                        markerTypes.Add(markerType);
                        break;
                    }
                }
            }
        }

        return markerTypes;
    }

    public List<MarkerData> GetMarkerData()
    {
        return markerData
            .Where(md => md != null && !string.IsNullOrEmpty(md.markerTypeName))
            .ToList();
    }
}
