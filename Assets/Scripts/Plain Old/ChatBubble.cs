using UnityEngine;

[System.Serializable]
public class ChatBubble
{
    public ChatUserId chatUserId;
    [TextArea(2, 5)] public string message;
    [Range(0, 10)] public float delayLength;
    [Range(0, 10)] public float typingFlagLength;
}