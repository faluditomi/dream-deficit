using UnityEngine;
using UnityEngine.UI;

public class MarkerCheatSheetController : BaseWindowController
{
    private GameObject markerCheatSheetEntryPrefab;
    private Transform markerEntryContainer;

    private void Start()
    {
        markerCheatSheetEntryPrefab = AddressableManager
            .Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.MarkerCheatSheetEntry);
        markerEntryContainer = transform.GetComponentInChildren<ContentSizeFitter>().transform;
        SetupTopBar();
        UpdateMarkers();
        GetComponent<TopBarHandler>().Close();
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
