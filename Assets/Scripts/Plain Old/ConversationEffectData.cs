[System.Serializable]
public class ConversationEffectData
{
    // NOTE: no application logic lives here — effects are applied by the conversation manager
    public ConversationEffectOperation operation;
    public string stringValue;
    public int intValue;
}
