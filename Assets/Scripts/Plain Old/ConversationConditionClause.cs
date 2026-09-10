using System.Collections.Generic;

[System.Serializable]
public class ConversationConditionClause
{
    public ConversationConditionKind kind;
    public string stringValue = string.Empty;
    public int intValue;

    // NOTE: an empty/null conditions list is always true — that is evaluated by the runner, not per-clause
    public bool Matches(Constants.SequenceEventType? triggeredEvent, int currentDay, List<string> flags)
    {
        switch(kind)
        {
            case ConversationConditionKind.Event:
                return triggeredEvent.HasValue &&
                    string.Equals(triggeredEvent.Value.ToString(), stringValue, System.StringComparison.OrdinalIgnoreCase);
            case ConversationConditionKind.DayMin:
                return currentDay >= intValue;
            case ConversationConditionKind.DayMax:
                return currentDay <= intValue;
            case ConversationConditionKind.RequiredFlag:
                return flags != null && flags.Contains(stringValue);
            default:
                return false;
        }
    }
}
