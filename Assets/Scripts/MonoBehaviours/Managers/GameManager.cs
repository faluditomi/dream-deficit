using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    private SequenceEventChannel dayStartEventChannel;
    private SequenceEventChannel dayEndEventChannel;
    private TMP_Text timeText;
    [Range(0, 24)] public float dayStartTime = 8f;
    [Range(0, 24)] public float dayEndTime = 17f;
    public float dayLengthInSeconds = 60f;
    private float currentDayTime = 0f;
    public bool isDayPassing = false;
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
        dayEndEventChannel = AddressableManager.Instance.RetrieveAddressable<SequenceEventChannel>(Constants.SequenceEventChannels.DayEnd);
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
        dayEndEventChannel.Raise();
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

    #region Chat Bubble Sequence Extra Behaviour Activator Logic
    public void TriggerChatBubbleSequence(Constants.ChatBubbleSequenceType chatBubbleSequenceType)
    {
        switch(chatBubbleSequenceType)
        {
            case Constants.ChatBubbleSequenceType.Simple:
                break;
            case Constants.ChatBubbleSequenceType.SupervisorDayStart:
            
            
                // TODO: these came here from the old SuperVisorController
                // TODO: for now it's shit code, because later we'll need a central solution for chat responses and this will be a use case for that
                GameObject daySignalButtonPrefab = AddressableManager.Instance
                    .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.DaySignalButton);
                ChatLogController myChatLogController = ChatLogManager.Instance.GetChatLogControllerByLogName(Constants.ChatLogs.Phoebe);
                Transform chatBubbleHolder = myChatLogController.transform.Find(Constants.GameObjectNames.Viewport).Find(Constants.GameObjectNames.Content);
                GameObject startDayButton = Instantiate(daySignalButtonPrefab, chatBubbleHolder);
                Button button = startDayButton.GetComponent<Button>();
                button.GetComponentInChildren<TMP_Text>().text = "Start Day";
                button.onClick.AddListener(() => 
                {
                    GameManager.Instance.TriggerDayTimePassing();
                    Destroy(startDayButton);
                });
                // TODO: shit code until here


                break; 
            case Constants.ChatBubbleSequenceType.SupervisorDayEnd:
            

                // TODO: these came here from the old SuperVisorController
                // TODO: for now it's shit code, because later we'll need a central solution for chat responses and this will be a use case for that
                GameObject daySignalButtonPrefab2 = AddressableManager.Instance
                    .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.DaySignalButton);
                ChatLogController myChatLogController2 = ChatLogManager.Instance.GetChatLogControllerByLogName(Constants.ChatLogs.Phoebe);
                Transform chatBubbleHolder2 = myChatLogController2.transform.Find(Constants.GameObjectNames.Viewport).Find(Constants.GameObjectNames.Content);
                GameObject endDayButton = Instantiate(daySignalButtonPrefab2, chatBubbleHolder2);
                Button button2 = endDayButton.GetComponent<Button>();
                button2.GetComponentInChildren<TMP_Text>().text = "End Day";
                button2.onClick.AddListener(() => 
                {
                    GameManager.Instance.EndDay();
                    Destroy(endDayButton);
                });
                // TODO: shit code until here


                break;
        }
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
