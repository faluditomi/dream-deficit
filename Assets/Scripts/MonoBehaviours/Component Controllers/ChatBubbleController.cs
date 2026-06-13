using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatBubbleController : MonoBehaviour
{
    private ChatBubble myChatBubble;
    private Image profilePictureImage;
    private TMP_Text usernameText;
    private TMP_Text messageText;

    private bool isSetUp = false;

    #region Setup
    public void Setup(ChatBubble chatBubble, ChatLog chatlog)
    {
        if(isSetUp || !FindElements()) return;

        myChatBubble = chatBubble;
        // NOTE: Depending on how large and numerous our chat logs will be, these per bubble addressable calls might get expensive
        string address = Constants.AddressablePrefixes.ChatUser + myChatBubble.chatUser.ToString().ToLower();
        ChatUser chatUser = AddressableManager.Instance.RetrieveAddressable<ChatUser>(address);

        if(chatUser == null) Debug.LogError($"ChatUser Addressable wasn't found for ChatUser: {myChatBubble.chatUser}. Setup of ChatBubble failed.");

        profilePictureImage.sprite = chatUser.profilePicture;
        usernameText.text = chatUser.username;
        messageText.text = myChatBubble.message;

        usernameText.AddComponent<HighlightHandler>().Setup(chatlog, myChatBubble, true);
        messageText.AddComponent<HighlightHandler>().Setup(chatlog, myChatBubble, true);

        isSetUp = true;
    }

    private bool FindElements()
    {
        Transform profilePicture = transform.Find(Constants.GameObjectNames.ProfilePicture);
        profilePictureImage = profilePicture.GetComponent<Image>();
        Transform username = transform.Find(Constants.GameObjectNames.Username);
        usernameText = username.GetComponent<TMP_Text>();
        messageText = transform.Find(Constants.GameObjectNames.Message).GetComponent<TMP_Text>();

        if(!profilePicture || !profilePictureImage || !usernameText || !messageText)
        {
            Debug.LogError("Setup of ChatBubble failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
    #endregion
}
