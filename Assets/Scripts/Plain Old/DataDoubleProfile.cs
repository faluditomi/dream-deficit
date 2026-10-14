using System.Collections.Generic;
using System.Linq;

public class DataDoubleProfile
{
    private Dictionary<int, DataDouble> dailyDoubles = new Dictionary<int, DataDouble>();
    private DataDouble globalDouble;

    public DataDoubleProfile(DataDouble globalDouble)
    {
        this.globalDouble = globalDouble;
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
