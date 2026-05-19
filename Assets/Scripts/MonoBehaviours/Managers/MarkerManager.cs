using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MarkerManager : Singleton<MarkerManager>, ISavable
{
    private GameObject markerFlagPrefab;
    private Transform uiCanvas;
    private List<MarkerData> placedMarkers = new List<MarkerData>();
    private Dictionary<MarkerType, MarkerFlagController> activeMarkerFlags = new Dictionary<MarkerType, MarkerFlagController>();
    private InputAction markerHoldAction;
    public List<MarkerType> activeMarkerTypeCache = new List<MarkerType>();
    public MarkerType activeMarkerType;

    protected override async void Awake()
    {
        base.Awake();
        SaveManager.Instance.LoadGame();
        markerFlagPrefab = await AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePaths.MarkerFlagPrefab);
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
        if(!GameManager.Instance.isDayPassing) return;

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

    public void AddMarker(ChatLog chatLog, ChatBubble chatBubble, int start, int end)
    {
        if(!GameManager.Instance.isDayPassing) return;
        float accuracy = 0f;

        chatBubble.markables.ForEach(markable =>
        {
            // TODO: safeguard against multiple markers per markable
                // -> maybe this should instead be handled during statistical analysis
            if(markable.startIndex < end && markable.endIndex > start && markable.markerType.name.Equals(activeMarkerType.name))
            {
                float markableLength = markable.endIndex - markable.startIndex + 1;
                float lengthOfOverlap = Mathf.Min(markable.endIndex, end) - Mathf.Max(markable.startIndex, start) + 1;
                float lengthOfExcess = Mathf.Max((markable.startIndex - start), 0f) + Mathf.Max((end - markable.endIndex), 0f);
                // Accuracy is a percentage based on the proportion of the markable that is correctly covered by the marker,
                // penalized by any excess marking outside the markable. The penalty for excess marking is halved.
                float totalAccuracy = ((lengthOfOverlap / markableLength) - (lengthOfExcess / markableLength / 2f)) * 100f;
                accuracy = Mathf.Max(accuracy, totalAccuracy);
                Debug.Log(accuracy);
            }
        });

        MarkerData markerData = new MarkerData(
            activeMarkerType,
            chatLog,
            chatBubble,
            start,
            end,
            0, // TODO: Set the current day number
            accuracy
        );

        placedMarkers.Add(markerData);
    }

    public void RemoveMarker(MarkerData markerData)
    {
        if(!GameManager.Instance.isDayPassing) return;
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
