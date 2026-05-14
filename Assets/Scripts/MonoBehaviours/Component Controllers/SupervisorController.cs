using UnityEngine;
using UnityEngine.UI;

public class SupervisorController : MonoBehaviour
{
    private GameObject chatLogPrefab;
    private ChatLog supervisorChatLog;
    private ChatLogController chatLogController;

    private async void Start()
    {
        chatLogPrefab = await AddressableManager.Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePaths.ChatLogPrefab);
        supervisorChatLog = await AddressableManager.Instance
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.Supervisor);
        GetComponent<Button>().onClick.AddListener(() => OpenChatLog());
    }

    private void OpenChatLog()
    {
        if(chatLogController == null)
        {
            Transform windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
            chatLogController = Instantiate(chatLogPrefab, windowContainer).GetComponent<ChatLogController>();
            chatLogController.Setup(supervisorChatLog);
        }

        if(!chatLogController.GetIsOpen())
        {
            chatLogController.Open();
        }
    }
}
