using Unity.VisualScripting;

[System.Serializable]
public class MarkerData
{
    public MarkerType markerType;
    public ChatLog chatLog;
    public ChatBubble chatBubble;
    public int startIndex;
    public int endIndex;
    public int dayNumber;
    public float accuracy;

    public MarkerData
    (
        MarkerType markerType, 
        ChatLog chatLog, 
        ChatBubble chatBubble, 
        int startIndex, 
        int endIndex,
        int dayNumber,
        float accuracy
    )
    {
        this.markerType = markerType;
        this.chatLog = chatLog;
        this.chatBubble = chatBubble;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.dayNumber = dayNumber;
        this.accuracy = accuracy;
    }
}