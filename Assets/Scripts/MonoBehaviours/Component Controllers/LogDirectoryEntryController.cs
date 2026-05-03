using TMPro;
using UnityEngine;

public class LogDirectoryEntryController : MonoBehaviour
{
    private ChatLog chatLog;
    private GameObject chatLogPrefab;
    private Transform uiCanvas;
    private PointerHandler entryPointerHandler;
    private ChatLogController chatLogController;

    private TMP_Text logNameText;

    private bool isSetUp = false;

    #region Setup
    public async void Setup(ChatLog chatLog)
    {
        if(isSetUp || !FindElements()) return;
        this.chatLog = chatLog;
        logNameText.text = chatLog.logName;
        chatLogPrefab = await AddressableManager.Instance().RetrieveAddressable<GameObject>(Constants.AddressablePaths.ChatLogPrefab);
        entryPointerHandler.OnPointerClickEvent += (eventData) => OpenLog(chatLog);
        isSetUp = true;
    }

    private bool FindElements()
    {
        uiCanvas = FindFirstObjectByType<Canvas>().transform;
        logNameText = transform.Find(Constants.GameObjectNames.LogName).GetComponent<TMP_Text>();
        entryPointerHandler = gameObject.AddComponent<PointerHandler>();
        
        if(uiCanvas == null || logNameText == null)
        {
            Debug.LogError("Setup of LogDirectoryEntry failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
    #endregion
    
    public void OpenLog(ChatLog chatLog)
    {
        if(chatLogController == null)
        {
            chatLogController = Instantiate(chatLogPrefab, uiCanvas).GetComponent<ChatLogController>();
            chatLogController.Setup(chatLog);
        }

        if(!chatLogController.isOpen)
        {
            chatLogController.Open();
        }

    }

    private void OnDestroy()
    {
        if(entryPointerHandler)
        {
            entryPointerHandler.OnPointerClickEvent -= (eventData) => OpenLog(chatLog);
        }
    }
}
