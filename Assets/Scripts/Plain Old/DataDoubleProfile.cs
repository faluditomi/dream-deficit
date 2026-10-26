using System.Collections.Generic;
using System.Linq;

public class DataDoubleProfile
{
    private Dictionary<int, DataDouble> dailyDoubles = new Dictionary<int, DataDouble>();
    private DataDouble globalDouble;

    // read access for serialization; the snapshot DTOs are built by DoublesManager
    public IEnumerable<KeyValuePair<int, DataDouble>> DailyDoubles => dailyDoubles;

    public DataDoubleProfile(DataDouble globalDouble)
    {
        this.globalDouble = globalDouble;
    }

    // used when rebuilding a DataDoubleProfile from save data
    public void AddDailyDouble(int day, DataDouble dataDouble)
    {
        if(dataDouble == null) return;
        dailyDoubles[day] = dataDouble;
    }

    public DataDouble GetDataDoubleForDay(int day)
    {
        if(!dailyDoubles.ContainsKey(day)) return null;
        return dailyDoubles[day];
    }

    public DataDouble GetGlobalDataDouble()
    {
        return globalDouble;
    }

    public int[] GetAllDailyMarkerScoresForType(DoubleMarkerType markerType)
    {
        return dailyDoubles.Values.Select(dataDouble => dataDouble.GetMarkerScoreByType(markerType)).ToArray();
    }

    public int[] GetAllDailyMarkerAverageScores()
    {
        return dailyDoubles.Values.Select(dataDouble => dataDouble.GetAverageMarkerScore()).ToArray();
    }

    public int[] GetAllDailyMarkerAverageScoresWithoutConsistency()
    {
        return dailyDoubles.Values.Select(dataDouble => dataDouble.GetAverageMarkerScoreWithoutConsistency()).ToArray();
    }
}
