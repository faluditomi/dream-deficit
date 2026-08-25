using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChatClientUserEntryController : MonoBehaviour
{
    private Image profilePictureImage;
    private TMP_Text usernameText;
    private TMP_Text lastMessageText;
    private ChatLogController myChatLogController;

    public void Setup(ChatUser chatUser, ChatLog chatLog, string lastMessage, Transform container)
    {
        profilePictureImage = transform.Find(Constants.GameObjectNames.ProfilePicture).GetComponent<Image>();
        usernameText = transform.Find(Constants.GameObjectNames.Username).GetComponent<TMP_Text>();
        lastMessageText = transform.Find(Constants.GameObjectNames.LastMessage).GetComponent<TMP_Text>();
        profilePictureImage.sprite = chatUser.profilePicture;
        usernameText.text = chatUser.username;
        lastMessageText.text = lastMessage;
        usernameText.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        lastMessageText.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        myChatLogController = ChatLogManager.Instance.InstantiateChatLog(chatLog, transform, false, container);
        GetComponent<Button>().onClick.AddListener(OnEntryClicked);
    }

    private void OnEntryClicked()
    {
        // TODO: open the chat log similar to how the AssignmentEntryController does
    }
}
