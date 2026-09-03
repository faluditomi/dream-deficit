using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SequenceEventChannel", menuName = "Scriptable Objects/SequenceEventChannel")]
public class SequenceEventChannel : ScriptableObject
{
    public Constants.SequenceEventType eventType;
    public event Action<SequenceEventData> OnSequenceEvent;

    public void Raise()
    {
        List<List<SequenceEventData>> variationSequenceEventData = new List<List<SequenceEventData>>();

        SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetSequences().ForEach(sequence => {
            SequenceEventData sequenceEventData = SequenceEventData.TryDecode(sequence.name);
            if(sequenceEventData == null || sequenceEventData.eventType != eventType) return;
            ChatLogController chatLogController = ChatLogManager.Instance.GetChatLogControllerByLogName(sequenceEventData.chatLogName);
            sequenceEventData.chatLogController = chatLogController;

            if(sequenceEventData.variationNumber != -1)
            {
                List<SequenceEventData> existingVariationList = variationSequenceEventData.Find(e => e[0].chatLogName == sequenceEventData.chatLogName);

                if(existingVariationList == null)
                {
                    variationSequenceEventData.Add(new List<SequenceEventData> { sequenceEventData });
                }
                else
                {
                    existingVariationList.Add(sequenceEventData);
                }
            }
            OnSequenceEvent?.Invoke(sequenceEventData);
        });

        variationSequenceEventData.ForEach(variationList =>
        {
            SequenceEventData sequenceEventData = variationList[UnityEngine.Random.Range(0, variationList.Count - 1)];
            OnSequenceEvent?.Invoke(sequenceEventData);
        });
    }

    private void OnDisable() => OnSequenceEvent = null;
}

public class SequenceEventData
{
    public string chatBubbleSequenceName;
    public Constants.SequenceEventType eventType;
    public string chatLogName;
    public Constants.ChatBubbleSequenceType chatBubbleSequenceType;
    public ChatLogController chatLogController;
    public int variationNumber;

    public SequenceEventData(
        string chatBubbleSequenceName,
        Constants.SequenceEventType eventType,
        string chatLogName,
        Constants.ChatBubbleSequenceType chatBubbleSequenceType,
        ChatLogController chatLogController,
        int variationNumber)
    {
        this.chatBubbleSequenceName = chatBubbleSequenceName;
        this.eventType = eventType;
        this.chatLogName = chatLogName;
        this.chatBubbleSequenceType = chatBubbleSequenceType;
        this.chatLogController = chatLogController;
        this.variationNumber = variationNumber;
    }

    /// <summary>
    /// Decodes a string matching the format:
    /// "event_sequence_" + {SequenceEventType} + "_" + {chatLogName} + "_" + {chatBubbleSequenceType} + "_" + {variationNumber}
    /// The variationNumber segment may be omitted.
    /// Returns null if the string cannot be decoded, otherwise a SequenceEventData with chatLogController = null.
    /// </summary>
    public static SequenceEventData TryDecode(string eventSequenceName)
    {
        if(string.IsNullOrEmpty(eventSequenceName)) return null;

        string[] parts = eventSequenceName.Split('_');

        // Minimum required segments: event_sequence_{eventType}_{chatLogName}_{chatBubbleSequenceType}
        if(parts.Length < 5 || parts[0] != "event" || parts[1] != "sequence") return null;

        if(!Enum.TryParse(parts[2], true, out Constants.SequenceEventType eventType))
        {
            Debug.LogError("A Sequence Event was triggered but was received with invalid Event Type: " + parts[2]);
            return null;
        }
        if(!Enum.TryParse(parts[4], true, out Constants.ChatBubbleSequenceType chatBubbleSequenceType))
        {
            Debug.LogError("A Sequence Event was triggered but was received with invalid Bubble Type: " + parts[4]);
            return null;
        }

        // The variationNumber is optional. If the last segment is an int, it is the variation;
        // otherwise the last segment must be the chatUser.
        bool hasVariation = int.TryParse(parts[parts.Length - 1], out int variationNumber);
        int chatUserIndex = hasVariation ? parts.Length - 2 : parts.Length - 1;

        // chatLogName is everything between eventType and chatUser, so it may contain underscores.
        string chatLogName = string.Join("_", parts, 3, chatUserIndex - 3);
        if(string.IsNullOrEmpty(chatLogName)) return null;
        
        return new SequenceEventData(
            eventSequenceName,
            eventType,
            chatLogName,
            chatBubbleSequenceType,
            null,
            hasVariation ? variationNumber : -1);
    }
}
