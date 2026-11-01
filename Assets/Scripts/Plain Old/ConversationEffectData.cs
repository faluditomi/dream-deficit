[System.Serializable]
public class ConversationEffectData
{
    // NOTE: no application logic lives here — effects are applied by the conversation manager
    public ConversationEffectOperation operation;
    public string stringValue;
    public int intValue;
    // seconds to wait after the triggering bubble posts / choice is picked before this effect fires
    [UnityEngine.Tooltip("Seconds to wait after the triggering bubble posts or choice is picked before this effect fires. 0 = fire immediately.")]
    public float delaySeconds;
}
