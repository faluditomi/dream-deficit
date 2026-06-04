using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is a save slot that holds the progress of the player. When created, it takes a GameTemplate that outlines the
/// structure of the game and a save file is created.
/// </summary>
[CreateAssetMenu(fileName = "SaveSlot", menuName = "Scriptable Objects/SaveSlot")]
public class SaveSlot : ScriptableObject
{
    public string slotName;
    public GameTemplate template;
    public int currentDayNumber;
    public List<GameTemplate.DayDataEntry> dayEntries = new List<GameTemplate.DayDataEntry>();
    public List<GameTemplate.SequencedChatLogEntry> sequencedChatLogEntries = new List<GameTemplate.SequencedChatLogEntry>();
    public DateTime createdAt;
    public DateTime lastPlayed;
}
