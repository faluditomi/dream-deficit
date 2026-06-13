using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogDirectoryEntryController : MonoBehaviour
{
    private TMP_Text logNameText;

    public void Setup(ChatLog chatLog)
    {
        ChatLogManager.Instance.InstantiateChatLog(chatLog, transform);
        logNameText = transform.Find(Constants.GameObjectNames.Name).GetComponent<TMP_Text>();
        logNameText.text = chatLog.logName;
    }
}
