using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarkerManager : MonoBehaviour, ISavable
{
    private static MarkerManager _instance;
    private List<MarkerData> markers = new List<MarkerData>();

    public static MarkerManager Instance()
    {
        if(_instance == null)
        {
            var obj = new GameObject("MarkerManager");
            _instance = obj.AddComponent<MarkerManager>();
            DontDestroyOnLoad(obj);
        }

        return _instance;
    }

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if(_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SaveManager.Instance().LoadGame();
    }

    public void AddMarker(MarkerData markerData)
    {
        markers.Add(markerData);
    }

    public void RemoveMarker(MarkerData markerData)
    {
        markers.Remove(markerData);
    }

    public List<MarkerData> GetMarkersForChatBubble(ChatBubble chatBubble)
    {
        return markers.Where(m => m.chatBubble == chatBubble).ToList();
    }

    public object Save()
    {
        return markers;
    }

    public void Load(object data)
    {
        markers = (List<MarkerData>)data;
    }
}

[System.Serializable]
public class MarkerData
{
    public MarkerType markerType;
    public ChatLog chatLog;
    public ChatBubble chatBubble;
    public int startIndex;
    public int endIndex;
    public bool isValid;

    public MarkerData
    (
        MarkerType markerType, 
        ChatLog chatLog, 
        ChatBubble chatBubble, 
        int startIndex, 
        int endIndex
    )
    {
        this.markerType = markerType;
        this.chatLog = chatLog;
        this.chatBubble = chatBubble;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        // TODO: this has to be calculated once we are tracking the possible markers for each markable bubble
        isValid = true;
    }
}
