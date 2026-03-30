using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatBubbleSequence", menuName = "Scriptable Objects/ChatBubbleSequence")]
public class ChatBubbleSequence : ScriptableObject
{
    public List<ChatBubble> messages;
}
