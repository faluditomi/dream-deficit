using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class MarkerCheatSheetEntryController : MonoBehaviour
{
    private Image backgroundImage;
    private TMP_Text nameText;
    private TMP_Text descriptionText;
    private TMP_Text keycodeText;
    
    private bool isSetUp = false;

    #region Setup
    public void Setup(MarkerType markerType)
    {
        if(isSetUp || !FindElements()) return;
        backgroundImage.color = markerType.colour;
        nameText.text = markerType.name;
        descriptionText.text = markerType.description;
        keycodeText.text = "Keybind: " + markerType.keycode.ToString();
        isSetUp = true;
    }

    private bool FindElements()
    {
        backgroundImage = GetComponent<Image>();
        nameText = transform.Find(Constants.GameObjectNames.Name).GetComponent<TMP_Text>();
        descriptionText = transform.Find(Constants.GameObjectNames.Description).GetComponent<TMP_Text>();
        keycodeText = transform.Find(Constants.GameObjectNames.Keycode).GetComponent<TMP_Text>();

        if(backgroundImage == null || nameText == null || descriptionText == null || keycodeText == null)
        {
            Debug.LogError("Setup of MarkerCheatSheetEntry failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
    #endregion
}
