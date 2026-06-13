using System;
using System.Collections.Generic;
using UnityEngine;

// when creating a CahtBubbleSequence that we want SequenceEventManager to pick up automatically, we have to name it like:
// "event_sequence_" + {Constants.SequenceEventType} + "_" + {Constants.ChatUser} + "_till_day_" + {dayNumberTillRelevant} + "_var_" + {variationNumber}
// writing a 0 in place of dayNumberTillRelevant means it is not constrained to any number of days and can always be activated. 
public class SequenceEventManager : Singleton<SequenceEventManager>
{
    List<SequenceEventChannel> eventChannels;
    List<SequenceCacheEntry> sequenceCache = new List<SequenceCacheEntry>();

    protected override void Awake()
    {
        base.Awake();
        eventChannels = AddressableManager.Instance
            .RetrieveAddressablesByLabel<SequenceEventChannel>(Constants.AddressableLabels.EventChannel);
        List<ChatBubbleSequence> sequences = AddressableManager.Instance
            .RetrieveAddressablesByLabel<ChatBubbleSequence>(Constants.AddressableLabels.EventChatBubbleSequence);

        sequences.ForEach(sequence =>
        {
            string[] parts = sequence.name.Split('_');

            if(parts.Length >= 7 && parts[0] == "event" && parts[1] == "sequence" && parts[4] == "till" && parts[5] == "day")
            {
                string eventTypeStr = parts[2];
                string chatUserStr = parts[3];

                if(int.TryParse(parts[6], out int dayNumber) &&
                    Enum.TryParse(eventTypeStr, true, out Constants.SequenceEventType eventType) &&
                    Enum.TryParse(chatUserStr, true, out Constants.ChatUser chatUser))
                {
                    sequenceCache.Add(new SequenceCacheEntry(sequence, eventType, chatUser, dayNumber));
                }
            }
            else
            {
                Debug.LogError("Found EventChatBubbleSequence Addressable with invalid name format: " + sequence.name
                 + ". Expected format: event_sequence_{SequenceEventType}_{ChatUser}_till_day_{dayNumberTillRelevant}");
            }
        });

        foreach(SequenceEventChannel channel in eventChannels) channel.OnSequenceEvent += OnSequenceEvent;
    }

    private void OnDestroy()
    {
        foreach(SequenceEventChannel channel in eventChannels) channel.OnSequenceEvent -= OnSequenceEvent;
    }

    private void OnSequenceEvent(SequenceEventData data)
    {
        // TODO: handle the event here

    }
    
    private class SequenceCacheEntry
    {
        public ChatBubbleSequence sequence { get; }
        public Constants.SequenceEventType eventType { get; }
        public Constants.ChatUser chatUser { get; }
        public int dayNumberTillRelevant { get; }

        public SequenceCacheEntry(
            ChatBubbleSequence sequence, 
            Constants.SequenceEventType eventType, 
            Constants.ChatUser chatUser, 
            int dayNumberTillRelevant)
        {
            this.sequence = sequence;
            this.eventType = eventType;
            this.chatUser = chatUser;
            this.dayNumberTillRelevant = dayNumberTillRelevant;
        }
    }

}
