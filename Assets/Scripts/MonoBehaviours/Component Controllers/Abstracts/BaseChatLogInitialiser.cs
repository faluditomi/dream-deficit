using UnityEngine;
using UnityEngine.UI;

public abstract class BaseChatLogInitialiser : MonoBehaviour
{
    protected GameObject chatLogPrefab;
    [HideInInspector] public ChatLogController myChatLogController;

    public virtual void Setup(ChatLog chatLog)
    {
        chatLogPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.ChatLog);
        GetComponent<Button>().onClick.AddListener(() => OpenChatLog());

        Transform windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
        myChatLogController = Instantiate(chatLogPrefab, windowContainer).GetComponent<ChatLogController>();
        myChatLogController.Setup(chatLog);
        myChatLogController.GetComponent<TopBarHandler>().Close();
    }

    private void OpenChatLog()
    {
        if(!myChatLogController.GetIsOpen())
        {
            myChatLogController.Open();
        }
    }
}
