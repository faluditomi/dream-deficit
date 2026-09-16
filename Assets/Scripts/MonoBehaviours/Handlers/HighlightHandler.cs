using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TMP_Text))]
public class HighlightHandler : MonoBehaviour, IHighlightable
{
    public Color hoverColour = Color.red;
    public Color activeColour = Color.lightBlue;
    public TMP_Text myText;
    private string rawText;
    private ChatLog chatLog;
    private ChatBubble chatBubble;
    private string nodeGuid;
    private FlagData previousHoveredFlag;
    private FlagData hoveredFlag;

    public bool canMark;
    private bool hasTemporaryHighlight = false;
    private int persistentTemporaryStart = -1;
    private int persistentTemporaryEnd = -1;
    private int currentSelectionStart = -1;
    private int currentSelectionEnd = -1;

    private void Awake()
    {
        myText = GetComponent<TMP_Text>();
        rawText = myText.text;
        myText.richText = true;
    }

    public void SetupOnlyHighlight()
    {
        canMark = false;
    }

    public void Setup(ChatLog chatLog, ChatBubble chatBubble, string nodeGuid, bool canMark)
    {
        this.chatLog = chatLog;
        this.chatBubble = chatBubble;
        this.nodeGuid = nodeGuid;
        this.canMark = canMark;
    }

    private List<FlagData> GetFlags()
    {
        if(string.IsNullOrEmpty(nodeGuid)) return new List<FlagData>();
        return FlagManager.Instance.GetFlagsForChatBubble(chatLog, nodeGuid);
    }

    public void OnMouseHover()
    {
        int hoveredCharIndex = GetCurrentCharIndex();
        var flags = GetFlags();
        List<FlagData> overlapping = flags.FindAll(m => hoveredCharIndex >= m.startIndex && hoveredCharIndex <= m.endIndex);

        if(overlapping.Count == 0 && FlagManager.Instance.activeFlagType == null)
        {
            overlapping = FindFlagInBoundsArea(flags);
        }

        if(overlapping.Count > 0 && FlagManager.Instance.activeFlagType == null)
        {
            hoveredFlag = overlapping[overlapping.Count - 1];

            if(previousHoveredFlag != hoveredFlag)
            {
                currentSelectionStart = hoveredFlag.startIndex;
                currentSelectionEnd = hoveredFlag.endIndex;
                previousHoveredFlag = hoveredFlag;
                Rebuild(hoverColour);
            }
        }
        else
        {
            if(previousHoveredFlag == null) return;
            currentSelectionStart = previousHoveredFlag.startIndex;
            currentSelectionEnd = previousHoveredFlag.endIndex;
            Rebuild(previousHoveredFlag.ResolvedFlagType.colour);
            previousHoveredFlag = hoveredFlag = null;
        }
    }

    private List<FlagData> FindFlagInBoundsArea(List<FlagData> flags)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        List<FlagData> flagsContainingPoint = new List<FlagData>();

        foreach(var flag in flags)
        {
            if(IsPointInFlagBounds(flag, mousePos))
            {
                flagsContainingPoint.Add(flag);
            }
        }

