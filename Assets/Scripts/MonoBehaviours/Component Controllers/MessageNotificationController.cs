using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageNotificationController : MonoBehaviour
{
    private ChatLogController myChatLogController;
    private TextMeshProUGUI newMessageCounter;
    private Image notificationBackground;
    private int newMessageCount;

    private void Start()
    {
        // TODO: once we need to add message notificaitons to more than just the supervisor, we'll have to get this in a more generic way
            // TODO: generalise the chat log creation/handling part of scripts like SupervisorController and LogDirectoryEntryController into an abstract class
            //       that those types of scripts can inherit from and this script can reference as .GetComponentInParent<BaseChatLogInitialiser>().myChatLogController
        myChatLogController = GameManager.Instance.GetSupervisorController().chatLogController;
        newMessageCounter = GetComponentInChildren<TextMeshProUGUI>();
        notificationBackground = GetComponentInChildren<Image>();

        if(myChatLogController != null)
        {
            myChatLogController.OnNewMessage += UpdateNotification;
            myChatLogController.OnGainedFocusEvent += ResetNotification;
        }

        notificationBackground.enabled = false;
        newMessageCounter.gameObject.SetActive(false);
        newMessageCount = 0;
        newMessageCounter.text = newMessageCount.ToString();
    }

    private void OnDestroy()
    {
        if(myChatLogController != null) myChatLogController.OnNewMessage -= UpdateNotification;
    }

    private void UpdateNotification(string newMessage)
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
