using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MarkerManager : MonoBehaviour, ISavable
{
    private static MarkerManager _instance;
    private GameObject markerFlagPrefab;
    private Transform uiCanvas;
    private List<MarkerData> placedMarkers = new List<MarkerData>();
    private Dictionary<MarkerType, MarkerFlagController> activeMarkerFlags = new Dictionary<MarkerType, MarkerFlagController>();
    private InputAction markerHoldAction;
    public List<MarkerType> activeMarkerTypeCache = new List<MarkerType>();
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

    private async void Awake()
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
        markerFlagPrefab = await AddressableManager.Instance().RetrieveAddressable<GameObject>(Constants.AddressablePaths.MarkerFlagPrefab);
        uiCanvas = FindFirstObjectByType<Canvas>().transform;

        // TODO: this will have to be done from the GameManager and there needs to be profiles created for game portions
        SetActiveMarkerTypes(new List<MarkerType> 
        { 
            Markers.CopyPasteSpam, 
            Markers.SynchronisedActivitySpike, 
            Markers.PersuasionPattern, 
            Markers.NarrativeShaping 
            });
    }

    public void OnKeyDown(Key key)
    {
        if(activeMarkerType != null)
        {
            activeMarkerFlags[activeMarkerType].SetRaised(false);
            activeMarkerType = null;
        }

        activeMarkerType = activeMarkerTypeCache.Find(mt => mt.keycode == key);

        if(activeMarkerType != null) activeMarkerFlags[activeMarkerType].SetRaised(true);
    }
    public void OnKeyUp(Key key)
    {
        if(activeMarkerType != null && activeMarkerType.keycode == key)
        {
            activeMarkerFlags[activeMarkerType].SetRaised(false);
            activeMarkerType = null;
        }
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

        activeMarkerFlags.Values.ToList().ForEach(mf => Destroy(mf.gameObject));
        activeMarkerFlags.Clear();

        if(markerHoldAction != null) 
        {
            markerHoldAction.Disable();
            markerHoldAction.Dispose();
        }

        markerHoldAction = new InputAction(type: InputActionType.PassThrough);
        markerHoldAction.performed += ctx =>
        {
            if(ctx.control is not KeyControl keyControl) return;

            float value = ctx.ReadValue<float>();

            if(value > 0)
            {
                OnKeyDown(keyControl.keyCode);
            }
            else
            {
                OnKeyUp(keyControl.keyCode);
            }
        };

        float markerFlagOffset = Screen.width / (activeMarkerTypeCache.Count + 1);

        foreach(MarkerType markerType in activeMarkerTypeCache)
        {
            markerHoldAction.AddBinding("<Keyboard>/" + markerType.keycode.ToString());
            MarkerFlagController newMarkerFlag = Instantiate(markerFlagPrefab, uiCanvas).GetComponent<MarkerFlagController>();
            activeMarkerFlags[markerType] = newMarkerFlag;
            float xPos = markerFlagOffset * activeMarkerFlags.Count;
            newMarkerFlag.Setup(markerType, xPos);
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
