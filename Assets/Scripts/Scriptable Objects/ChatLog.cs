using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatLog", menuName = "Scriptable Objects/ChatLog")]
public class ChatLog : ScriptableObject
{
    public string logName;
    public List<ChatBubble> messages;
}
