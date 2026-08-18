using TMPro;
using UnityEngine;

// TODO: maybe this could be revamped to handle files and folders too (since those will probably only have a name too in code)
public class AssignmentEntryController : MonoBehaviour
{
    private TMP_Text logNameText;

    public void Setup(ChatLog chatLog)
    {
        ChatLogManager.Instance.InstantiateChatLog(chatLog, transform);
        logNameText = transform.Find(Constants.GameObjectNames.Name).GetComponent<TMP_Text>();
        logNameText.text = chatLog.logName;
    }
}
