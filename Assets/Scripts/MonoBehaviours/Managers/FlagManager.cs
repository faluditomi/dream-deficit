using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

public class FlagManager : Singleton<FlagManager>, IDayLoadable, IDaySavable
{
    private GameObject flagIndicatorPrefab;
    private List<FlagData> placedFlags = new List<FlagData>();
    public List<FlagType> activeFlagTypeCache = new List<FlagType>();
    private Dictionary<FlagType, FlagIndicatorController> activeFlagIndicators = new Dictionary<FlagType, FlagIndicatorController>();
    private InputAction flagHoldAction;
    // non-serialized is needed, because otherwise activeFlagType wouldn't be null on startup, which messes up multiple systems
    [System.NonSerialized] public FlagType activeFlagType;
    private SequenceEventChannel flagOverloadSequenceEventChannel;
    private static float _flagOverloadWordCountModifier = 0.8f;
    private static int _flagOverloadMinimumThreshold = 4;

    protected override void Awake()
    {
        base.Awake();
        flagIndicatorPrefab = AddressableManager.Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.FlagIndicator);
        flagOverloadSequenceEventChannel = AddressableManager.Instance
            .RetrieveAddressable<SequenceEventChannel>(Constants.SequenceEventChannels.FlagOverload);
    }

    private Transform ResolveUiCanvas()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        foreach(GameObject root in activeScene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if(canvas != null) return canvas.transform;
        }

        Canvas fallback = FindFirstObjectByType<Canvas>(FindObjectsInactive.Exclude);
        return fallback != null ? fallback.transform : null;
    }

    public void LoadFromDayData(DayData dayData)
    {
        activeFlagType = null;
        activeFlagTypeCache.Clear();
        placedFlags.Clear();
        activeFlagTypeCache.AddRange(dayData.GetFlagTypes());
        SetActiveFlagTypes();
    }

    public void SaveToDayData(DayData dayData)
    {
        dayData.flagData = placedFlags.ToList();
    }

    public void OnKeyDown(Key key)
    {
        if(!GameManager.Instance.isDayPassing) return;

        if(activeFlagType != null)
        {
            activeFlagIndicators[activeFlagType].SetLowered(true);
            activeFlagType = null;
        }

        activeFlagType = activeFlagTypeCache.Find(mt => mt.keycode == key);
        if(activeFlagType != null) activeFlagIndicators[activeFlagType].SetLowered(false);
    }
    public void OnKeyUp(Key key)
    {
        if(activeFlagType != null && activeFlagType.keycode == key)
        {
            activeFlagIndicators[activeFlagType].SetLowered(true);
            activeFlagType = null;
        }
    }

    public void AddFlag(ChatLog chatLog, ChatBubble chatBubble, string nodeGuid, int start, int end)
    {
        if(!GameManager.Instance.isDayPassing) return;

        FlagData flagData = new FlagData(
            activeFlagType,
            chatLog,
            start,
            end,
            GameManager.Instance.currentDayNumber,
            CalculateFlagAccuracy(chatBubble, start, end),
            nodeGuid
        );

        placedFlags.Add(flagData);
        FlagOverloadEventCheck(chatLog, chatBubble, nodeGuid);
    }

    private float CalculateFlagAccuracy(ChatBubble chatBubble, int start, int end)
    {
        if(chatBubble == null || chatBubble.flaggables == null) return 0f;
        float accuracy = 0f;

        chatBubble.flaggables.ForEach(flaggable =>
        {
            if(flaggable.startIndex < end && flaggable.endIndex > start && flaggable.flagType.name.Equals(activeFlagType.name))
            {
                float flaggableLength = flaggable.endIndex - flaggable.startIndex + 1;
                float lengthOfOverlap = Mathf.Min(flaggable.endIndex, end) - Mathf.Max(flaggable.startIndex, start) + 1;
                float lengthOfExcess = Mathf.Max(flaggable.startIndex - start, 0f) + Mathf.Max(end - flaggable.endIndex, 0f);
                // accuracy is a percentage based on the proportion of the flaggable that is correctly covered by the flag,
                // penalized by any excess marking outside the flaggable. The penalty for excess marking is halved.
                float totalAccuracy = ((lengthOfOverlap / flaggableLength) - (lengthOfExcess / flaggableLength / 2f)) * 100f;
                accuracy = Mathf.Max(accuracy, totalAccuracy);
            }
        });

        return accuracy;
    }

    private void FlagOverloadEventCheck(ChatLog chatLog, ChatBubble chatBubble, string nodeGuid)
    {
        int wordCount = chatBubble.message.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
        int overloadThreshold = Mathf.Max(Mathf.CeilToInt(wordCount * _flagOverloadWordCountModifier), _flagOverloadMinimumThreshold);
        
        if(overloadThreshold <= GetFlagsForChatBubble(chatLog, nodeGuid).Count)
        {
            flagOverloadSequenceEventChannel.Raise();
            Debug.Log("Raised: Flag Overload Sequence Event");
        }
    }

    public void RemoveFlag(FlagData flagData)
    {
        if(!GameManager.Instance.isDayPassing) return;
        if(flagData != null) flagData.isRemoved = true;
    }

    public void SetActiveFlagTypes()
    {
        activeFlagType = null;

        if(activeFlagIndicators.Count > 0)
        {
            activeFlagIndicators.Values.ToList().ForEach(mf => Destroy(mf.gameObject));
            activeFlagIndicators.Clear();
        }

        if(flagHoldAction != null) 
        {
            flagHoldAction.Disable();
            flagHoldAction.Dispose();
        }

        flagHoldAction = new InputAction(type: InputActionType.PassThrough);
        
        flagHoldAction.performed += ctx =>
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

        Transform uiCanvas = ResolveUiCanvas();
        float flagIndicatorOffset = Screen.currentResolution.width / (activeFlagTypeCache.Count + 1);

        foreach(FlagType flagType in activeFlagTypeCache)
        {
            flagHoldAction.AddBinding("<Keyboard>/" + flagType.keycode.ToString());
            FlagIndicatorController newFlagIndicator = Instantiate(flagIndicatorPrefab, uiCanvas).GetComponent<FlagIndicatorController>();
            activeFlagIndicators[flagType] = newFlagIndicator;
            float xPos = flagIndicatorOffset * activeFlagIndicators.Count;
            newFlagIndicator.Setup(flagType, xPos);
        }

        flagHoldAction.Enable();
    }

    public List<FlagData> GetFlagsForChatBubble(ChatLog chatLog, string nodeGuid)
    {
        if(chatLog == null || string.IsNullOrEmpty(nodeGuid)) return new List<FlagData>();

        return placedFlags.Where(m => m.chatLogPath == chatLog.name && m.nodeGuid == nodeGuid && !m.isRemoved).ToList();
    }

    /// <summary>
    /// Accrues and returns the accuracy of the total flags placed on the chat log.
    /// Returns 0 if no flags are present.
    /// </summary>
    public float CalculateChatLogAccuracy(ChatLog chatLog)
    {
        List<FlagData> flags = placedFlags.Where(m => m.chatLogPath == chatLog.name && !m.isRemoved).ToList();
        if(flags.Count == 0) return 0;
        float totalAccuracy = 0;
        // NOTE: for now, duplicate flags for a single flaggable are not removed, since if i'm correct,
        //       the potential advantage gained by putting more flags on a single flaggable is negated
        //       by the fact that we divide the total accuracy with the total flag count.
        flags.ForEach(m => totalAccuracy += m.accuracy);

        return totalAccuracy / flags.Count;
    }
}
