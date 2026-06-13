using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ChatUser", menuName = "Scriptable Objects/ChatUser")]
public class ChatUser : ScriptableObject
{
    public Constants.ChatUser chatUser;
    public string username;
    public Sprite profilePicture;
}
