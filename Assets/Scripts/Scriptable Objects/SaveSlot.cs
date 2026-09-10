using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SaveSlot", menuName = "Scriptable Objects/SaveSlot")]
public class SaveSlot : ScriptableObject
{
    public string slotName;
    public GameTemplate template;
    public int currentDayNumber = 1;
    public List<GameTemplate.DayDataEntry> dayEntries = new List<GameTemplate.DayDataEntry>();
}
