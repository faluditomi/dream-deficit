using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ActiveChatLogCache : MonoBehaviour
{
    private static ActiveChatLogCache _instance;

    public static List<ChatLog> activeLogs = new List<ChatLog>();

    public static ActiveChatLogCache Instance()
    {
        if(_instance == null)
        {
            var obj = new GameObject("ActiveChatLogCache");
            _instance = obj.AddComponent<ActiveChatLogCache>();
            DontDestroyOnLoad(obj);
        }

        return _instance;
    }

    public static async Task<List<ChatLog>> RetrieveCurrentCache()
    {
        ChatLog testLog = await AddressableController.Instance().RetrieveAddressable<ChatLog>("chat_logs/first_test");
        activeLogs.Add(testLog);
        ChatLog testLogAlso = await AddressableController.Instance().RetrieveAddressable<ChatLog>("chat_logs/second_test");
        activeLogs.Add(testLogAlso);
        return activeLogs;
    }
}
