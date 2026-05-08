[System.Serializable]
public class MarkerData
{
    public MarkerType markerType;
    public ChatLog chatLog;
    public ChatBubble chatBubble;
    public int startIndex;
    public int endIndex;
    public int dayNumber;
    public bool isValid;

    public MarkerData
    (
        MarkerType markerType, 
        ChatLog chatLog, 
        ChatBubble chatBubble, 
        int startIndex, 
        int endIndex
    )
    {
        this.markerType = markerType;
        this.chatLog = chatLog;
        this.chatBubble = chatBubble;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        // TODO: once a GameManager and the day-night cycle are in place, we have to set this for archival/statistical purposes
        this.dayNumber = 0;
        // TODO: this has to be calculated once we are tracking the possible markers for each markable bubble
        isValid = true;
    }
}