using TMPro;
using UnityEngine;

public class LogDirectoryEntryUI : MonoBehaviour
{
    private LogDirectoryEntryController logDirectoryEntryController;
    private PointerHandler entryPointerHandler;

    private TMP_Text logNameText;

    private bool isSetUp = false;

    public void Setup(ChatLog chatLog)
    {
        if(isSetUp || !FindElements()) return;

        chatLog.isOpen = false;
        logNameText.text = chatLog.logName;

        entryPointerHandler.OnPointerClickEvent += (eventData) => logDirectoryEntryController.OpenLog(chatLog);

        isSetUp = true;
    }

    private bool FindElements()
    {
        logDirectoryEntryController = GetComponent<LogDirectoryEntryController>();
        logNameText = transform.Find(Constants.GameObjectNames.LogName).GetComponent<TMP_Text>();
        entryPointerHandler = gameObject.AddComponent<PointerHandler>();
        
        if(logDirectoryEntryController  == null || logNameText == null)
        {
            Debug.LogError("Setup of LogDirectoryEntry failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
}
