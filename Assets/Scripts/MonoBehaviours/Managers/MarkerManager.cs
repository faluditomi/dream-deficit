using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MarkerManager : MonoBehaviour, ISavable
{
    private static MarkerManager _instance;
    private List<MarkerData> placedMarkers = new List<MarkerData>();
    private List<MarkerType> activeMarkerTypeCache = new List<MarkerType>();
    private InputAction markerHoldAction;
    public MarkerType activeMarkerType;

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

        markerHoldAction = new InputAction(type: InputActionType.Button);

        markerHoldAction.started += ctx =>
        {
            if(activeMarkerType != null) return;

            if(ctx.control is KeyControl keyControl)
            {
                activeMarkerType = activeMarkerTypeCache.Find(mt => mt.keybind == keyControl.keyCode);
            }
        };

        markerHoldAction.canceled += ctx =>
        {
            activeMarkerType = null;
        };

        markerHoldAction.Enable();

        // TODO: this will have to be done from the GameManager and there needs to be profiles created for game portions
        SetActiveMarkerTypes(new List<MarkerType> { Markers.CopyPasteSpam, Markers.SynchronisedActivitySpike });
    }

    public void AddMarker(MarkerData markerData)
    {
        placedMarkers.Add(markerData);
    }

    public void RemoveMarker(MarkerData markerData)
    {
        placedMarkers.Remove(markerData);
    }

    public void SetActiveMarkerTypes(List<MarkerType> markerTypes)
    {
        activeMarkerTypeCache = markerTypes;
        markerHoldAction.Disable();

        foreach(MarkerType markerType in activeMarkerTypeCache)
        {
            markerHoldAction.AddBinding("<Keyboard>/" + markerType.keybind.ToString());
        }

        markerHoldAction.Enable();
    }

    public List<MarkerData> GetMarkersForChatBubble(ChatBubble chatBubble)
    {
        return placedMarkers.Where(m => m.chatBubble == chatBubble).ToList();
    }

    public object Save()
    {
        return placedMarkers;
    }

    public void Load(object data)
    {
        placedMarkers = (List<MarkerData>)data;
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
