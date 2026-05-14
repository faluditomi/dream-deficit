using System.Collections.Generic;
using System.Threading.Tasks;

public class LogDirectoryCacheManager : Singleton<LogDirectoryCacheManager>
{
    public static List<ChatLog> activeLogs = new List<ChatLog>();

    public static async Task<List<ChatLog>> RetrieveCurrentCache()
    {
        ChatLog testLog = await AddressableManager.Instance
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.TinderLog);
        activeLogs.Add(testLog);
        ChatLog testLogAlso = await AddressableManager.Instance
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.Nonsense);
        activeLogs.Add(testLogAlso);
        return activeLogs;
    }
}
