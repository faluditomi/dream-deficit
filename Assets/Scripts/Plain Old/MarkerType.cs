using UnityEngine;

public static class Markers
{
    public static readonly MarkerType CopyPasteSpam = new
    (
        "Copy Paste Spam",
        "Publishing identical or nearly identical content over and over again.",
        Color.darkOrange
    );
    public static readonly MarkerType SynchronisedActivitySpike = new
    (
        "Synchronised Activity Spike",
        "Separate users publishing identical or nearly identical content, or behaving in similar ways all at once.",
        Color.darkBlue
    );
}

public class MarkerType
{
    public string name;
    public string description;
    public Color colour;

    public MarkerType(string name, string description, Color colour)
    {
        this.name = name;
        this.description = description;
        this.colour = colour;
    }
}
