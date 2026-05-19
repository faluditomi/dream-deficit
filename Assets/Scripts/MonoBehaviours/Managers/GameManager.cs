using TMPro;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
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
        // TODO: these will have to be removed once the supervisor is properly hooked up
        StartDay();
        TriggerDayTimePassing();
    }

    // TODO: called from the dream scene based on an end state
    private void StartDay()
    {
        dayNumber++;
        UpdateTimeText();
        // TODO: reset user interface
        // TODO: depending on the day number, we establish the active chat log cache and trigger the supervisor
        // TODO: trigger supervisor
    }

    // TODO: called from the supervisor button signaling the player's readiness for the day
    public void TriggerDayTimePassing()
    {
        currentDayTime = dayLengthInSeconds;
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

    public void TriggerEndOfDayTimePassing()
    {
        isDayPassing = false;
        currentDayTime = dayLengthInSeconds;
        timeText.text = FormatTime(dayEndTime);
        // TODO: turn off marking and other interactive elements of the user interface
        // TODO: trigger the supervisor
    }

    // TODO: called from the supervisor button signaling the player's readiness for the dream
    private void EndDay()
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
}
