using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssignmentDocketController : BaseWindowController, ILoadable
{
    private GameObject assignmentEntryPrefab;
    private Transform content;

    #region Setup
    private void Awake()
    {
        assignmentEntryPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.AssignmentEntry);
        content = GetComponentInChildren<ContentSizeFitter>().transform;
        SetupTopBar(Constants.WindowAndFileNames.AssignmentDocket.ToString());
        GetComponent<TopBarHandler>().Close();
    }

    public void LoadFromDayData(DayData dayData)
    {
        List<ResolvedChatLogEntry> activeEntries = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetActiveChatLogEntries();

        foreach(Transform child in content)
        {
            Destroy(child.gameObject);
        }
        
        foreach(ResolvedChatLogEntry entry in activeEntries)
        {
            AssignmentEntryController logDirectoryEntryInstance = Instantiate(assignmentEntryPrefab, content).GetComponent<AssignmentEntryController>();
            logDirectoryEntryInstance.Setup(entry.chatLog, entry.isBonus, entry.isUnlocked);
        }
    }
    #endregion
}
