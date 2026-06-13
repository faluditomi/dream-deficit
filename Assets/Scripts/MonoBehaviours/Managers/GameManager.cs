using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    private SupervisorController supervisorController;
    private TMP_Text timeText;
    [Range(0, 24)]
    public float dayStartTime = 8f;
    [Range(0, 24)]
    public float dayEndTime = 17f;
    public float dayLengthInSeconds = 60f;
    private float currentDayTime = 0f;
    public bool isDayPassing = false;
    public Transform focusedWindow;

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
        // TODO: this will have to be called when we select a save slot in the menu
        SaveManager.Instance.LoadGame();
        // TODO: this will also have to be called from elsewhere
        StartDay();
    }

    public void StartDay()
    {
        currentDayTime = dayLengthInSeconds;
        UpdateTimeText();
        SaveManager.Instance.LoadDay(CurrentDayNumber);
        // TODO: this sequence will have to be gotten from the DayData's supervisor sequences
        ChatBubbleSequence nextSupervisorDayStartSequence = AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(
            Constants.AddressablePrefixes.ChatBubbleSequence + Constants.ChatBubbleSequences.DaySignalTest);
        GetSupervisorController().myChatLogController
            .RunBubbleSequence(nextSupervisorDayStartSequence, Constants.ChatBubbleSequenceType.SupervisorDayStart);
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
        // TODO: this sequence will have to be gotten from the DayData's supervisor sequences
        ChatBubbleSequence nextSupervisorDayEndSequence = AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(
            Constants.AddressablePrefixes.ChatBubbleSequence + Constants.ChatBubbleSequences.DaySignalTest);
        GetSupervisorController().myChatLogController
            .RunBubbleSequence(nextSupervisorDayEndSequence, Constants.ChatBubbleSequenceType.SupervisorDayEnd);
    }

    public void EndDay()
    {
        StartCoroutine(EndOfDaybehaviour());
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

    public SupervisorController GetSupervisorController()
    {
        if(supervisorController == null)
        {
            supervisorController = FindFirstObjectByType<SupervisorController>();
        }

        return supervisorController;
    }

    #region Chat Bubble Sequence Activator Logic
    public void TriggerChatBubbleSequence(Constants.ChatBubbleSequenceType chatBubbleSequenceType)
    {
        switch(chatBubbleSequenceType)
        {
            case Constants.ChatBubbleSequenceType.Simple:
                break;
            case Constants.ChatBubbleSequenceType.SupervisorDayStart:
                SupervisorDayStartSequence();  
                break; 
            case Constants.ChatBubbleSequenceType.SupervisorDayEnd:
                SupervisorDayEndSequence();
                break;
        }
    }

    private void SupervisorDayStartSequence()
    {
        GetSupervisorController().DayStartSignal();
    }

    private void SupervisorDayEndSequence()
    {
        GetSupervisorController().DayEndSignal();
    }
    #endregion

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
