using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChatClientUserEntryController : MonoBehaviour
{
    private Image profilePictureImage;
    private TMP_Text usernameText;
    private TMP_Text lastMessageText;
    private ChatClientController chatClientController;
    public ChatLogController myChatLogController { get; private set; }
    public ChatUser myChatUser { get; private set; }
    public bool isUnopened { get; private set; } = false;

    private Button entryButton;
    private UnityAction onClickAction;
    private Action<string> onNewMessageHandler;
    private Action<GameObject, int> onGainedFocusHandler;

    public void Setup(ChatUser chatUser, ChatLog chatLog, string lastMessage, Transform container)
    {
        chatClientController = FindFirstObjectByType<ChatClientController>();
        profilePictureImage = transform.Find(Constants.GameObjectNames.ProfilePicture).GetComponent<Image>();
        usernameText = transform.Find(Constants.GameObjectNames.Username).GetComponent<TMP_Text>();
        lastMessageText = transform.Find(Constants.GameObjectNames.LastMessage).GetComponent<TMP_Text>();
        myChatUser = chatUser;
        profilePictureImage.sprite = myChatUser.profilePicture;
        usernameText.text = myChatUser.username;
        lastMessageText.text = lastMessage;
        usernameText.gameObject.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        lastMessageText.gameObject.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        myChatLogController = ChatLogManager.Instance.InstantiateChatLog(chatLog, transform, false, container);
        entryButton = GetComponent<Button>();
        onClickAction = () => {
            chatClientController.myChatLogController = myChatLogController;
            myChatLogController.Open();
        };
        entryButton.onClick.AddListener(onClickAction);
        onNewMessageHandler = message => {
            UpdateMessagePreview(message);
            transform.SetAsFirstSibling();
        };
        onGainedFocusHandler = OpenMessagePreview;
        myChatLogController.OnNewMessageEvent += onNewMessageHandler;
        myChatLogController.OnGainedFocusEvent += onGainedFocusHandler;
    }

    private void OnDestroy()
    {
        if(entryButton != null && onClickAction != null) entryButton.onClick.RemoveListener(onClickAction);

        if(myChatLogController != null)
        {
            if(onNewMessageHandler != null) myChatLogController.OnNewMessageEvent -= onNewMessageHandler;
            if(onGainedFocusHandler != null) myChatLogController.OnGainedFocusEvent -= onGainedFocusHandler;
        }
    }

    private void UpdateMessagePreview(string message)
    {
        isUnopened = true;
        lastMessageText.text = message;

        if(!ChatLogManager.Instance.IsChatLogInFocus(myChatLogController))
        {
            lastMessageText.fontStyle = FontStyles.Bold;
            lastMessageText.color = Color.white;
        }
    }

    public void MarkCurrentMessageAsUnopened()
    {
        isUnopened = true;
        lastMessageText.fontStyle = FontStyles.Bold;
        lastMessageText.color = Color.white;
    }

    private void OpenMessagePreview(GameObject focusedWindow, int unreadMessages)
    {
        isUnopened = false;
        lastMessageText.fontStyle = FontStyles.Normal;
        lastMessageText.color = Color.gray;
    }
}
