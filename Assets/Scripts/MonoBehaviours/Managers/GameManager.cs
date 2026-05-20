using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private SupervisorController supervisorController;

    private TMP_Text timeText;
    
    [Range(0, 24)]
    public float dayStartTime = 8f; // 8 AM
    [Range(0, 24)]
    public float dayEndTime = 17f; // 5 PM
    public float dayLengthInSeconds = 60f;
    private float currentDayTime = 0f;
    private int dayNumber = 0;
    public bool isDayPassing = false;

    protected override void Awake()
    {
        base.Awake();
        timeText = GameObject.Find(Constants.GameObjectNames.Clock)
            .transform.Find(Constants.GameObjectNames.Time)
            .GetComponent<TMP_Text>();
    }

    private void Start()
    {
        // TODO: this will have to be called from somewhere else
        StartDay();
    }

    // TODO: called from the dream scene based on an end state
    private void StartDay()
    {
        dayNumber++;
        currentDayTime = dayLengthInSeconds;
        UpdateTimeText();
        // TODO: reset user interface
        // TODO: depending on the day number, we establish the active chat log cache
        ChatBubbleSequence nextSupervisorDayStartSequence = AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(
            // TODO: these sequences will have to change dynamically based on day number and performance
            Constants.AddressablePaths.ChatBubbleSequence + Constants.ChatBubbleSequenceCodes.DaySignalTest);
        GetSupervisorController().chatLogController
            .RunBubbleSequence(nextSupervisorDayStartSequence, ChatBubbleSequenceType.SupervisorDayStart);
    }

    // TODO: called from the supervisor button signaling the player's readiness for the day
    public void TriggerDayTimePassing()
    {
        isDayPassing = true;
        // TODO: turn on marking and other interactive elements of the user interface
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
        // TODO: turn off marking and other interactive elements of the user interface
        ChatBubbleSequence nextSupervisorDayEndSequence = AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(
            // TODO: these sequences will have to change dynamically based on day number and performance
            Constants.AddressablePaths.ChatBubbleSequence + Constants.ChatBubbleSequenceCodes.DaySignalTest);
        GetSupervisorController().chatLogController
            .RunBubbleSequence(nextSupervisorDayEndSequence, ChatBubbleSequenceType.SupervisorDayEnd);
    }

    // TODO: called from the supervisor button signaling the player's readiness for the dream
    public void EndDay()
    {
        // TODO: depending on the day number, trigger the appropriate dream scene
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

    private SupervisorController GetSupervisorController()
    {
        if(supervisorController == null)
        {
            supervisorController = FindFirstObjectByType<SupervisorController>();
        }

        return supervisorController;
    }

    #region Chat Bubble Sequence Activator Logic
    public void TriggerChatBubbleSequence(ChatBubbleSequenceType chatBubbleSequenceType)
    {
        switch(chatBubbleSequenceType)
        {
            case ChatBubbleSequenceType.Simple:
                break;
            case ChatBubbleSequenceType.SupervisorDayStart:
                SupervisorDayStartSequence();  
                break; 
            case ChatBubbleSequenceType.SupervisorDayEnd:
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
}
