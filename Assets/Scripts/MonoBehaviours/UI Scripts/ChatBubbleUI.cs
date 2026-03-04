using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubbleUI : MonoBehaviour
{
    private ChatBubbleController chatBubbleController;
    private PointerHandler bubblePointerHandler;
    // private PointerHandler profilePicturePointerHandler;
    // private PointerHandler usernamePointerHandler;

    private Image profilePictureImage;
    private TMP_Text usernameText;
    private TMP_Text messageText;

    private bool isSetUp = false;

    public async void Setup(ChatBubble chatBubble)
    {
        if(isSetUp || !FindElements()) return;

        string address = Constants.Addressable.ChatUserPrefix + chatBubble.chatUserId.ToString().ToLower();
        ChatUser chatUser = await AddressableController.Instance().RetrieveAddressable<ChatUser>(address);

        if(chatUser == null) Debug.LogError($"ChatUser Addressable wasn't found for ChatUser: {chatBubble.chatUserId}. Setup of ChatBubble failed.");

        profilePictureImage = chatUser.profilePicture;
        usernameText.text = chatUser.username;
        messageText.text = chatBubble.message;

        bubblePointerHandler.OnPointerUpEvent += chatBubbleController.ReleaseBubble;
        bubblePointerHandler.OnPointerDownEvent += chatBubbleController.PressBubble;

        isSetUp = true;
    }

    private bool FindElements()
    {
        chatBubbleController = GetComponent<ChatBubbleController>();
        Transform profilePicture = transform.Find(Constants.GameObjectNames.ProfilePicture);
        profilePictureImage = profilePicture.GetComponent<Image>();
        // profilePicturePointerHandler = profilePicture.AddComponent<PointerHandler>();
        Transform username = transform.Find(Constants.GameObjectNames.Username);
        usernameText = username.GetComponent<TMP_Text>();
        // usernamePointerHandler = username.AddComponent<PointerHandler>();
        messageText = transform.Find(Constants.GameObjectNames.Message).GetComponent<TMP_Text>();
        bubblePointerHandler = transform.Find(Constants.GameObjectNames.Bubble).AddComponent<PointerHandler>();

        if(!profilePicture || !profilePictureImage || !usernameText || !messageText)
        {
            Debug.LogError("Setup of ChatBubble failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if(bubblePointerHandler || chatBubbleController)
        {
            bubblePointerHandler.OnPointerUpEvent -= chatBubbleController.ReleaseBubble;
            bubblePointerHandler.OnPointerDownEvent -= chatBubbleController.PressBubble;
        }
    }
}
