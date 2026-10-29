using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// for guidance on how to hook up notifications to a chat log, see the ChatClientUserEntry -> MessageNotification objects
// just take the MessageNotification prefab and place it as a child of the button that opens your chat log
public class MessageNotificationController : MonoBehaviour
{
    private List<ChatLogController> myChatLogControllers;
    private TextMeshProUGUI unreadMessageCounter;
    private Image notificationBackground;
    private Dictionary<ChatLogController, ChatLogSubscription> subscriptions = new Dictionary<ChatLogController, ChatLogSubscription>();
    public int unreadMessageCount { get; private set; } = 0;

    private sealed class ChatLogSubscription
    {
        public System.Action<string> onNewMessage;
        public System.Action<GameObject, int> onGainedFocus;
    }

    // We can provide multiple chat log controllers, in case we want the notification counter to keep track of more logs at once.
    public void Setup(List<ChatLogController> chatLogControllers)
    {
        UnsubscribeAll();
        myChatLogControllers = chatLogControllers != null && chatLogControllers.Count > 0
            ? new List<ChatLogController>(chatLogControllers)
            : new List<ChatLogController>();
        // include inactive: Setup hides these children, and a later Setup on the same instance must find them again
        unreadMessageCounter = GetComponentInChildren<TextMeshProUGUI>(true);
        notificationBackground = GetComponentInChildren<Image>(true);
        if(unreadMessageCounter == null || notificationBackground == null) return;

        foreach(ChatLogController chatLogController in myChatLogControllers)
        {
            if(chatLogController == null) continue;
            if(subscriptions.ContainsKey(chatLogController)) continue;

            ChatLogSubscription subscription = new ChatLogSubscription
            {
                onNewMessage = message => AddNotification(message, chatLogController),
                onGainedFocus = SubtractNotification
            };

            subscriptions.Add(chatLogController, subscription);
            chatLogController.OnNewMessageEvent += subscription.onNewMessage;
            chatLogController.OnGainedFocusEvent += subscription.onGainedFocus;
        }

        notificationBackground.enabled = false;
        unreadMessageCounter.gameObject.SetActive(false);
        unreadMessageCounter.text = unreadMessageCount.ToString();
    }

    private void UnsubscribeAll()
    {
        foreach(KeyValuePair<ChatLogController, ChatLogSubscription> entry in subscriptions)
        {
            ChatLogController chatLogController = entry.Key;
            if(chatLogController == null) continue;
            ChatLogSubscription subscription = entry.Value;
            chatLogController.OnNewMessageEvent -= subscription.onNewMessage;
            chatLogController.OnGainedFocusEvent -= subscription.onGainedFocus;
        }

        subscriptions.Clear();
    }

    private void OnDestroy()
    {
        UnsubscribeAll();
    }

    private void AddNotification(string message, ChatLogController chatLogController)
    {
        if(ChatLogManager.Instance.IsChatLogInFocus(chatLogController)) return;
        unreadMessageCount++;

        if(!unreadMessageCounter.gameObject.activeSelf)
        {
            notificationBackground.enabled = true;
            unreadMessageCounter.gameObject.SetActive(true);
        }

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

    public void SetNotificationCount(int notificationCount)
    {
        unreadMessageCount = notificationCount;
        unreadMessageCounter.text = unreadMessageCount.ToString();

        if(unreadMessageCount <= 0)
        {
            notificationBackground.enabled = false;
            unreadMessageCounter.gameObject.SetActive(false);
        }
        else if(!unreadMessageCounter.gameObject.activeSelf)
        {
            notificationBackground.enabled = true;
            unreadMessageCounter.gameObject.SetActive(true);
        }
    }
}
