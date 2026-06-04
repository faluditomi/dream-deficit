using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogDirectoryController : MonoBehaviour, ILoadable
{
    private List<ChatLog> activeLogs;
    private GameObject logDirectoryEntryPrefab;
    private Transform content;

    #region Setup
    private void Awake()
    {
        logDirectoryEntryPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePaths.LogDirectoryEntryPrefab);
        content = GetComponentInChildren<ContentSizeFitter>().transform;
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
            LogDirectoryEntryController logDirectoryEntryInstance = Instantiate(logDirectoryEntryPrefab, content).GetComponent<LogDirectoryEntryController>();
            logDirectoryEntryInstance.Setup(chatLog);
        }
    }
    #endregion
}
