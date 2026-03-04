using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogUI : MonoBehaviour
{
    private ChatLogController chatLogController;
    private PointerHandler closePointerHandler;

    private bool isSetUp = false;

    public void Setup(ChatLog chatLog)
    {
        if(isSetUp || !FindElements()) return;

        closePointerHandler.OnPointerClickEvent += chatLogController.Close;

        Transform content = GetComponentInChildren<ContentSizeFitter>().transform;
        chatLogController.Setup(content, chatLog);

        isSetUp = true;
    }

    private bool FindElements()
    {
        chatLogController = GetComponent<ChatLogController>();
        Transform topBar = transform.Find(Constants.GameObjectNames.TopBar);
        topBar.AddComponent<DragHandler>().objectToDrag = transform;
        closePointerHandler = topBar.Find(Constants.GameObjectNames.CloseButton).AddComponent<PointerHandler>();

        if(chatLogController  == null)
        {
            Debug.LogError("Setup of ChatLog failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if(!closePointerHandler || !chatLogController)
        {
            closePointerHandler.OnPointerUpEvent -= chatLogController.Close;
        }
    }
}
