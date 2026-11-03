using UnityEngine;
using UnityEngine.InputSystem;

public static class Flags
{
    // Watch out not to have two active flag types with the same keycode.
    public static readonly FlagType Scam = new
    (
        "Scam",
        "The act of deceiving someone for financial gain or other benefits.",
        Color.darkMagenta,
        Key.S
    );
    public static readonly FlagType Harassment = new
    (
        "Harassment",
        "The persistent and unwanted attention or behavior that is intended to intimidate, offend, or upset another person.",
        Color.darkBlue,
        Key.H
    );
    public static readonly FlagType Manipulation = new
    (
        "Manipulation",
        "The exercise of emotional pressure, guilt-tripping, or other manipulative tactics to coerce others into sharing content or engaging in specific behaviors.",
        Color.darkSeaGreen,
        Key.M
    );
    public static readonly FlagType Trolling = new
    (
        "Trolling",
        "The deliberate act of posting inflammatory, irrelevant, or offensive content with the intent to provoke a reaction or disrupt a conversation.",
        Color.darkOrange,
        Key.T
    );
    public static readonly FlagType InformationRisk = new
    (
        "Information Risk",
        "The potential for harm or negative consequences resulting from the sharing of sensitive, personal, or confidential information.",
        Color.darkRed,
        Key.I
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
