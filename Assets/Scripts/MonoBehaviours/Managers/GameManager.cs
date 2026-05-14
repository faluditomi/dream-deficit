using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private int dayNumber = 0;
    public float dayLengthInSeconds = 60f;
    private float currentDayTime = 0f;
    private bool isDayPassing = false;

    // TODO: called from the dream scene based on an end state
    private void StartDay()
    {
        dayNumber++;
        // TODO: reset user interface method
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
            // TODO: update the visal representations of the passing of time

            if(currentDayTime <= 0)
            {
                EndDay();
            }
        }
    }

    // TODO: triggered from the supervisor button signaling the player's readiness for the dream
    public void TriggerEndOfDayTimePassing()
    {
        isDayPassing = false;
        currentDayTime = dayLengthInSeconds;
        // TODO: turn off marking and other interactive elements of the user interface
        // TODO: trigger the supervisor
    }

    private void EndDay()
    {
        // TODO: depending on the day number, trigger the appropriate dream scene
    }
}
