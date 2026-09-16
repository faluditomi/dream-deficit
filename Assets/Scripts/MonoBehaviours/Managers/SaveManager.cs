using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static GameTemplate;

public class SaveManager : Singleton<SaveManager>
{
    // TODO: once we have a menu, we have to create a save slot picker/creator. for now, we just assign the slot brute force.
    public SaveSlot activeSlot;
    private List<IDaySavable> daySavables = new List<IDaySavable>();
    private List<IDayLoadable> dayLoadables = new List<IDayLoadable>();
    private List<IRunSavable> runSavables = new List<IRunSavable>();
    private List<IRunLoadable> runLoadables = new List<IRunLoadable>();
    private RunData currentRunSaveData;
    private DayData currentDayData;
    private Dictionary<int, DayData> daySaveData = new Dictionary<int, DayData>();
    private string savePath;

    protected override void Awake()
    {
        base.Awake();
        if(activeSlot == null) return;
        savePath = Path.Combine(Application.persistentDataPath, $"save_{activeSlot.slotName}.json");
        if(!LoadGame()) InitializeFromTemplate();
    }

    public void SaveDay(int dayNumber)
    {
        DayData dayData = daySaveData[dayNumber];
        foreach(var savable in daySavables) savable.SaveToDayData(dayData);
        foreach(var savable in runSavables) savable.SaveToRunData(currentRunSaveData);
        daySaveData[dayNumber] = dayData;
        UpdateSlotDayEntry(dayNumber, dayData);
        activeSlot.runData = currentRunSaveData;
        SaveGame();
    }

    private void UpdateSlotDayEntry(int dayNumber, DayData dayData)
    {
        if (activeSlot == null) return;
        int index = activeSlot.dayEntries.FindIndex(e => e.dayNumber == dayNumber);
        if(index >= 0) activeSlot.dayEntries[index] = new GameTemplate.DayDataEntry { dayNumber = dayNumber, dayData = dayData };
        else activeSlot.dayEntries.Add(new GameTemplate.DayDataEntry { dayNumber = dayNumber, dayData = dayData });
        #if UNITY_EDITOR
        EditorUtility.SetDirty(activeSlot);
        #endif
    }

    public void LoadDay(int dayNumber)
    {
        currentDayData = GetDayData(dayNumber);

        if(currentDayData == null)
        {
            Debug.LogWarning($"No save data found for day {dayNumber}.");
            return;
        }

        foreach(var loadable in dayLoadables) loadable.LoadFromDayData(currentDayData);
        foreach(var loadable in runLoadables) loadable.LoadFromRunData(currentRunSaveData);
    }

    private DayData GetDayData(int dayNumber)
    {
        // first check runtime data (loaded from JSON)
        if(daySaveData.ContainsKey(dayNumber)) return daySaveData[dayNumber];

        // then check slot's own day entries
        if(activeSlot != null)
        {
            var slotEntry = activeSlot.dayEntries.Find(e => e.dayNumber == dayNumber);
            if(slotEntry.dayData != null) return slotEntry.dayData;
        }

        // finally fall back to template data
        if(activeSlot != null && activeSlot.template != null)
        {
            DayData templateData = activeSlot.template.GetDayData(dayNumber);
            if(templateData != null) return templateData;
        }

        return null;
    }

    public DayData GetCurrentDayData()
    {
        return currentDayData;
    }

    public void SaveGame()
    {
        // sync slot's day entries to runtime data before saving
        foreach(var kvp in daySaveData) UpdateSlotDayEntry(kvp.Key, kvp.Value);

        List<DayDataEntry> containerList = daySaveData
            .Select(kvp => new DayDataEntry { dayNumber = kvp.Key, dayData = kvp.Value })
            .ToList();

        string json = JsonUtility.ToJson(new SaveFileData
        {
            days = containerList,
            runSaveData = currentRunSaveData
        }, true);

        File.WriteAllText(savePath, json);
    }

    public bool LoadGame()
    {
        if(!File.Exists(savePath)) return false;
        string json = File.ReadAllText(savePath);
        SaveFileData saveFile = JsonUtility.FromJson<SaveFileData>(json);
        daySaveData.Clear();
        foreach(var entry in saveFile.days) daySaveData[entry.dayNumber] = entry.dayData;
        currentRunSaveData = saveFile.runSaveData ?? new RunData();
        return true;
    }

    private void InitializeFromTemplate()
    {
        var template = activeSlot.template;
        if(template == null) return;
        // copy template day entries into slot's own day entries
        activeSlot.dayEntries.Clear();
        daySaveData.Clear();
        currentRunSaveData = new RunData();

        foreach(var entry in template.dayEntries)
        {
            // deep-copy so runtime mutations (e.g. unlock state) don't leak back into the
            // template asset, and so the slot/runtime don't share one DayData instance
            DayData cloned = JsonUtility.FromJson<DayData>(JsonUtility.ToJson(entry.dayData));
            activeSlot.dayEntries.Add(new GameTemplate.DayDataEntry { dayNumber = entry.dayNumber, dayData = cloned });
            daySaveData[entry.dayNumber] = cloned;
        }

        #if UNITY_EDITOR
        EditorUtility.SetDirty(activeSlot);
        #endif

        if(daySaveData.Count > 0) SaveGame();
    }

    public bool HasSaveForDay(int dayNumber)
    {
        return daySaveData.ContainsKey(dayNumber) ||
            (activeSlot != null && activeSlot.dayEntries.Any(e => e.dayNumber == dayNumber)) ||
            (activeSlot != null && activeSlot.template != null && activeSlot.template.HasDay(dayNumber));
    }

    #region SaveLoad Subscription

    public void AddDaySavable(IDaySavable iSavable)
    {
        daySavables.Add(iSavable);
    }

    public void AddDayLoadable(IDayLoadable iLoadable)
    {
        dayLoadables.Add(iLoadable);
        if(currentDayData != null) iLoadable.LoadFromDayData(currentDayData);
    }

    public void RemoveDaySavable(IDaySavable iSavable)
    {
        daySavables.Remove(iSavable);
    }

    public void RemoveDayLoadable(IDayLoadable iLoadable)
    {
        dayLoadables.Remove(iLoadable);
    }

    public void AddRunSavable(IRunSavable iSavable)
    {
        runSavables.Add(iSavable);
    }

    public void AddRunLoadable(IRunLoadable iLoadable)
    {
        runLoadables.Add(iLoadable);
        if(currentDayData != null) iLoadable.LoadFromRunData(currentRunSaveData);
    }

    public void RemoveRunSavable(IRunSavable iSavable)
    {
        runSavables.Remove(iSavable);
    }

    public void RemoveRunLoadable(IRunLoadable iLoadable)
    {
        runLoadables.Remove(iLoadable);
    }

    #endregion

    [System.Serializable]
    private class SaveFileData
    {
        public List<DayDataEntry> days;
        public RunData runSaveData;
    }
}
