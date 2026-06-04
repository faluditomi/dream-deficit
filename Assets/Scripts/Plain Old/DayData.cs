using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[System.Serializable]
public class DayData
{
    public int dayNumber;
    public List<string> activeChatLogNames = new List<string>();
    public List<string> supervisorBubbleSequenceNames = new List<string>();
    public List<string> markerTypeNames = new List<string>();
    public List<MarkerData> markerData = new List<MarkerData>();
    // TODO: add dream scene reference once we have that set up

    public List<ChatLog> GetActiveChatLogs()
    {
        return activeChatLogNames
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => AddressableManager.Instance.RetrieveAddressable<ChatLog>(
                Constants.AddressablePaths.ChatLogPrefix + path))
            .Where(log => log != null)
            .ToList();
    }

    public List<ChatBubbleSequence> GetSupervisorSequences()
    {
        return supervisorBubbleSequenceNames
            .Where(path => !string.IsNullOrEmpty(path))
            .Select(path => AddressableManager.Instance.RetrieveAddressable<ChatBubbleSequence>(
                Constants.AddressablePaths.ChatBubbleSequence + path))
            .Where(seq => seq != null)
            .ToList();
    }

    public List<MarkerType> GetMarkerTypes()
    {
        var markerTypeFields = typeof(Markers).GetFields(
            BindingFlags.Public |
            BindingFlags.Static);

        List<MarkerType> markerTypes = new List<MarkerType>();

        foreach (var typeName in markerTypeNames)
        {
            foreach (var field in markerTypeFields)
            {
                if (field.FieldType == typeof(MarkerType))
                {
                    MarkerType markerType = (MarkerType)field.GetValue(null);
                    if (markerType != null && markerType.name == typeName)
                    {
                        markerTypes.Add(markerType);
                        break;
                    }
                }
            }
        }

        return markerTypes;
    }

    public List<MarkerData> GetMarkerData()
    {
        return markerData
            .Where(md => md != null && !string.IsNullOrEmpty(md.markerTypeName))
            .ToList();
    }
}
