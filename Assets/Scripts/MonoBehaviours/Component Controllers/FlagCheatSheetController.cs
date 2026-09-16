using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlagCheatSheetController : BaseWindowController, IDayLoadable
{
    private GameObject flagCheatSheetEntryPrefab;
    private Transform flagEntryContainer;

    private void Awake()
    {
        flagCheatSheetEntryPrefab = AddressableManager
            .Instance
            .RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.FlagCheatSheetEntry);
        flagEntryContainer = transform.GetComponentInChildren<ContentSizeFitter>().transform;
        SetupBaseWindow(Constants.WindowAndFileNames.FlagCheatSheet.ToString());
        GetComponent<TopBarHandler>().Close();
    }
    
    // TODO: we should get rid of the FlagManager.Instance.activeFlagTypeCache since we're not using it here anymore
    // TODO: and simply are getting the flag types from the DayData instead
    public void LoadFromDayData(DayData dayData)
    {
        List<FlagType> activeFlags = SaveManager.Instance.GetCurrentDayData().GetFlagTypes();

        foreach(Transform child in flagEntryContainer)
        {
            Destroy(child.gameObject);
        }

        foreach(FlagType flagType in activeFlags)
        {
            FlagCheatSheetEntryController flagCheatSheetEntryInstance = 
                Instantiate(flagCheatSheetEntryPrefab, flagEntryContainer).GetComponent<FlagCheatSheetEntryController>();
            flagCheatSheetEntryInstance.Setup(flagType);
        }
    }
}