        return flagsContainingPoint;
    }

    private bool IsPointInFlagBounds(FlagData flag, Vector2 point)
    {
        myText.ForceMeshUpdate();
        TMP_TextInfo textInfo = myText.textInfo;
        if(textInfo == null || textInfo.characterCount == 0) return false;
        int start = Mathf.Max(0, flag.startIndex);
        int end = Mathf.Min(textInfo.characterCount - 1, flag.endIndex);
        if(start >= textInfo.characterCount || end < 0) return false;
        RectTransform rectTransform = myText.GetComponent<RectTransform>();
        Matrix4x4 localToWorld = rectTransform.localToWorldMatrix;
        Camera cam = null;
        Canvas canvas = myText.canvas;
        if(canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera;
        Vector2 minPos = Vector2.positiveInfinity;
        Vector2 maxPos = Vector2.negativeInfinity;

        for(int i = start; i <= end; i++)
        {
            if(i < 0 || i >= textInfo.characterCount) continue;
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            int lineIndex = charInfo.lineNumber;
            TMP_LineInfo lineInfo = textInfo.lineInfo[lineIndex];

            float lineAscender = lineInfo.ascender;
            float lineDescender = lineInfo.descender;
            float charLeft = charInfo.origin;
            float charRight = charInfo.xAdvance;

            Vector2[] localCorners = new Vector2[4]
            {
                new Vector2(charLeft, lineAscender),
                new Vector2(charLeft, lineDescender),
                new Vector2(charRight, lineAscender),
                new Vector2(charRight, lineDescender)
            };

            foreach(var localCorner in localCorners)
            {
                Vector3 worldCorner = localToWorld.MultiplyPoint(localCorner);
                Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(cam, worldCorner);
                minPos = Vector2.Min(minPos, screenCorner);
                maxPos = Vector2.Max(maxPos, screenCorner);
            }
        }

        if(minPos == Vector2.positiveInfinity || maxPos == Vector2.negativeInfinity) return false;

        return point.x >= minPos.x && point.x <= maxPos.x && point.y >= minPos.y && point.y <= maxPos.y;
    }

    public void OnMouseDown()
    {
        currentSelectionStart = currentSelectionEnd = GetClosestCharIndex();
    }

    public void OnMouseHeld()
    {
        currentSelectionEnd = GetClosestCharIndex();
        var activeFlagType = FlagManager.Instance.activeFlagType;

        if(hoveredFlag != null && currentSelectionStart == currentSelectionEnd && activeFlagType == null)
        {
            Rebuild(hoverColour);
            return;
        }

        Color highlightColour = activeFlagType != null ? activeFlagType.colour : activeColour;
        Rebuild(highlightColour);
    }

    public void OnMouseUp()
    {
        FlagType activeFlagType = FlagManager.Instance.activeFlagType;
        currentSelectionEnd = GetClosestCharIndex();

        if(currentSelectionStart == currentSelectionEnd && activeFlagType == null)
        {
            FlagManager.Instance.RemoveFlag(hoveredFlag);
            Rebuild(Color.clear);
            hoveredFlag = previousHoveredFlag = null;
        }
        else
        {
            HighlightMouseUp(activeFlagType);
        }

        CanMarkMouseUp(activeFlagType);
    }
    
    public void ClearPersistentSelection()
    {
        hasTemporaryHighlight = false;
        persistentTemporaryStart = persistentTemporaryEnd = -1;
        Rebuild(Color.clear);
    }

    private void CanMarkMouseUp(FlagType activeFlagType)
    {
        if(!canMark || activeFlagType == null) return;
        FlagData flag = null;

        if(canMark && currentSelectionStart >= 0 && currentSelectionEnd >= 0)
        {
            int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
            int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);
            if(start != end) FlagManager.Instance.AddFlag(chatLog, chatBubble, nodeGuid, start, end);
        }

        currentSelectionStart = currentSelectionEnd = -1;
        Color newFlagColour = flag != null ? flag.ResolvedFlagType.colour : Color.clear;
        Rebuild(newFlagColour);
    }

    private void HighlightMouseUp(FlagType activeFlagType)
    {
        if(canMark && activeFlagType != null) return;
        int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);

        if(start != end)
        {
            hasTemporaryHighlight = true;
            persistentTemporaryStart = start;
            persistentTemporaryEnd = end;
            Rebuild(activeColour);
        }

        currentSelectionStart = currentSelectionEnd = -1;
        return;
    }

    private int GetCurrentCharIndex()
    {
        Camera cam = null;
        Canvas canvas = myText.canvas;
        if(canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera;

        return TMP_TextUtilities.FindIntersectingCharacter(
            myText,
            Mouse.current.position.ReadValue(),
            cam,
            true
        );
    }

    private int GetClosestCharIndex()
    {
        if(Mouse.current == null) return -1;
        Vector2 pointerPos = Mouse.current.position.ReadValue();
        Camera cam = null;
        Canvas canvas = myText.canvas;
        if(canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.worldCamera;
        return TMP_TextUtilities.FindNearestCharacter(myText, pointerPos, cam, true);
    }

    private string GetFlaggedText(Color overrideColor, List<FlagData> flags)
    {
        if(string.IsNullOrEmpty(rawText)) return rawText;

        int selectionStart = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int selectionEnd = Mathf.Max(currentSelectionStart, currentSelectionEnd);
        bool hasActiveSelection = selectionStart >= 0 && selectionEnd > selectionStart;
        int length = rawText.Length;
        var hasMark = new bool[length];
        var markColor = new Color[length];

        if(hasActiveSelection && overrideColor != Color.clear)
        {
            for(int i = selectionStart; i <= selectionEnd && i < length; i++)
            {
                if(i < 0) continue;
                hasMark[i] = true;
                markColor[i] = overrideColor;
            }
        }
        else if(hoveredFlag != null && overrideColor == hoverColour)
        {
            int start = Mathf.Max(0, hoveredFlag.startIndex);
            int end = Mathf.Min(length - 1, hoveredFlag.endIndex);

            for(int i = start; i <= end; i++)
            {
                hasMark[i] = true;
                markColor[i] = overrideColor;
            }
        }
        else if(hasTemporaryHighlight)
        {
            int start = Mathf.Max(0, persistentTemporaryStart);
            int end = Mathf.Min(length - 1, persistentTemporaryEnd);

            for(int i = start; i <= end; i++)
            {
                hasMark[i] = true;
                markColor[i] = activeColour;
            }
        }

        foreach(var flag in flags)
        {
            int start = Mathf.Max(0, flag.startIndex);
            int end = Mathf.Min(length - 1, flag.endIndex);

            for(int i = start; i <= end; i++)
            {
                if(hasMark[i]) continue;
                markColor[i] += flag.ResolvedFlagType.colour;
                hasMark[i] = true;
            }
        }

        if(System.Array.TrueForAll(hasMark, m => !m)) return rawText;
        var sb = new StringBuilder(rawText.Length + 32);
        bool inMark = false;

        for(int i = 0; i < length; i++)
        {
            if(hasMark[i])
            {
                if(!inMark)
                {
                    string tag = ColorUtility.ToHtmlStringRGBA(markColor[i]);
                    sb.Append($"<mark=#{tag}>");
                    inMark = true;
                }
                else if(ColorUtility.ToHtmlStringRGBA(markColor[i]) != ColorUtility.ToHtmlStringRGBA(markColor[i - 1]))
                {
                    sb.Append("</mark>");
                    string tag = ColorUtility.ToHtmlStringRGBA(markColor[i]);
                    sb.Append($"<mark=#{tag}>");
                }
            }
            else if(inMark)
            {
                sb.Append("</mark>");
                inMark = false;
            }

            sb.Append(rawText[i]);
        }

        if(inMark) sb.Append("</mark>");
        return sb.ToString();
    }

    public void Rebuild(Color overrideColor)
    {
        List<FlagData> flags = GetFlags();
        if(flags == null || myText == null ||myText.text == null) return;
        myText.text = GetFlaggedText(overrideColor, flags);
        myText.ForceMeshUpdate();
    }
}