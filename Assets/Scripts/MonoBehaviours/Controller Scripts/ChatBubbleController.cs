using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatBubbleController : MonoBehaviour
{
    private PointerHandler bubblePointerHandler;

    private Image profilePictureImage;
    private TMP_Text usernameText;
    private TMP_Text messageText;

    private bool isSetUp = false;

    #region Setup
    public async void Setup(ChatBubble chatBubble)
    {
        if(isSetUp || !FindElements()) return;

        // NOTE: Depending on how large and numerous our chat logs will be, these per bubble addressable calls might get expensive
        string address = Constants.AddressablePaths.ChatUserPrefix + chatBubble.chatUserId.ToString().ToLower();
        ChatUser chatUser = await AddressableController.Instance().RetrieveAddressable<ChatUser>(address);

        if(chatUser == null) Debug.LogError($"ChatUser Addressable wasn't found for ChatUser: {chatBubble.chatUserId}. Setup of ChatBubble failed.");

        profilePictureImage = chatUser.profilePicture;
        usernameText.text = chatUser.username;
        messageText.text = chatBubble.message;

        bubblePointerHandler.OnPointerUpEvent += ReleaseBubble;
        bubblePointerHandler.OnPointerDownEvent += PressBubble;

        isSetUp = true;
    }

    private bool FindElements()
    {
        Transform profilePicture = transform.Find(Constants.GameObjectNames.ProfilePicture);
        profilePictureImage = profilePicture.GetComponent<Image>();
        Transform username = transform.Find(Constants.GameObjectNames.Username);
        usernameText = username.GetComponent<TMP_Text>();
        messageText = transform.Find(Constants.GameObjectNames.Message).GetComponent<TMP_Text>();
        bubblePointerHandler = transform.Find(Constants.GameObjectNames.Bubble).AddComponent<PointerHandler>();

        if(!profilePicture || !profilePictureImage || !usernameText || !messageText)
        {
            Debug.LogError("Setup of ChatBubble failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
    #endregion
    
    public void PressBubble(PointerEventData evetnData)
    {
        Debug.Log("bubble down");
    }

    public void ReleaseBubble(PointerEventData eventData)
    {
        Debug.Log("bubble up");
    }

    private void OnDestroy()
    {
        if(bubblePointerHandler)
        {
            bubblePointerHandler.OnPointerUpEvent -= ReleaseBubble;
            bubblePointerHandler.OnPointerDownEvent -= PressBubble;
        }
    }
}
