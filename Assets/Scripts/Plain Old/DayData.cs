using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[System.Serializable]
public class DayData
{
    public int dayNumber;
    public List<ChatLogEntry> activeAssignments = new List<ChatLogEntry>();
    // runtime-only unlock state — never seeded from the template
    public List<string> unlockedChatLogNames = new List<string>();
    public List<string> flagTypeNames = new List<string>();
    public List<string> activeChatClientUsers = new List<string>();
    public List<FlagData> flagData = new List<FlagData>();

    public List<ResolvedChatLogEntry> GetActiveChatLogEntries()
    {
        if(activeAssignments == null) activeAssignments = new List<ChatLogEntry>();

        return activeAssignments
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

    public List<ChatLog> GetActiveAssignments()
    {
        return GetActiveChatLogEntries()
            .Select(entry => entry.chatLog)
            .ToList();
    }

    public bool IsLogBonus(string logName)
    {
        if(activeAssignments == null) return false;
        ChatLogEntry entry = activeAssignments.Find(e => e != null && e.logName == logName);
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

    public List<ChatUser> GetActiveChatClientUsers()
    {
        if(activeChatClientUsers == null) activeChatClientUsers = new List<string>();
        List<ChatUser> chatUsers = new List<ChatUser>();

        foreach(string chatUserName in activeChatClientUsers)
        {
            if(string.IsNullOrEmpty(chatUserName)) continue;
            ChatUser chatUser = AddressableManager.Instance.RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + chatUserName.ToLower());
            if(chatUser != null) chatUsers.Add(chatUser);
        }

        return chatUsers;
    }

    public List<FlagType> GetFlagTypes()
    {
        var flagTypeFields = typeof(Flags).GetFields(
            BindingFlags.Public |
            BindingFlags.Static);

        List<FlagType> flagTypes = new List<FlagType>();

        foreach(var typeName in flagTypeNames)
        {
            foreach(var field in flagTypeFields)
            {
                if(field.FieldType == typeof(FlagType))
                {
                    FlagType flagType = (FlagType)field.GetValue(null);
                    
                    if(flagType != null && flagType.name == typeName)
                    {
                        flagTypes.Add(flagType);
                        break;
                    }
                }
            }
        }

        return flagTypes;
    }

    public List<FlagData> GetFlagData()
    {
        return flagData
            .Where(md => md != null && !string.IsNullOrEmpty(md.flagTypeName))
            .ToList();
    }
}
