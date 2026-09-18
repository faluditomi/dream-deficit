using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static GameTemplate;

public class SaveManager : Singleton<SaveManager>
{
    // TODO: once we have a menu, we have to create a save slot picker/creator. for now, we just assign the slot brute force.
    public SaveSlot activeSlot;
    // Registration state is static so subscribing never goes through Singleton.Instance. Instance auto-creates a SaveManager when
    // none exists, and returns null while the application is quitting, which made OnDisable throw a NullReferenceException on exit.
    private static readonly List<IDaySavable> daySavables = new List<IDaySavable>();
    private static readonly List<IDayLoadable> dayLoadables = new List<IDayLoadable>();
    private static readonly List<IRunSavable> runSavables = new List<IRunSavable>();
    private static readonly List<IRunLoadable> runLoadables = new List<IRunLoadable>();
    private static RunData currentRunData;
    private static DayData currentDayData;
    private readonly Dictionary<int, DayData> daySaveData = new Dictionary<int, DayData>();
    private string savePath;

    // clears registration state when entering play mode with "Enter Play Mode Options" domain reload disabled,
    // otherwise stale references survive between play sessions.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        daySavables.Clear();
        dayLoadables.Clear();
        runSavables.Clear();
        runLoadables.Clear();
        currentRunData = null;
        currentDayData = null;
    }

    protected override void Awake()
    {
        base.Awake();
        if(activeSlot == null) return;
        savePath = Path.Combine(Application.persistentDataPath, $"{activeSlot.name}.json");
        // TODO: this will have to be called when we select a save slot in the menu
        if(!LoadGame()) InitializeFromTemplate();
    }

    public void SaveDay(int dayNumber)
    {
        DayData dayData = daySaveData[dayNumber];
        // snapshot: savables can be registered/unregistered from inside these callbacks
        // (e.g. a lazily auto-created singleton registering in OnEnable)
        foreach(var savable in daySavables.ToArray()) savable.SaveToDayData(dayData);
        foreach(var savable in runSavables.ToArray()) savable.SaveToRunData(currentRunData);
        SaveGame();
    }

    public void LoadDay(int dayNumber)
    {
        currentDayData = GetDayData(dayNumber);

        if(currentDayData == null)
        {
            Debug.LogWarning($"No save data found for day {dayNumber}.");
            return;
        }

        // snapshot: LoadFromDayData can instantiate windows, and a lazily auto-created singleton
        // registers itself in OnEnable, which would otherwise mutate the list mid-enumeration
        foreach(var loadable in dayLoadables.ToArray()) loadable.LoadFromDayData(currentDayData);
    }

    private DayData GetDayData(int dayNumber)
    {
        // first check runtime data (loaded from JSON)
        if(daySaveData.ContainsKey(dayNumber)) return daySaveData[dayNumber];

        // then fall back to template data
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
        List<DayDataEntry> containerList = daySaveData
            .Select(kvp => new DayDataEntry { dayNumber = kvp.Key, dayData = kvp.Value })
            .ToList();

        string json = JsonUtility.ToJson(new SaveFileData
        {
            days = containerList,
            runSaveData = currentRunData
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
        currentRunData = saveFile.runSaveData ?? new RunData();
        foreach(var loadable in runLoadables.ToArray()) loadable.LoadFromRunData(currentRunData);
        return true;
    }

    private void InitializeFromTemplate()
    {
        var template = activeSlot.template;

        if(template == null) 
        {
            Debug.LogError("No save file present. Tried initialising from scratch, but received no Template.");
            return;
        }

        daySaveData.Clear();
        currentRunData = new RunData();
        foreach(var loadable in runLoadables.ToArray()) loadable.LoadFromRunData(currentRunData);

        foreach(var entry in template.dayEntries)
        {
            // deep-copy so runtime mutations (e.g. unlock state) don't leak back into the template asset
            DayData cloned = JsonUtility.FromJson<DayData>(JsonUtility.ToJson(entry.dayData));
            daySaveData[entry.dayNumber] = cloned;
        }

        if(daySaveData.Count > 0) SaveGame();
    }

    public bool HasSaveForDay(int dayNumber)
    {
        return daySaveData.ContainsKey(dayNumber) ||
            (activeSlot != null && activeSlot.template != null && activeSlot.template.HasDay(dayNumber));
    }

    #region SaveLoad Subscription

    public static void AddDaySavable(IDaySavable iSavable)
    {
        if(iSavable == null || daySavables.Contains(iSavable)) return;
        daySavables.Add(iSavable);
    }

    public static void AddDayLoadable(IDayLoadable iLoadable)
    {
        if(iLoadable == null || dayLoadables.Contains(iLoadable)) return;
        dayLoadables.Add(iLoadable);
        if(currentDayData != null) iLoadable.LoadFromDayData(currentDayData);
    }

    public static void RemoveDaySavable(IDaySavable iSavable)
    {
        if(iSavable != null) daySavables.Remove(iSavable);
    }

    public static void RemoveDayLoadable(IDayLoadable iLoadable)
    {
        if(iLoadable != null) dayLoadables.Remove(iLoadable);
    }

    public static void AddRunSavable(IRunSavable iSavable)
    {
        if(iSavable == null || runSavables.Contains(iSavable)) return;
        runSavables.Add(iSavable);
    }

    public static void AddRunLoadable(IRunLoadable iLoadable)
    {
        if(iLoadable == null || runLoadables.Contains(iLoadable)) return;
        runLoadables.Add(iLoadable);
        if(currentRunData != null) iLoadable.LoadFromRunData(currentRunData);
    }

    public static void RemoveRunSavable(IRunSavable iSavable)
    {
        if(iSavable != null) runSavables.Remove(iSavable);
    }

    public static void RemoveRunLoadable(IRunLoadable iLoadable)
    {
        if(iLoadable != null) runLoadables.Remove(iLoadable);
    }

    #endregion

    [System.Serializable]
    private class SaveFileData
    {
        public List<DayDataEntry> days;
        public RunData runSaveData;
    }
}
