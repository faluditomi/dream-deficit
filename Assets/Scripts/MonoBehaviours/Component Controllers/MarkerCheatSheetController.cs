using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarkerCheatSheetController : BaseWindowController, ILoadable
{
    private GameObject markerCheatSheetEntryPrefab;
    private Transform markerEntryContainer;

    private void Awake()
    {
        markerCheatSheetEntryPrefab = AddressableManager
            .Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.MarkerCheatSheetEntry);
        markerEntryContainer = transform.GetComponentInChildren<ContentSizeFitter>().transform;
        SetupTopBar(Constants.WindowAndFileNames.FlagCheatSheet.ToString());
        GetComponent<TopBarHandler>().Close();
    }
    
    // TODO: we should get rid of the MarkerManager.Instance.activeMarkerTypeCache since we're not using it here anymore
    // TODO: and simply are getting the marker types from the DayData instead
    public void LoadFromDayData(DayData dayData)
    {
        List<MarkerType> activeMarkers = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetMarkerTypes();

        foreach(Transform child in markerEntryContainer)
        {
            Destroy(child.gameObject);
        }

        foreach(MarkerType markerType in activeMarkers)
        {
            MarkerCheatSheetEntryController markerCheatSheetEntryInstance = 
                Instantiate(markerCheatSheetEntryPrefab, markerEntryContainer).GetComponent<MarkerCheatSheetEntryController>();
            markerCheatSheetEntryInstance.Setup(markerType);
        }
    }
}
