using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChatBubble
{
    public Constants.ChatUser chatUser;
    [TextArea(2, 5)] public string message;
    [Range(0, 10)] public float delayLength;
    [Range(0, 10)] public float typingFlagLength;
    [SerializeField] public List<Markable> markables = new List<Markable>();

    public void SyncMarkables()
    {
        if(markables == null) markables = new List<Markable>();
        
        foreach(var markable in markables)
        {
            if(markable == null) continue;
            markable.RecalculateIndexes(message);
        }
    }
}