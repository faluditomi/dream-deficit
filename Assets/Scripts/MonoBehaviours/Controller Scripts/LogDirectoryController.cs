using System.Collections.Generic;
using UnityEngine;

public class LogDirectoryController : MonoBehaviour
{
    private List<ChatLog> activeLogs;
    private GameObject logDirectoryEntryPrefab;

    public async void Setup(Transform content)
    {
        activeLogs = await ActiveChatLogCache.RetrieveCurrentCache();
        logDirectoryEntryPrefab = await AddressableController.Instance().RetrieveAddressable<GameObject>(Constants.Addressable.LogDirectoryEntry);

        foreach(ChatLog chatLog in activeLogs)
        {
            LogDirectoryEntryUI logDirectoryEntryInstance = Instantiate(logDirectoryEntryPrefab, content).GetComponent<LogDirectoryEntryUI>();
            logDirectoryEntryInstance.Setup(chatLog);
        }
    }
}
