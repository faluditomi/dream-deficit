using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// NOTE: When creating a ChatBubbleSequence that we want the auto-sequencer to pick up automatically, we have to name it like:
///       "event_sequence_" + {SequenceEventType} + "_" + {chatLogName} + "_" + {chatBubbleSequenceType} + "_" + {variationNumber}
///       The variation number and the underscore prefix can be ommited.
public class SequenceEventManager : Singleton<SequenceEventManager>
{
    private Dictionary<SequenceEventChannel, EventChannelMetadata> eventChannelsWithMetadata = new();

    protected override void Awake()
    {
        base.Awake();
        List<SequenceEventChannel> eventChannels = AddressableManager.Instance
            .RetrieveAddressablesByLabel<SequenceEventChannel>(Constants.AddressableLabels.EventChannel);

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
        if(metadata != null && SpamProtectionCheck(metadata)) return;
        ChatBubbleSequence sequence = AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(Constants.AddressablePrefixes.ChatBubbleSequence + data.chatBubbleSequenceName);
        data.chatLogController.RunBubbleSequence(sequence, data.chatBubbleSequenceType);
    }

    // NOTE: this could later be expanded to handle checks based on event types also
    //       (use a switch(eventType) instead of generally handling metadata)
    private bool SpamProtectionCheck(EventChannelMetadata metadata)
    {
        if(metadata.cooldownDurationInSeconds == 0f) return false;
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
            new EventChannelMetadata(Constants.SequenceEventType.DayStart, 0f),
            new EventChannelMetadata(Constants.SequenceEventType.DayEnd, 0f),
            new EventChannelMetadata(Constants.SequenceEventType.Default, 0f)
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
