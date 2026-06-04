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

    [System.Serializable] public struct SequencedChatLogEntry
    {
        public ChatLog chatLog;
        public List<ChatBubbleSequence> sequences;
    }

    public List<DayDataEntry> dayEntries = new List<DayDataEntry>();
    public List<SequencedChatLogEntry> sequencedChatLogEntries = new List<SequencedChatLogEntry>();

    public Dictionary<int, DayData> DayDataMap
    {
        get { return dayEntries.ToDictionary(entry => entry.dayNumber, entry => entry.dayData); }
    }

    public Dictionary<ChatLog, List<ChatBubbleSequence>> SequencedChatLogMap
    {
        get { return sequencedChatLogEntries.ToDictionary(entry => entry.chatLog, entry => entry.sequences); }
    }

    public List<ChatBubbleSequence> GetSequencesForChatLog(ChatLog chatLog)
    {
        var entry = sequencedChatLogEntries.Find(e => e.chatLog == chatLog);
        return entry.sequences;
    }

    public void AddSequenceToChatLog(ChatLog chatLog, ChatBubbleSequence sequence)
    {
        if(sequencedChatLogEntries.Any(e => e.chatLog == chatLog))
        {
            var entry = sequencedChatLogEntries.Find(e => e.chatLog == chatLog);
            entry.sequences.Add(sequence);
        }
        else
        {
            sequencedChatLogEntries.Add(
                new SequencedChatLogEntry 
                { 
                    chatLog = chatLog, 
                    sequences = new List<ChatBubbleSequence> { sequence } 
                }
            );
        }
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
