using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserChatResponseOptionController : MonoBehaviour
{
    public void Setup(string previewText, System.Action onClick)
    {
        if(onClick == null) return;
        Transform previewTextTransform = transform.Find(Constants.GameObjectNames.PreviewText);
        TMP_Text previewTextText = previewTextTransform.GetComponent<TMP_Text>();
        previewTextText.text = previewText;
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() => onClick());
    }
}