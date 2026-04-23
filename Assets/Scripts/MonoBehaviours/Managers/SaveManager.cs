using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// TODO: once we have things to save, make a gamemanager that loads stuf at the start and saves stuff at the end
public class SaveManager : MonoBehaviour
{
    private ISavable[] saveables;
    private string savePath;

    private static SaveManager _instance;

    public static SaveManager Instance()
    {
        if(_instance == null)
        {
            var obj = new GameObject("SaveManager");
            _instance = obj.AddComponent<SaveManager>();
            DontDestroyOnLoad(obj);
        }

        return _instance;
    }

    private void Awake()
    {
        saveables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISavable>().ToArray();
        savePath = Path.Combine(Application.persistentDataPath, "receipts.json");
    }

    public void SaveGame()
    {
        List<string> jsonList = new List<string>();

        foreach(var saveable in saveables)
        {
            string json = JsonUtility.ToJson(new ObjectWrapper { json = JsonUtility.ToJson(saveable.Save()) });
            jsonList.Add(json);
        }

        string finalSaveJson = JsonUtility.ToJson(new SaveWrapper { objects = jsonList }, true);
        File.WriteAllText(savePath, finalSaveJson);
    }

    public void LoadGame()
    {
        if(!File.Exists(savePath))
        {
            Debug.LogWarning("No save file fould while trying to load.");
            return;
        }

        string finalSaveJson = File.ReadAllText(savePath);
        SaveWrapper saveWrapper = JsonUtility.FromJson<SaveWrapper>(finalSaveJson);

        // TODO: better log message -> is this the best way to check integrity? 
        if(saveWrapper.objects.Count != saveables.Length)
        {
            Debug.LogWarning("Mismatch between the number of saved objects and scene objects");
        }

        // TODO: check whether this retreives the save objects in the same order as the saveables are retreived
        for(int i = 0; i < saveables.Length; i++)
        {
            string objectJson = saveWrapper.objects[i];
            ObjectWrapper objectWrapper = JsonUtility.FromJson<ObjectWrapper>(objectJson);
            saveables[i].Load(JsonUtility.FromJson(objectWrapper.json, saveables[i].Save().GetType()));
        }
    }

    [System.Serializable]
    private class ObjectWrapper
    {
        public string json;
    }

    [System.Serializable]
    private class SaveWrapper
    {
        public List<string> objects;
    }
}
