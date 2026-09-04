using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// for guidance on how to hook up notifications to a chat log, see the ChatClientUserEntry -> MessageNotification objects
// just take the MessageNotification prefab and place it as a child of the button that opens your chat log
public class MessageNotificationController : MonoBehaviour
{
    [HideInInspector] public List<ChatLogController> myChatLogControllers;
    private TextMeshProUGUI unreadMessageCounter;
    private Image notificationBackground;
    private int unreadMessageCount = 0;

    // We can provide multiple chat log controllers, in case we want the notification counter to keep track of more logs at once.
    public void Setup(List<ChatLogController> chatLogControllers)
    {
        unreadMessageCounter = GetComponentInChildren<TextMeshProUGUI>();
        notificationBackground = GetComponentInChildren<Image>();
        myChatLogControllers = chatLogControllers.Count > 0 ? chatLogControllers : new List<ChatLogController>();

        foreach(ChatLogController chatLogController in myChatLogControllers)
        {
            chatLogController.OnNewMessageEvent += message => AddNotification(message, chatLogController);
            chatLogController.OnGainedFocusEvent += SubtractNotification;
        }

        notificationBackground.enabled = false;
        unreadMessageCounter.gameObject.SetActive(false);
        unreadMessageCounter.text = unreadMessageCount.ToString();
    }

    private void OnDestroy()
    {
        if(myChatLogControllers.Count <= 0) return;
        myChatLogControllers.ForEach(chatLogController => chatLogController.OnNewMessageEvent -= message => AddNotification(message, chatLogController));
        myChatLogControllers.ForEach(chatLogController => chatLogController.OnGainedFocusEvent -= SubtractNotification);
    }

    private void AddNotification(string message, ChatLogController chatLogController)
    {
        if(ChatLogManager.Instance.IsChatLogInFocus(chatLogController)) return;

        if(!unreadMessageCounter.gameObject.activeSelf)
        {
            notificationBackground.enabled = true;
            unreadMessageCounter.gameObject.SetActive(true);
        }

        unreadMessageCount++;
        unreadMessageCounter.text = unreadMessageCount.ToString();
    }

    private void SubtractNotification(GameObject focusedWindow, int unreadMessages)
    {
        unreadMessageCount -= unreadMessages;
        unreadMessageCounter.text = unreadMessageCount.ToString();

        if(unreadMessageCount == 0)
        {
            notificationBackground.enabled = false;
            unreadMessageCounter.gameObject.SetActive(false);
        }
    }
}
