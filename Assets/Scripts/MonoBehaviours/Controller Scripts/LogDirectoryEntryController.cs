using UnityEngine;

public class LogDirectoryEntryController : MonoBehaviour
{
    private GameObject chatLogPrefab;
    private Transform uiCanvas;

    private async void Start()
    {
        chatLogPrefab = await AddressableController.Instance().RetrieveAddressable<GameObject>(Constants.Addressable.ChatLog);
        uiCanvas = FindFirstObjectByType<Canvas>().transform;
    }

    // TODO: this is a general todo, but combine the UI and Controller scripts into one... it's getting confusing af
    public void OpenLog(ChatLog chatLog)
    {
        if(chatLog.isOpen) return;

        ChatLogUI chatLogUI = Instantiate(chatLogPrefab, uiCanvas).GetComponent<ChatLogUI>();
        chatLogUI.Setup(chatLog);
    }
}
