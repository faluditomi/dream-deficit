using TMPro;
using UnityEngine;
using UnityEngine.UI;

// for guidance on how to hook up notifications to a chat log, see the ChatClientUserEntry -> MessageNotification objects
// just take the MessageNotification prefab and place it as a child of the button that opens your chat log
public class MessageNotificationController : MonoBehaviour
{
    [HideInInspector] public ChatLogController myChatLogController;
    private ChatClientController chatClientController;
    private TextMeshProUGUI newMessageCounter;
    private Image notificationBackground;
    private int newMessageCount;

    // TODO: if it's the chat client, take all the chat log controller under chat client controller and subscribe with all of them
    public void Setup(ChatLogController chatLogController, bool isChatClient = false)
    {
        chatClientController = FindFirstObjectByType<ChatClientController>();
        newMessageCounter = GetComponentInChildren<TextMeshProUGUI>();
        notificationBackground = GetComponentInChildren<Image>();
        myChatLogController = chatLogController;
        myChatLogController.OnNewMessageEvent += UpdateNotification;
        myChatLogController.OnGainedFocusEvent += ResetNotification;
        notificationBackground.enabled = false;
        newMessageCounter.gameObject.SetActive(false);
        newMessageCount = 0;
        newMessageCounter.text = newMessageCount.ToString();
    }

    private void OnDestroy()
    {
        if(myChatLogController != null) myChatLogController.OnNewMessageEvent -= UpdateNotification;
    }

    private void UpdateNotification(string message)
    {
        if(UIFocusManager.Instance.focusedWindow != myChatLogController && chatClientController.myChatLogController != myChatLogController)
        {
            if(!newMessageCounter.gameObject.activeSelf)
            {
                notificationBackground.enabled = true;
                newMessageCounter.gameObject.SetActive(true);
            }
            newMessageCount++;
            newMessageCounter.text = newMessageCount.ToString();
        }
    }

    private void ResetNotification(GameObject focusedWindow)
    {
        notificationBackground.enabled = false;
        newMessageCounter.gameObject.SetActive(false);
        newMessageCount = 0;
        newMessageCounter.text = newMessageCount.ToString();
    }
}
