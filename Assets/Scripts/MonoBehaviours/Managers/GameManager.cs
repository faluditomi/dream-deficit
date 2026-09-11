using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    private SequenceEventChannel dayStartEventChannel;
    private SequenceEventChannel workEndEventChannel;
    private TMP_Text timeText;
    [Range(0, 24)] public float dayStartTime = 8f;
    [Range(0, 24)] public float dayEndTime = 17f;
    public float dayLengthInSeconds = 60f;
    private float currentDayTime = 0f;
    public bool isDayPassing = false;
    // NOTE: set while EndDay is deferred behind an unresolved day-blocking choice.
    //       Guards EndDay against re-entry so EndOfDaybehaviour can never be started twice.
    private bool isEndDayDeferred = false;
    [HideInInspector] public Transform focusedWindow;

    public int CurrentDayNumber
    {
        get => SaveManager.Instance != null && SaveManager.Instance.activeSlot != null
            ? SaveManager.Instance.activeSlot.currentDayNumber
            : 1;
        set
        {
            if(SaveManager.Instance != null && SaveManager.Instance.activeSlot != null)
            {
                SaveManager.Instance.activeSlot.currentDayNumber = value;
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        timeText = GameObject.Find(Constants.GameObjectNames.Clock)
            .transform.Find(Constants.GameObjectNames.Time)
            .GetComponent<TMP_Text>();
    }

    private void Start()
    {
        dayStartEventChannel = AddressableManager.Instance.RetrieveAddressable<SequenceEventChannel>(Constants.SequenceEventChannels.DayStart);
        workEndEventChannel = AddressableManager.Instance.RetrieveAddressable<SequenceEventChannel>(Constants.SequenceEventChannels.WorkEnd);
        // TODO: this will have to be called when we select a save slot in the menu
        SaveManager.Instance.LoadGame();
        ConversationManager.Instance.OnSequenceEventRaised += OnSequenceEventRaised;
        // TODO: this will also have to be called from elsewhere
        StartDay();
    }

    public void StartDay()
    {
        currentDayTime = dayLengthInSeconds;
        UpdateTimeText();
        // NOTE: restore run-level chat state BEFORE the day's windows are built, so chat log controllers render the restored history during setup,
        //       and so runners are reused (never recreated) and windows stay subscribed to them. OnDayChanged then guarantees a runner exists for
        //       every active log before DayStart fires — otherwise the event reaches an empty runner set and conversations never start.
        ConversationManager.Instance?.RestoreChatState(SaveManager.Instance.chatRunState);
        SaveManager.Instance.LoadDay(CurrentDayNumber);
        ConversationManager.Instance?.OnDayChanged();
        dayStartEventChannel.Raise();
    }

    public void TriggerDayTimePassing()
    {
        isDayPassing = true;
    }

    private void Update() 
    {
        if(isDayPassing)
        {
            currentDayTime -= Time.deltaTime;
            UpdateTimeText();

            if(currentDayTime <= 0)
            {
                TriggerEndOfDayTimePassing();
            }
        }
    }

    private void TriggerEndOfDayTimePassing()
    {
        isDayPassing = false;
        currentDayTime = dayLengthInSeconds;
        timeText.text = FormatTime(dayEndTime);
        if(workEndEventChannel != null) workEndEventChannel.Raise();
    }
    
    private void OnSequenceEventRaised(Constants.SequenceEventType eventType)
    {
        switch(eventType)
        {
            case Constants.SequenceEventType.WorkStart:
                TriggerDayTimePassing();
                break;

            case Constants.SequenceEventType.DayEnd:
                EndDay();
                break;
        }
    }

    public void EndDay()
    {
        // NOTE: the day does not end while any day-blocking choice is unresolved. Defer and
        //       retry once ConversationManager reports that the blocking choice has been cleared.
        if(isEndDayDeferred) return;

        if(ConversationManager.Instance.HasUnresolvedDayBlockingChoice)
        {
            isEndDayDeferred = true;
            ConversationManager.Instance.OnDayBlockingChoiceResolved += OnDayBlockingChoiceResolved;
            return;
        }

        StartCoroutine(EndOfDaybehaviour());
    }

    private void OnDayBlockingChoiceResolved()
    {
        ConversationManager.Instance.OnDayBlockingChoiceResolved -= OnDayBlockingChoiceResolved;
        isEndDayDeferred = false;
        EndDay();
    }

    private void UpdateTimeText()
    {
        float progress = 1f - (currentDayTime / dayLengthInSeconds);
        float currentHour = dayStartTime + (dayEndTime - dayStartTime) * progress;
        timeText.text = FormatTime(currentHour);
    }

    private string FormatTime(float hourOfDay)
    {
        int totalMinutes = Mathf.RoundToInt(hourOfDay * 60f);
        totalMinutes = Mathf.RoundToInt(totalMinutes / 10f) * 10;
        totalMinutes = Mathf.Clamp(totalMinutes, 0, 23 * 60 + 59);

        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        return $"{hours:D2} : {minutes:D2}";
    }

    #region Coroutines
    private IEnumerator EndOfDaybehaviour()
    {
        SaveManager.Instance.SaveDay(CurrentDayNumber);
        Scene oldScene = SceneManager.GetActiveScene();
        string dreamSceneName = Constants.SceneNames.DreamPrefix + CurrentDayNumber;
        CurrentDayNumber++;
        AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(dreamSceneName, LoadSceneMode.Additive);
        sceneLoadOperation.allowSceneActivation = false;
        // TODO: do stuff like screen turning off animation and stuff

        yield return new WaitUntil(() => sceneLoadOperation.progress >= 0.9f);

        sceneLoadOperation.allowSceneActivation = true;

        yield return new WaitUntil(() => sceneLoadOperation.isDone);
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(dreamSceneName));
        SceneManager.UnloadSceneAsync(oldScene);
    }
    #endregion
}
