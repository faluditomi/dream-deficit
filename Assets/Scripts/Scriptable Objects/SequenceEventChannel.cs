using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SequenceEventChannel", menuName = "Scriptable Objects/SequenceEventChannel")]
public class SequenceEventChannel : ScriptableObject
{
    public Constants.SequenceEventType eventType;
    public event Action<Constants.SequenceEventType> OnSequenceEvent;

    /// A plain event bus: raising the channel broadcasts the event type once.
    /// The conversation manager's runners evaluate their own entries for it.
    public void Raise()
    {
        OnSequenceEvent?.Invoke(eventType);
    }

    private void OnDisable() => OnSequenceEvent = null;
}
