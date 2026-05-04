using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MarkerCheatSheetController : MonoBehaviour, ITopBar
{
    private GameObject markerCheatSheetPrefab;
    private GameObject markerCheatSheetEntryPrefab;
    private GameObject myMarkerCheatSheet;
    private TopBarHandler topBarHandler;
    private Transform markerEntryContainer;
    private bool isOpen = false;

    private async void Start()
    {
        markerCheatSheetPrefab = await AddressableManager
            .Instance()
            .RetrieveAddressable<GameObject>(Constants.AddressablePaths.MarkerCheatSheetPrefab);
        markerCheatSheetEntryPrefab = await AddressableManager
            .Instance()
            .RetrieveAddressable<GameObject>(Constants.AddressablePaths.MarkerCheatSheetEntryPrefab);
        GetComponent<Button>().onClick.AddListener(() => OpenCheatSheet());
    }

    private void OpenCheatSheet()
    {
        if(myMarkerCheatSheet == null)
        {
            myMarkerCheatSheet = Instantiate(markerCheatSheetPrefab, transform.parent);
            topBarHandler = myMarkerCheatSheet.AddComponent<TopBarHandler>();
            topBarHandler.Setup(myMarkerCheatSheet, this);
            markerEntryContainer = myMarkerCheatSheet.transform.GetComponentInChildren<ContentSizeFitter>().transform;
            UpdateMarkers();
        }

        if(!isOpen)
        {
            topBarHandler.Open();
        }
    }

    private void UpdateMarkers()
    {
        foreach(Transform child in markerEntryContainer)
        {
            Destroy(child.gameObject);
        }

        foreach(MarkerType markerType in MarkerManager.Instance().activeMarkerTypeCache)
        {
            MarkerCheatSheetEntryController markerCheatSheetEntryInstance = Instantiate(markerCheatSheetEntryPrefab, markerEntryContainer)
                .GetComponent<MarkerCheatSheetEntryController>();
            markerCheatSheetEntryInstance.Setup(markerType);
        }
    }

    #region ITopBar
    public bool GetIsOpen()
    {
        return isOpen;
    }

    public void SetIsOpen(bool isOpen)
    {
        this.isOpen = isOpen;
    }

    public void Open()
    {
        topBarHandler.Open();
    }
    #endregion
}
