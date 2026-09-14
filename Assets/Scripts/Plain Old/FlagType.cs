using UnityEngine;
using UnityEngine.InputSystem;

public static class Flags
{
    // NOTE: watch out not to have two active flag types with the same keycode
    public static readonly FlagType CopyPasteSpam = new
    (
        "Copy Paste Spam",
        "The often unsolicited and irritating act of publishing identical or nearly identical content repeatedly with the intent to disrupt, promote, or troll.",
        Color.darkMagenta,
        Key.C
    );
    public static readonly FlagType SynchronisedActivitySpike = new
    (
        "Synchronised Activity Spike",
        "An unusual burst of activity in a condensed timeframe from multiple sources that may indicate coordination.",
        Color.darkBlue,
        Key.S
    );
    public static readonly FlagType PersuasionPattern = new
    (
        "Persuasion Pattern",
        "The exercise of emotional pressure, guilt-tripping, or other manipulative tactics to coerce others into sharing content or engaging in specific behaviors.",
        Color.darkSeaGreen,
        Key.P
    );
    public static readonly FlagType NarrativeShaping = new
    (
        "Narrative Shaping",
        "The strategic dissemination of information, often through selective sharing or framing, to influence public perception in a way that aligns with a harmful agenda.",
        Color.darkOrange,
        Key.N
    );
}

[System.Serializable]
public class FlagType
{
    public string name;
    public string description;
    public Color colour;
    public Key keycode;

    public FlagType() { }

    public FlagType(string name, string description, Color colour, Key keycode)
    {
        this.name = name;
        this.description = description;
        this.colour = colour;
        this.keycode = keycode;
    }
}
