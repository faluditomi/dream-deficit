using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

// when creating a ChatBubbleSequence that we want SequenceEventManager to pick up automatically, we have to name it like:
// "event_sequence_" + {Constants.SequenceEventType} + "_" + {Constants.ChatUser} + "_till_day_" + {dayNumberTillRelevant} + "_var_" + {variationNumber}
// writing a 0 in place of dayNumberTillRelevant means it is not constrained to any number of days and can always be activated. 
public class SequenceEventManager : Singleton<SequenceEventManager>
{
    private Dictionary<SequenceEventChannel, EventChannelMetadata> eventChannelsWithMetadata = new();
    private List<SequenceCacheEntry> sequenceCache = new List<SequenceCacheEntry>();

    protected override void Awake()
    {
        base.Awake();
        List<SequenceEventChannel> eventChannels = AddressableManager.Instance
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

        foreach(SequenceEventChannel channel in eventChannels)
        {
            EventChannelMetadata template = EventChannelMetadata.metadata.Find(m => m.eventType == channel.eventType) 
                ?? EventChannelMetadata.metadata.Find(m => m.eventType == Constants.SequenceEventType.Default);
            
            EventChannelMetadata metadata = new EventChannelMetadata(
                template.eventType, 
                template.cooldownDurationInSeconds
            );

            eventChannelsWithMetadata.Add(channel, metadata);
            channel.OnSequenceEvent += (data) => OnSequenceEvent(channel, data);
        }
    }

    private void OnDestroy()
    {
        foreach(SequenceEventChannel channel in eventChannelsWithMetadata.Keys) channel.OnSequenceEvent -= (data) => OnSequenceEvent(channel, data);
    }

    private void OnSequenceEvent(SequenceEventChannel channel, SequenceEventData data)
    {
        EventChannelMetadata metadata = eventChannelsWithMetadata[channel];
        if(SpamProtectionCheck(metadata)) return;

        List<SequenceCacheEntry> sequenceCacheEntries = sequenceCache.FindAll(entry =>
            entry.eventType == data.eventType &&
            entry.chatUser == data.chatUser &&
            (entry.dayNumberTillRelevant == 0 || entry.dayNumberTillRelevant >= GameManager.Instance.CurrentDayNumber)
        );

        if(sequenceCacheEntries.Count <= 0) return;
        ChatBubbleSequence sequence = sequenceCacheEntries[UnityEngine.Random.Range(0, sequenceCacheEntries.Count)].sequence;
        // TODO: this runs in the overloaded chatlog, not the supervisor
        data.chatLogController.RunBubbleSequence(sequence, Constants.ChatBubbleSequenceType.Simple);
    }

    // NOTE: this could later be expanded to handle checks based on event types also
    //       (use a switch(eventType) instead of generally handling metadata)
    private bool SpamProtectionCheck(EventChannelMetadata metadata)
    {
        if(metadata.isOnCooldown) return true;
        metadata.cooldownCoroutine = StartCoroutine(CooldownCoroutine(metadata));
        return false;
    }

    private IEnumerator CooldownCoroutine(EventChannelMetadata metadata)
    {
        metadata.isOnCooldown = true;
        yield return new WaitForSeconds(metadata.cooldownDurationInSeconds);
        metadata.isOnCooldown = false;
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

    #region Event Channel Metadata
    // NOTE: this could later be expanded if we want more nuanced checks, like cooldowns in days,
    //       continuity in response text, or anything more complex
    private class EventChannelMetadata
    {
        // NOTE: every time a new SequenceEventChannel type (Constants.SequenceEventChannels) is added,
        //       a new entry should be added here
        public static List<EventChannelMetadata> metadata = new List<EventChannelMetadata>
        {
            new EventChannelMetadata(Constants.SequenceEventType.MarkerOverload, 10f),
            new EventChannelMetadata(Constants.SequenceEventType.Default, 60f)
        };

        public Constants.SequenceEventType eventType;
        public float cooldownDurationInSeconds;
        public bool isOnCooldown;
        public Coroutine cooldownCoroutine;

        public EventChannelMetadata(Constants.SequenceEventType eventType, float cooldownDurationInSeconds)
        {
            this.eventType = eventType;
            this.cooldownDurationInSeconds = cooldownDurationInSeconds;
            isOnCooldown = false;
            cooldownCoroutine = null;
        }
    }
    #endregion
}
