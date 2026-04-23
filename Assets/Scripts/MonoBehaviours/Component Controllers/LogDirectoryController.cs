using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogDirectoryController : MonoBehaviour
{
    private List<ChatLog> activeLogs;
    private GameObject logDirectoryEntryPrefab;

    private bool isSetUp = false;

    #region Setup
    private void Start()
    {
        Setup();
    }

    private async void Setup()
    {
        if(isSetUp || !FindElements()) return;

        Transform content = GetComponentInChildren<ContentSizeFitter>().transform;
        activeLogs = await LogDirectoryCacheManager.RetrieveCurrentCache();
        logDirectoryEntryPrefab = await AddressableManager.Instance().RetrieveAddressable<GameObject>(Constants.AddressablePaths.LogDirectoryEntryPrefab);

        foreach(ChatLog chatLog in activeLogs)
        {
            LogDirectoryEntryController logDirectoryEntryInstance = Instantiate(logDirectoryEntryPrefab, content).GetComponent<LogDirectoryEntryController>();
            logDirectoryEntryInstance.Setup(chatLog);
        }

        isSetUp = true;
    }

    private bool FindElements()
    {
        return true;
    }
    #endregion
}
