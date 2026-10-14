using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DoubleMarker", menuName = "Scriptable Objects/DoubleMarker")]
public class DoubleMarker : ScriptableObject
{
    [Tooltip("How the marker is named in the in-game graph")]
    public string nameLiteral;
    [Tooltip("The color of the marker in the in-game graph")]
    public Color colour;
    [Tooltip("Whether the marker is related to clients or moderators")]
    public bool isClientRelated;
    [Tooltip("The day on which the marker becomes part of the data double graphs")]
    public int introducedOnDay;
    public DoubleMarkerType markerType;

    public int CalculateDailyMarkerScore(DayData dayData)
    {
        int markerValue = markerType switch
        {
            DoubleMarkerType.Accuracy => CalculateAccuracy(dayData),
            DoubleMarkerType.Precision => CalculatePrecision(dayData),
            DoubleMarkerType.PolicyIq => CalculatePolicyIq(dayData),
            DoubleMarkerType.Efficiency => CalculateEfficiency(dayData),
            DoubleMarkerType.Consistency => CalculateConcistency(dayData),
            DoubleMarkerType.Ambition => CalculateAmbition(dayData),
            _ => -1,
        };

        if(markerValue == -1) Debug.LogError("CalculateDailyMarkerScore was called on a marker with a non-supported marker type.");
        return markerValue;
    }

    #region Player Moderator Score Calculators
    /// <summary>
    /// How many of the total markables in the daily (non-bonus/opened) chat logs were correctly flagged by the moderator.
    /// </summary>
    private static int CalculateAccuracy(DayData dayData)
    {
        int found = 0;
        int total = 0;

        foreach(ResolvedChatLogEntry entry in dayData.GetGradeableAssignments())
        {
            if(entry.chatLog?.Graphs == null) continue;

            foreach(ConversationGraph graph in entry.chatLog.Graphs)
            {
                if(graph?.nodes == null) continue;

                foreach(ConversationNodeData node in graph.nodes)
                {
                    if(node == null || node.kind != ConversationNodeKind.Bubble || node.bubble?.flaggables == null) continue;

                    List<FlagData> placedFlags = dayData.flagData.FindAll(flag =>
                        flag != null &&
                        flag.nodeGuid == node.guid &&
                        flag.chatLogPath == entry.chatLog.name);

                    foreach(Flaggable flaggable in node.bubble.flaggables)
                    {
                        if(flaggable?.flagType == null) continue;
                        total++;

                        if(placedFlags.Any(flag => flag.MatchesType(flaggable) 
                            && flaggable.Overlaps(flag.startIndex, flag.endIndex)))
                        {
                            found++;
                        }
                    }
                }
            }
        }

        return total > 0 ? Mathf.RoundToInt(found / (float)total * 100f) : 0;
    }
    
    /// <summary>
    /// What the average accuracy of the player's placed flags is across all (non-bonus/opened) chat logs for the day.
    /// </summary>
    private static int CalculatePrecision(DayData dayData)
    {
        List<ResolvedChatLogEntry> entries = dayData.GetGradeableAssignments();
        if(entries.Count == 0) return 0;
        int totalPrecision = Mathf.RoundToInt(entries.Sum(resolvedChatLogEntry => FlagManager.Instance.CalculateChatLogAccuracy(resolvedChatLogEntry.chatLog)));
        return Mathf.RoundToInt(totalPrecision / (float)entries.Count);
    }
    
    /// <summary>
    /// How many times the moderator removed a correctly placed marker, compared to how many correct ones are left.
    /// </summary>
    private static int CalculatePolicyIq(DayData dayData)
    {
        int correctFlagCount = dayData.flagData.Count(flag => flag.accuracy > 0f);
        int correctRemovedFlagCount = dayData.flagData.Count(flag => flag.accuracy > 0f && flag.isRemoved);
        if(correctFlagCount <= 0) return 0;
        int truePolicyIq = 100 - Mathf.RoundToInt(correctRemovedFlagCount / (float)correctFlagCount * 100f);
        return truePolicyIq > 0 ? truePolicyIq : 0;
    }
    
    /// <summary>
    /// What percentage of all assignments have been opened by the player. (minimum quota opened by default)
    /// </summary>
    private static int CalculateAmbition(DayData dayData)
    {
        return dayData.GetActiveAssignments().Count > 0 
            ? Mathf.RoundToInt(dayData.GetGradeableAssignments().Count / (float)dayData.GetActiveAssignments().Count * 100f) 
            : 0;
    }

    /// <summary>
    /// How many of the total placed flags were correct, compared to incorrect.
    /// </summary>
    private static int CalculateEfficiency(DayData dayData)
    {
        int falseFlagCount = dayData.flagData.Count(flag => flag.accuracy <= 0f);
        int correctFlagCount = dayData.flagData.Count(flag => flag.accuracy > 0f);
        if(correctFlagCount <= 0) return 0;
        int trueEfficiency = 100 - Mathf.RoundToInt(falseFlagCount / (float)correctFlagCount * 100f);
        return trueEfficiency > 0 ? trueEfficiency : 0;
    }
    #endregion
    
    #region Player Global-Only Calculators
    /// <summary>
    /// GLOBAL DATA DOUBLE ONLY. Calculates the consistency of the player's performance across all days based on the daily
    /// average scores of the other markers. Constant values and steady changes are preferred.
    /// </summary>
    private static int CalculateConcistency(DayData dayData)
    {
        // NOTE: it's probably not a good idea that we are getting this each time. we should somehow pass it to the method instead
        ChatUser playerChatUser = AddressableManager.Instance
            .RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + Constants.ChatUser.Avner);
        float[] consistencyValues = DoublesManager.Instance
            .GetDoubleProfileForUser(playerChatUser)
            .GetAllDailyMarkerAverageScoresWithoutConsistency()
            .Select(score => (float)score)
            .ToArray();

        if(consistencyValues == null || consistencyValues.Length < 2) return 100;
        int differenceCount = consistencyValues.Length - 1;
        float[] differences = new float[differenceCount];
        // Calculate differences
        for(int i = 0; i < differenceCount; i++) differences[i] = consistencyValues[i + 1] - consistencyValues[i];
        // Calculate mean difference
        float meanDifference = differences.Average();
        // Calculate average absolute deviation
        float deviation = 0f;
        foreach(float difference in differences) deviation += Mathf.Abs(difference - meanDifference);
        deviation /= differenceCount;
        // Assuming values are percentages (0-100)
        int consistency = Mathf.RoundToInt(100f * (1f - deviation / 100f));
        return Mathf.Clamp(consistency, 0, 100);
    }
    #endregion
}

public enum DoubleMarkerType
{
    Accuracy = 0,
    Precision = 1,
    PolicyIq = 2,
    Ambition = 3,
    Efficiency = 4,
    Consistency = 5,
}
