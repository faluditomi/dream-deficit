using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatClientUserEntryController : MonoBehaviour
{
    private Image profilePictureImage;
    private TMP_Text usernameText;
    private TMP_Text lastMessageText;
    private ChatClientController chatClientController;
    private ChatLogController myChatLogController;
    public ChatUser myChatUser { get; private set; }
    public bool isUnopened { get; private set; } = false;

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

        GetComponent<Button>().onClick.AddListener(() => {
            chatClientController.myChatLogController = myChatLogController;
            myChatLogController.Open();
        });

        myChatLogController.OnNewMessageEvent += (message) => {
            UpdateMessagePreview(message);
            transform.SetAsFirstSibling();
        };

        myChatLogController.OnGainedFocusEvent += OpenMessagePreview;
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
