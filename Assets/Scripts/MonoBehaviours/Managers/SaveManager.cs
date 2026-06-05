using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    // TODO: once we have a menu, we have to create a save slot picker/creator. for now, we just assign the slot brute force.
    public SaveSlot activeSlot;
    private List<ISavable> savables = new List<ISavable>();
    private List<ILoadable> loadables = new List<ILoadable>();
    private Dictionary<int, DayData> runtimeSaveData = new Dictionary<int, DayData>();
    private List<GameTemplate.SequencedChatLogEntry> runtimeSequencedChatLogEntries = new List<GameTemplate.SequencedChatLogEntry>();
    private string savePath;

    protected override void Awake()
    {
        base.Awake();
        if(activeSlot == null) return;

        savePath = Path.Combine(Application.persistentDataPath, $"save_{activeSlot.slotName}.json");

        if(!LoadGame())
        {
            InitializeFromTemplate();
        }
    }

    private void RegisterSavables()
    {
        savables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISavable>().ToList();
    }

    private void RegisterLoadables()
    {
        loadables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ILoadable>().ToList();
    }

    public void SaveDay(int dayNumber)
    {
        DayData dayData = runtimeSaveData[dayNumber];

        foreach(var savable in savables)
        {
            savable.SaveToDayData(dayData);
        }

        runtimeSaveData[dayNumber] = dayData;
        UpdateSlotDayEntry(dayNumber, dayData);
        SaveGame();
    }

    private void UpdateSlotDayEntry(int dayNumber, DayData dayData)
    {
        if (activeSlot == null) return;
        int index = activeSlot.dayEntries.FindIndex(e => e.dayNumber == dayNumber);
        
        if(index >= 0)
        {
            activeSlot.dayEntries[index] = new GameTemplate.DayDataEntry { dayNumber = dayNumber, dayData = dayData };
        }
        else
        {
            activeSlot.dayEntries.Add(new GameTemplate.DayDataEntry { dayNumber = dayNumber, dayData = dayData });
        }

        #if UNITY_EDITOR
        EditorUtility.SetDirty(activeSlot);
        #endif
    }

    public void LoadDay(int dayNumber)
    {
        RegisterSavables();
        RegisterLoadables();
        DayData dayData = GetDayData(dayNumber);

        if(dayData == null)
        {
            Debug.LogWarning($"No save data found for day {dayNumber}.");
            return;
        }

        foreach(var loadable in loadables)
        {
            loadable.LoadFromDayData(dayData);
        }
    }

    public DayData GetDayData(int dayNumber)
    {
        // first check runtime data (loaded from JSON)
        if(runtimeSaveData.ContainsKey(dayNumber))
        {
            return runtimeSaveData[dayNumber];
        }

        // then check slot's own day entries
        if(activeSlot != null)
        {
            var slotEntry = activeSlot.dayEntries.Find(e => e.dayNumber == dayNumber);

            if(slotEntry.dayData != null)
            {
                return slotEntry.dayData;
            }
        }

        // finally fall back to template data
        if(activeSlot != null && activeSlot.template != null)
        {
            DayData templateData = activeSlot.template.GetDayData(dayNumber);

            if(templateData != null)
            {
                return templateData;
            }
        }

        return null;
    }

    public void SaveGame()
    {
        // sync slot's day entries to runtime data before saving
        foreach(var kvp in runtimeSaveData)
        {
            UpdateSlotDayEntry(kvp.Key, kvp.Value);
        }

        List<DayDataContainer> containerList = runtimeSaveData
            .Select(kvp => new DayDataContainer { dayNumber = kvp.Key, dayData = kvp.Value })
            .ToList();

        string json = JsonUtility.ToJson(new SaveFileData
        {
            days = containerList,
            sequencedChatLogEntries = runtimeSequencedChatLogEntries
        }, true);

        File.WriteAllText(savePath, json);
    }

    public bool LoadGame()
    {
        if(!File.Exists(savePath)) return false;
        string json = File.ReadAllText(savePath);
        SaveFileData saveFile = JsonUtility.FromJson<SaveFileData>(json);
        runtimeSaveData.Clear();

        foreach(var entry in saveFile.days)
        {
            runtimeSaveData[entry.dayNumber] = entry.dayData;
        }

        runtimeSequencedChatLogEntries = saveFile.sequencedChatLogEntries ?? new List<GameTemplate.SequencedChatLogEntry>();
        MergeSequencedChatLogEntries();
        return true;
    }

    private void InitializeFromTemplate()
    {
        var template = activeSlot.template;
        if(template == null) return;
        // copy template day entries into slot's own day entries
        activeSlot.dayEntries.Clear();
        runtimeSaveData.Clear();

        foreach(var entry in template.dayEntries)
        {
            activeSlot.dayEntries.Add(entry);
            runtimeSaveData[entry.dayNumber] = entry.dayData;
        }

        // copy template sequenced entries into slot's own entries
        activeSlot.sequencedChatLogEntries = new List<GameTemplate.SequencedChatLogEntry>(template.sequencedChatLogEntries);
        runtimeSequencedChatLogEntries = new List<GameTemplate.SequencedChatLogEntry>(template.sequencedChatLogEntries);
        MergeSequencedChatLogEntries();

        #if UNITY_EDITOR
        EditorUtility.SetDirty(activeSlot);
        #endif

        if(runtimeSaveData.Count > 0 || runtimeSequencedChatLogEntries.Count > 0)
        {
            SaveGame();
        }
    }

    public void MergeSequencedChatLogEntries()
    {
        if(runtimeSequencedChatLogEntries == null) runtimeSequencedChatLogEntries = new List<GameTemplate.SequencedChatLogEntry>();
        var template = activeSlot?.template;
        if(template == null) return;
        var runtimeKeys = new HashSet<ChatLog>(runtimeSequencedChatLogEntries.Select(e => e.chatLog));

        foreach(var defaultEntry in template.sequencedChatLogEntries)
        {
            if(!runtimeKeys.Contains(defaultEntry.chatLog))
            {
                runtimeSequencedChatLogEntries.Add(new GameTemplate.SequencedChatLogEntry
                {
                    chatLog = defaultEntry.chatLog,
                    sequences = new List<ChatBubbleSequence>(defaultEntry.sequences)
                });
            }
        }
    }

    public List<ChatBubble> GetSequencedChatBubblesForChatLog(ChatLog chatLog)
    {
        GameTemplate.SequencedChatLogEntry entry = runtimeSequencedChatLogEntries.FirstOrDefault(e => e.chatLog == chatLog);
        if(entry.chatLog == null) return new List<ChatBubble>();
        return entry.sequences.SelectMany(s => s.messages).ToList();
    }

    public List<MarkerData> GetSavedMarkersForChatLog(ChatLog chatLog)
    {
        DayData currentDayData = GetDayData(GameManager.Instance.CurrentDayNumber);
        if(currentDayData == null) return new List<MarkerData>();
        List<MarkerData> allMarkers = currentDayData.GetMarkerData();
        if(allMarkers == null) return new List<MarkerData>();
        return allMarkers.Where(m => m.ResolvedChatLog == chatLog).ToList();
    }

    public bool HasSaveForDay(int dayNumber)
    {
        return runtimeSaveData.ContainsKey(dayNumber) ||
               (activeSlot != null && activeSlot.dayEntries.Any(e => e.dayNumber == dayNumber)) ||
               (activeSlot != null && activeSlot.template != null && activeSlot.template.HasDay(dayNumber));
    }

    [System.Serializable]
    private class DayDataContainer
    {
        public int dayNumber;
        public DayData dayData;
    }

    [System.Serializable]
    private class SaveFileData
    {
        public List<DayDataContainer> days;
        public List<GameTemplate.SequencedChatLogEntry> sequencedChatLogEntries;
    }
}
