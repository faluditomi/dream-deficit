using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LogDirectoryCacheManager : MonoBehaviour
{
    private static LogDirectoryCacheManager _instance;

    public static List<ChatLog> activeLogs = new List<ChatLog>();

    public static LogDirectoryCacheManager Instance()
    {
        if(_instance == null)
        {
            var obj = new GameObject("ActiveChatLogCache");
            _instance = obj.AddComponent<LogDirectoryCacheManager>();
            DontDestroyOnLoad(obj);
        }

        return _instance;
    }

    public static async Task<List<ChatLog>> RetrieveCurrentCache()
    {
        ChatLog testLog = await AddressableManager.Instance()
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.TinderLog);
        activeLogs.Add(testLog);
        ChatLog testLogAlso = await AddressableManager.Instance()
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.Nonsense);
        activeLogs.Add(testLogAlso);
        return activeLogs;
    }
}
