using TMPro;
using UnityEngine;
using UnityEngine.UI;

// for guidance on how to hook up notifications to a chat log, see the SupervisorButton -> MessageNotificaiton objects
// just take the MessageNotification prefab and place it as a child of the button that opens your chat log
public class MessageNotificationController : MonoBehaviour
{
    [HideInInspector] public ChatLogController myChatLogController;
    private TextMeshProUGUI newMessageCounter;
    private Image notificationBackground;
    private int newMessageCount;

    private void Start()
    {
        newMessageCounter = GetComponentInChildren<TextMeshProUGUI>();
        notificationBackground = GetComponentInChildren<Image>();

        if(myChatLogController != null)
        {
            myChatLogController.OnNewMessageEvent += UpdateNotification;
            myChatLogController.OnGainedFocusEvent += ResetNotification;
        }

        notificationBackground.enabled = false;
        newMessageCounter.gameObject.SetActive(false);
        newMessageCount = 0;
        newMessageCounter.text = newMessageCount.ToString();
    }

    private void OnDestroy()
    {
        if(myChatLogController != null) myChatLogController.OnNewMessageEvent -= UpdateNotification;
    }

    private void UpdateNotification()
    {
        if(UIFocusManager.Instance.FocusedWindow != myChatLogController)
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
