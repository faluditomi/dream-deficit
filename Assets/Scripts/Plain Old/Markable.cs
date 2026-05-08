using UnityEngine;

[System.Serializable]
public class Markable
{
    public MarkerType markerType;
    [TextArea(1, 2)] public string spanText;
    [Min(0)] public int occurrence;
    public int startIndex;
    public int endIndex;

    public void RecalculateIndexes(string message)
    {
        if(string.IsNullOrEmpty(message) || string.IsNullOrEmpty(spanText))
        {
            startIndex = endIndex = -1;
            return;
        }

        int found = -1;
        int searchFrom = 0;

        for(int attempt = 0; attempt <= occurrence; attempt++)
        {
            found = message.IndexOf(spanText, searchFrom, System.StringComparison.InvariantCulture);
            if (found < 0) break;
            searchFrom = found + 1;
        }

        if(found >= 0)
        {
            startIndex = found;
            endIndex = Mathf.Min(found + spanText.Length - 1, message.Length - 1);
        }
        else
        {
            startIndex = endIndex = -1;
        }
    }

    public string GetSelectedText(string message)
    {
        if(string.IsNullOrEmpty(message) || startIndex < 0 || endIndex < startIndex || endIndex >= message.Length) return string.Empty;
        return message.Substring(startIndex, endIndex - startIndex + 1);
    }

    public bool HasValidRange(string message)
    {
        return !string.IsNullOrEmpty(message) && startIndex >= 0 && endIndex >= startIndex && endIndex < message.Length;
    }
}
