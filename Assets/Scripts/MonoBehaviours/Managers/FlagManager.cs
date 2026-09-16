using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class FlagManager : Singleton<FlagManager>, ILoadable, ISavable
{
    private GameObject flagIndicatorPrefab;
    private Transform uiCanvas;
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
        uiCanvas = FindFirstObjectByType<Canvas>().transform;
    }

    public void LoadFromDayData(DayData dayData)
    {
        activeFlagTypeCache.Clear();
        activeFlagTypeCache.AddRange(dayData.GetFlagTypes());
        SetActiveFlagTypes();
    }

    public void SaveToDayData(DayData dayData)
    {
        dayData.flagData = placedFlags;
    }

    public void OnKeyDown(Key key)
    {
        if(!GameManager.Instance.isDayPassing) return;

        if(activeFlagType != null)
        {
            activeFlagIndicators[activeFlagType].SetRaised(false);
            activeFlagType = null;
        }

        activeFlagType = activeFlagTypeCache.Find(mt => mt.keycode == key);

        if(activeFlagType != null) activeFlagIndicators[activeFlagType].SetRaised(true);
    }
    public void OnKeyUp(Key key)
    {
        if(activeFlagType != null && activeFlagType.keycode == key)
        {
            activeFlagIndicators[activeFlagType].SetRaised(false);
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
            GameManager.Instance.CurrentDayNumber,
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
        }
    }

    public void RemoveFlag(FlagData flagData)
    {
        if(!GameManager.Instance.isDayPassing) return;
        placedFlags.Remove(flagData);
    }

    public void SetActiveFlagTypes()
    {
        if(activeFlagType != null && activeFlagTypeCache.Count > 0)
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

        float flagIndicatorOffset = Screen.width / (activeFlagTypeCache.Count + 1);

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

        return placedFlags.Where(m =>
            m.chatLogPath == chatLog.logName &&
            m.nodeGuid == nodeGuid).ToList();
    }

    /// <summary>
    /// Accrues and returns the accuracy of the total flags placed on the chat log.
    /// Returns 0 if no flags are present.
    /// </summary>
    public float EvaluateChatLogAccuracy(ChatLog chatLog)
    {
        List<FlagData> flags = placedFlags.Where(m => m.chatLogPath == chatLog.logName).ToList();
        if(flags.Count == 0) return 0;
        float totalAccuracy = 0;
        // NOTE: for now, duplicate flags for a single flaggable are not removed, since if i'm correct,
        //       the potential advantage gained by putting more flags on a single flaggable is negated
        //       by the fact that we divide the total accuracy with the total flag count.
        flags.ForEach(m => totalAccuracy += m.accuracy);

        return totalAccuracy / flags.Count;
    }
}
