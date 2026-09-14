using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChatBubble
{
    public Constants.ChatUser chatUser;
    [TextArea(2, 5)] public string message;
    [Range(0, 10)] public float delayLength;
    [Range(0, 10)] public float typingIndicatorLength;
    [SerializeField] public List<Flaggable> flaggables = new List<Flaggable>();

    public void SyncFlaggables()
    {
        if(flaggables == null) flaggables = new List<Flaggable>();
        
        foreach(var flaggable in flaggables)
        {
            if(flaggable == null) continue;
            flaggable.RecalculateIndexes(message);
        }
    }
}
