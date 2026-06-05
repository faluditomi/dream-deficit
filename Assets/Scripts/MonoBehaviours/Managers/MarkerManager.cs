using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MarkerManager : Singleton<MarkerManager>, ILoadable
{
    private GameObject markerFlagPrefab;
    private Transform uiCanvas;
    private List<MarkerData> placedMarkers = new List<MarkerData>();
    private Dictionary<MarkerType, MarkerFlagController> activeMarkerFlags = new Dictionary<MarkerType, MarkerFlagController>();
    private InputAction markerHoldAction;
    public List<MarkerType> activeMarkerTypeCache = new List<MarkerType>();
    // non-serialized is needed, because otherwise activeMarkerType wouldn't be null on startup, which messes up multiple systems
    [System.NonSerialized] public MarkerType activeMarkerType;

    protected override void Awake()
    {
        base.Awake();
        markerFlagPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.MarkerFlag);
        uiCanvas = FindFirstObjectByType<Canvas>().transform;
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

    public void AddMarkersInstantly(List<MarkerData> markerDataList)
    {
        foreach(MarkerData markerData in markerDataList)
        {
            markerData.accuracy = CalculateMarkerAccuracy(markerData.ResolvedChatBubble, markerData.startIndex, markerData.endIndex);
            placedMarkers.Add(markerData);
        }
    }

    public void AddMarker(ChatLog chatLog, ChatBubble chatBubble, int start, int end)
    {
        if(!GameManager.Instance.isDayPassing) return;

        MarkerData markerData = new MarkerData(
            activeMarkerType,
            chatLog,
            chatBubble,
            start,
            end,
            GameManager.Instance.CurrentDayNumber,
            CalculateMarkerAccuracy(chatBubble, start, end)
        );

        placedMarkers.Add(markerData);
    }

    private float CalculateMarkerAccuracy(ChatBubble chatBubble, int start, int end)
    {
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
                // accuracy is a percentage based on the proportion of the markable that is correctly covered by the marker,
                // penalized by any excess marking outside the markable. The penalty for excess marking is halved.
                float totalAccuracy = ((lengthOfOverlap / markableLength) - (lengthOfExcess / markableLength / 2f)) * 100f;
                accuracy = Mathf.Max(accuracy, totalAccuracy);
            }
        });

        return accuracy;
    }

    public void RemoveMarker(MarkerData markerData)
    {
        if(!GameManager.Instance.isDayPassing) return;
        placedMarkers.Remove(markerData);
    }

    public void SetActiveMarkerTypes(List<MarkerType> markerTypes)
    {
        activeMarkerTypeCache = markerTypes;

        if(activeMarkerType != null && activeMarkerTypeCache.Count > 0)
        {
            activeMarkerFlags.Values.ToList().ForEach(mf => Destroy(mf.gameObject));
            activeMarkerFlags.Clear();
        }

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
        string chatLogPath = GetChatLogPathFromBubble(chatBubble);
        int bubbleIndex = GetBubbleIndexInLog(chatBubble);
        return placedMarkers.Where(m =>
            m.chatLogPath == chatLogPath &&
            m.chatBubbleIndex == bubbleIndex &&
            bubbleIndex >= 0).ToList();
    }

    private static string GetChatLogPathFromBubble(ChatBubble chatBubble)
    {
        if(chatBubble == null) return string.Empty;

        foreach(var log in SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetActiveChatLogs())
        {
            if(log != null && log.messages != null && log.messages.Contains(chatBubble))
            {
                return log.logName;
            }
        }

        return string.Empty;
    }

    private static int GetBubbleIndexInLog(ChatBubble chatBubble)
    {
        if(chatBubble == null) return -1;

        foreach(var log in SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetActiveChatLogs())
        {
            if(log != null && log.messages != null)
            {
                int index = log.messages.IndexOf(chatBubble);
                if(index >= 0) return index;
            }
        }

        return -1;
    }

    public void LoadFromDayData(DayData dayData)
    {
        List<MarkerType> markerTypes = dayData.GetMarkerTypes();
        SetActiveMarkerTypes(markerTypes);
        placedMarkers = dayData.GetMarkerData();
    }
}
