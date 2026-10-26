using System.Collections.Generic;
using System.Linq;

public class DataDouble
{
    private Dictionary<DoubleMarker, MarkerMetaData> markers = new Dictionary<DoubleMarker, MarkerMetaData>();
    public bool isModeratorProfile;
    public bool isClientProfile;

    // read access for serialization; the snapshot DTOs are built by DoublesManager
    public IEnumerable<DoubleMarker> MarkerKeys => markers.Keys;

    public DataDouble(bool isModeratorProfile, bool isClientProfile)
    {
        this.isModeratorProfile = isModeratorProfile;
        this.isClientProfile = isClientProfile;
    }

    // used when rebuilding a DataDouble from save data
    public void SetMarker(DoubleMarker marker, int baseline, int score)
    {
        if(marker == null) return;
        markers[marker] = new MarkerMetaData(baseline, score);
    }

    public bool IsMarkerInRed(DoubleMarker marker)
    {
        if(!markers.ContainsKey(marker)) return false;
        MarkerMetaData markerMetaData = markers[marker];
        return markerMetaData.baseline > 0 && markerMetaData.score < markerMetaData.baseline;
    }

    public int GetMarkerScore(DoubleMarker marker)
    {
        if(!markers.ContainsKey(marker)) return 0;
        return markers[marker].score;
    }

    public int GetMarkerScoreByType(DoubleMarkerType markerType)
    {
        foreach(DoubleMarker marker in markers.Keys)
        {
            if(marker.markerType == markerType) return markers[marker].score;
        }

        return 0;
    }

    public int GetMarkerBaseline(DoubleMarker marker)
    {
        if(!markers.ContainsKey(marker)) return 0;
        return markers[marker].baseline;
    }

    public int GetMarkerBaselineByType(DoubleMarkerType markerType)
    {
        foreach(DoubleMarker marker in markers.Keys)
        {
            if(marker.markerType == markerType) return markers[marker].baseline;
        }

        return 0;
    }

    public int GetAverageMarkerScore()
    {
        if(markers.Count == 0) return 0;
        int totalScore = markers.Values.Sum(markerMetaData => markerMetaData.score);
        return totalScore / markers.Count;
    }

    public int GetAverageMarkerScoreWithoutConsistency()
    {
        if(markers.Count == 0) return 0;
        MarkerMetaData[] markerMetaData = markers.Where(marker => marker.Key.markerType != DoubleMarkerType.Consistency)
            .Select(marker => marker.Value).ToArray(); 
        int totalScore = markerMetaData.Sum(marker => marker.score);
        return totalScore / markerMetaData.Length;
    }

    private class MarkerMetaData
    {
        public int baseline = 0;
        public int score = 0;

        public MarkerMetaData(int baseline, int score)
        {
            this.baseline = baseline;
            this.score = score;
        }
    }
}
