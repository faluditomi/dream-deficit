using System.Collections.Generic;
using System.Threading.Tasks;

public class LogDirectoryCacheManager : Singleton<LogDirectoryCacheManager>
{
    public static List<ChatLog> activeLogs = new List<ChatLog>();

    public static List<ChatLog> RetrieveCurrentCache()
    {
        ChatLog testLog = AddressableManager.Instance
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.TinderLog);
        activeLogs.Add(testLog);
        ChatLog testLogAlso = AddressableManager.Instance
            .RetrieveAddressable<ChatLog>(Constants.AddressablePaths.ChatLogPrefix + Constants.ChatLogs.Nonsense);
        activeLogs.Add(testLogAlso);
        return activeLogs;
    }
}
