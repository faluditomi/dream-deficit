using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatClientUserEntryController : MonoBehaviour
{
    private Image profilePictureImage;
    private TMP_Text usernameText;
    private TMP_Text lastMessageText;
    private ChatLogController myChatLogController;
    private ChatClientController chatClientController;

    public void Setup(ChatUser chatUser, ChatLog chatLog, string lastMessage, Transform container)
    {
        chatClientController = FindFirstObjectByType<ChatClientController>();
        profilePictureImage = transform.Find(Constants.GameObjectNames.ProfilePicture).GetComponent<Image>();
        usernameText = transform.Find(Constants.GameObjectNames.Username).GetComponent<TMP_Text>();
        lastMessageText = transform.Find(Constants.GameObjectNames.LastMessage).GetComponent<TMP_Text>();
        profilePictureImage.sprite = chatUser.profilePicture;
        usernameText.text = chatUser.username;
        lastMessageText.text = lastMessage;
        usernameText.gameObject.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        lastMessageText.gameObject.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        myChatLogController = ChatLogManager.Instance.InstantiateChatLog(chatLog, transform, false, container);

        GetComponent<Button>().onClick.AddListener(() => {
            chatClientController.myChatLogController = myChatLogController;
            myChatLogController.Open();
        });

        myChatLogController.OnNewMessageEvent += UpdateMessagePreview;
        myChatLogController.OnGainedFocusEvent += OpenMessagePreview;
    }

    private void UpdateMessagePreview(string message)
    {
        lastMessageText.text = message;

        if(UIFocusManager.Instance.focusedWindow != myChatLogController && chatClientController.myChatLogController != myChatLogController)
        {
            lastMessageText.fontStyle = FontStyles.Bold;
            lastMessageText.color = Color.white;
        }
    }

    private void OpenMessagePreview(GameObject focusedWindow, int unreadMessages)
    {
        lastMessageText.fontStyle = FontStyles.Normal;
        lastMessageText.color = Color.gray;
    }
}
