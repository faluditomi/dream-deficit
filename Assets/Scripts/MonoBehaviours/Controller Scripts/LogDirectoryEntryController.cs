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

    // TODO: start tracking whether a log is open, and only open it if its not already open
    // TODO: this is a general todo, but combine the UI and Controller scripts into one... it's getting confusing af
    public void OpenLog(ChatLog chatLog)
    {
        ChatLogUI chatLogUI = Instantiate(chatLogPrefab, uiCanvas).GetComponent<ChatLogUI>();
        chatLogUI.Setup(chatLog);
    }
}
