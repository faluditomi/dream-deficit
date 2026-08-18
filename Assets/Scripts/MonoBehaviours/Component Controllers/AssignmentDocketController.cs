using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssignmentDocketController : BaseWindowController, ILoadable
{
    private List<ChatLog> activeLogs;
    private GameObject assignmentEntryPrefab;
    private Transform content;

    #region Setup
    private void Awake()
    {
        assignmentEntryPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.AssignmentEntry);
        content = GetComponentInChildren<ContentSizeFitter>().transform;
        SetupTopBar(Constants.WindowAndFileNames.FlagCheatSheet.ToString());
        GetComponent<TopBarHandler>().Close();
    }

    public void LoadFromDayData(DayData dayData)
    {
        activeLogs = SaveManager.Instance.GetDayData(GameManager.Instance.CurrentDayNumber).GetActiveChatLogs();

        foreach(Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach(ChatLog chatLog in activeLogs)
        {
            AssignmentEntryController logDirectoryEntryInstance = Instantiate(assignmentEntryPrefab, content).GetComponent<AssignmentEntryController>();
            logDirectoryEntryInstance.Setup(chatLog);
        }
    }
    #endregion
}
