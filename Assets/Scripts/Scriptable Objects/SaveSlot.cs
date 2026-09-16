using System.Collections.Generic;
using UnityEngine;
using static GameTemplate;

[CreateAssetMenu(fileName = "SaveSlot", menuName = "Scriptable Objects/SaveSlot")]
public class SaveSlot : ScriptableObject
{
    public string slotName;
    public GameTemplate template;
    public RunData runData;
    public List<DayDataEntry> dayEntries = new List<DayDataEntry>();
}
