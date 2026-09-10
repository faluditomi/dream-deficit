using System.Collections.Generic;

// NOTE: run-level chat state — JsonUtility round-trip safe. History stores identity
//       (graph asset name + node GUID), never content copies, so this file
//       holds no asset references and no dictionaries.
[System.Serializable]
public class PlayedBubbleRecord
{
    public string graphName;
    public string nodeGuid;
    // when the record was produced by a player choice reply, the id of the chosen
    // option — the bubble content lives on the option, not on the choice node itself
    public string optionGuid;
}

[System.Serializable]
public class ConversationThreadState
{
    public string graphName;
    public string nodeGuid;
    public ConversationThreadStateKind stateKind;
}

// One-shot entry activation guard (run-level, per log). Flat JsonUtility-safe record
// keyed by current day + event type + graph name + entry node GUID. A non-repeatable
// Entry node activated for a given event on a given day must not be queued again when
// the same event fires again that day.
[System.Serializable]
public class ConversationEntryActivation
{
    public string graphName;
    public string nodeGuid;
    public string eventType;
    public int dayNumber;
}

[System.Serializable]
public class LogChatState
{
    public string logName;
    public List<PlayedBubbleRecord> history = new List<PlayedBubbleRecord>();
    public List<ConversationThreadState> threads = new List<ConversationThreadState>();
    public List<ConversationEntryActivation> activations = new List<ConversationEntryActivation>();
}

[System.Serializable]
public class ChatRunState
{
    public List<string> flags = new List<string>();
    public List<LogChatState> logs = new List<LogChatState>();
}
