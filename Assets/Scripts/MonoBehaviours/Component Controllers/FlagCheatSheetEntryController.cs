using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class FlagCheatSheetEntryController : MonoBehaviour
{
    private Image backgroundImage;
    private TMP_Text nameText;
    private TMP_Text descriptionText;
    private TMP_Text keycodeText;
    
    private bool isSetUp = false;

    #region Setup
    public void Setup(FlagType flagType)
    {
        if(isSetUp || !FindElements()) return;
        backgroundImage.color = flagType.colour;
        nameText.text = flagType.name;
        descriptionText.text = flagType.description;
        keycodeText.text = "Keybind: " + flagType.keycode.ToString();

        nameText.gameObject.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        descriptionText.gameObject.AddComponent<HighlightHandler>().SetupOnlyHighlight();
        keycodeText.gameObject.AddComponent<HighlightHandler>().SetupOnlyHighlight();

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
            Debug.LogError("Setup of FlagCheatSheetEntry failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
    #endregion
}
