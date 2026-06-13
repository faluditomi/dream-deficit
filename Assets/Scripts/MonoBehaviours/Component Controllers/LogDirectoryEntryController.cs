using TMPro;

public class LogDirectoryEntryController : BaseChatLogInitialiser
{
    private TMP_Text logNameText;

    public override void Setup(ChatLog chatLog)
    {
        base.Setup(chatLog);
        logNameText = transform.Find(Constants.GameObjectNames.Name).GetComponent<TMP_Text>();
        logNameText.text = chatLog.logName;
    }
}
