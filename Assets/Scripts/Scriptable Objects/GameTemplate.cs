using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This is a template that outlines the structure of a game run with all its contents. It can be used to
/// create different "game runs" like a tutorial or the entire game.
/// </summary>
[CreateAssetMenu(fileName = "GameTemplate", menuName = "Scriptable Objects/GameTemplate")]
public class GameTemplate : ScriptableObject
{
    [System.Serializable] public struct DayDataEntry
    {
        public int dayNumber;
        public DayData dayData;
    }

    public List<DayDataEntry> dayEntries = new List<DayDataEntry>();

    public Dictionary<int, DayData> DayDataMap
    {
        get { return dayEntries.ToDictionary(entry => entry.dayNumber, entry => entry.dayData); }
    }

    public DayData GetDayData(int dayNumber)
    {
        var entry = dayEntries.Find(e => e.dayNumber == dayNumber);
        return entry.dayData;
    }

    public bool HasDay(int dayNumber)
    {
        return dayEntries.Any(e => e.dayNumber == dayNumber);
    }
}
