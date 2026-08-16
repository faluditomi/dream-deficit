using UnityEngine;
using UnityEngine.UI;

public class MarkerCheatSheetButtonController : BaseWindowController
{
    public void Start()
    {
        GameObject markerCheatSheetPrefab = AddressableManager
            .Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.MarkerCheatSheet);
        Transform windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
        MarkerCheatSheetController myMarkerCheatSheet = Instantiate(markerCheatSheetPrefab, windowContainer).GetComponent<MarkerCheatSheetController>();
        GetComponent<Button>().onClick.AddListener(() => myMarkerCheatSheet.Open());
    }
}
