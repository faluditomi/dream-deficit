using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatLog", menuName = "Scriptable Objects/ChatLog")]
public class ChatLog : ScriptableObject
{
    public string logName;
    public List<ChatBubble> messages;
    public bool isOpen = false;
}

[System.Serializable]
public class ChatBubble
{
    public ChatUserId chatUserId;
    [TextArea(2, 5)]
    public string message;
}
