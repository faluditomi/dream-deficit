using System.Collections.Generic;

[System.Serializable]
public class ConversationNodeData
{
    // unique per node — assigned by editor tooling at creation
    public string guid;
    public ConversationNodeKind kind;
    // used by Bubble nodes — full ChatBubble carries chatUser/message/delay/typingLength/markables
    public ChatBubble bubble = new ChatBubble();
    // used by Choice nodes — one entry per player draft
    public List<ConversationChoiceOptionData> options = new List<ConversationChoiceOptionData>();
    // used by Choice nodes — while a day-blocking choice is unresolved the day does not end
    public bool blocksDay;
    // used by Entry and Wait nodes — empty list means always eligible
    public List<ConversationConditionClause> conditions = new List<ConversationConditionClause>();
    // used by Entry nodes — true means the entry participates in exclusive-group resolution
    public bool exclusive;
    // used by Entry nodes — when exclusive is false the entry is additive and the group id is ignored
    public string exclusiveGroupId;
    // used by Entry nodes — true means the entry may activate every time its event fires;
    // false (default) means it activates at most once per event type per day
    public bool isRepeatable;
    // canvas position for the graph editor (editor-only usage, serialized with the asset)
    public UnityEngine.Vector2 editorPosition;
}

[System.Serializable]
public class ConversationChoiceOptionData
{
    // its own GUID so choice edges survive option reordering (design D3)
    public string guid;
    // what the player sees on the draft
    public string previewText;
    // the bubble actually posted when picked — chatUser should be the player's
    public ChatBubble postedBubble = new ChatBubble();
    public List<ConversationEffectData> effects = new List<ConversationEffectData>();
}

[System.Serializable]
public class ConversationEdgeData
{
    public string fromNodeGuid;
    // empty when the source is not a choice option
    public string fromOptionGuid;
    public string toNodeGuid;
}
