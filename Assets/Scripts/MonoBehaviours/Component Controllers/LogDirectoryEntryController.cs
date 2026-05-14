using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogDirectoryEntryController : MonoBehaviour
{
    private GameObject chatLogPrefab;
    private ChatLogController chatLogController;
    private TMP_Text logNameText;

    private bool isSetUp = false;

    #region Setup
    public async void Setup(ChatLog chatLog)
    {
        if(isSetUp || !FindElements()) return;
        logNameText.text = chatLog.logName;
        chatLogPrefab = await AddressableManager.Instance().RetrieveAddressable<GameObject>(Constants.AddressablePaths.ChatLogPrefab);
        GetComponent<Button>().onClick.AddListener(() => OpenChatLog(chatLog));
        isSetUp = true;
    }

    private bool FindElements()
    {
        logNameText = transform.Find(Constants.GameObjectNames.Name).GetComponent<TMP_Text>();
        
        if(logNameText == null)
        {
            Debug.LogError("Setup of LogDirectoryEntry failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
    #endregion
    
    public void OpenChatLog(ChatLog chatLog)
    {
        if(chatLogController == null)
        {
            Transform windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
            chatLogController = Instantiate(chatLogPrefab, windowContainer).GetComponent<ChatLogController>();
            chatLogController.Setup(chatLog);
        }

        if(!chatLogController.GetIsOpen())
        {
            chatLogController.Open();
        }
    }
}
