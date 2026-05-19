using UnityEngine;
using UnityEngine.UI;

public class MarkerCheatSheetController : BaseWindowController
{
    private GameObject markerCheatSheetPrefab;
    private GameObject markerCheatSheetEntryPrefab;
    private GameObject myMarkerCheatSheet;
    private Transform markerEntryContainer;

    private async void Start()
    {
        markerCheatSheetPrefab = await AddressableManager
            .Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePaths.MarkerCheatSheetPrefab);
        markerCheatSheetEntryPrefab = await AddressableManager
            .Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePaths.MarkerCheatSheetEntryPrefab);
        GetComponent<Button>().onClick.AddListener(() => OpenCheatSheet());
    }

    private void OpenCheatSheet()
    {
        if(myMarkerCheatSheet == null)
        {
            Transform windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
            myMarkerCheatSheet = Instantiate(markerCheatSheetPrefab, windowContainer);
            SetupTopBar(myMarkerCheatSheet);
            markerEntryContainer = myMarkerCheatSheet.transform.GetComponentInChildren<ContentSizeFitter>().transform;
            UpdateMarkers();
        }

        if(!GetIsOpen())
        {
            Open();
        }
    }

    private void UpdateMarkers()
    {
        foreach(Transform child in markerEntryContainer)
        {
            Destroy(child.gameObject);
        }

        foreach(MarkerType markerType in MarkerManager.Instance.activeMarkerTypeCache)
        {
            MarkerCheatSheetEntryController markerCheatSheetEntryInstance = 
                Instantiate(markerCheatSheetEntryPrefab, markerEntryContainer).GetComponent<MarkerCheatSheetEntryController>();
            markerCheatSheetEntryInstance.Setup(markerType);
        }
    }
}
