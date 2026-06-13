using UnityEngine;

[CreateAssetMenu(fileName = "SequenceEventChannel", menuName = "Scriptable Objects/SequenceEventChannel")]
public class SequenceEventChannel : ScriptableObject
{
    public Constants.SequenceEventType eventType;
    public event System.Action<SequenceEventData> OnSequenceEvent;

    public void Raise(Constants.ChatUser chatUser, int currentDayNumber, ChatLogController chatLogController)
    {
        SequenceEventData data = new SequenceEventData(eventType, chatUser, currentDayNumber, chatLogController);

        OnSequenceEvent?.Invoke(data);
    }

    private void OnDisable() => OnSequenceEvent = null;
}

public class SequenceEventData
{
    public Constants.SequenceEventType eventType;
    public Constants.ChatUser chatUser;
    public int currentDayNumber;
    public ChatLogController chatLogController;

    public SequenceEventData(Constants.SequenceEventType eventType, Constants.ChatUser chatUser, int currentDayNumber, ChatLogController chatLogController)
    {
        this.eventType = eventType;
        this.chatUser = chatUser;
        this.currentDayNumber = currentDayNumber;
        this.chatLogController = chatLogController;
    }
}
