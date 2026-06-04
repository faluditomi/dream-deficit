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
    private MarkerData previousHoveredMarker;
    private MarkerData hoveredMarker;

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

    public void Setup(ChatLog chatLog, ChatBubble chatBubble, bool canMark)
    {
        this.chatLog = chatLog;
        this.chatBubble = chatBubble;
        this.canMark = canMark;
    }

    public void OnMouseHover()
    {
        int hoveredCharIndex = GetCurrentCharIndex();
        var markers = MarkerManager.Instance.GetMarkersForChatBubble(chatBubble);
        List<MarkerData> overlapping = markers.FindAll(m => hoveredCharIndex >= m.startIndex && hoveredCharIndex <= m.endIndex);

        if(overlapping.Count == 0 && MarkerManager.Instance.activeMarkerType == null)
        {
            overlapping = FindMarkerInBoundsArea(markers);
        }

        if(overlapping.Count > 0 && MarkerManager.Instance.activeMarkerType == null)
        {
            hoveredMarker = overlapping[overlapping.Count - 1];

            if(previousHoveredMarker != hoveredMarker)
            {
                currentSelectionStart = hoveredMarker.startIndex;
                currentSelectionEnd = hoveredMarker.endIndex;
                previousHoveredMarker = hoveredMarker;
                Rebuild(hoverColour);
            }
        }
        else
        {
            if(previousHoveredMarker == null) return;
            currentSelectionStart = previousHoveredMarker.startIndex;
            currentSelectionEnd = previousHoveredMarker.endIndex;
            Rebuild(previousHoveredMarker.ResolvedMarkerType.colour);
            previousHoveredMarker = hoveredMarker = null;
        }
    }

    private List<MarkerData> FindMarkerInBoundsArea(List<MarkerData> markers)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        List<MarkerData> markersContainingPoint = new List<MarkerData>();

        foreach(var marker in markers)
        {
            if(IsPointInMarkerBounds(marker, mousePos))
            {
                markersContainingPoint.Add(marker);
            }
        }

        return markersContainingPoint;
    }

    private bool IsPointInMarkerBounds(MarkerData marker, Vector2 point)
    {
        myText.ForceMeshUpdate();
        TMP_TextInfo textInfo = myText.textInfo;
        if(textInfo == null || textInfo.characterCount == 0) return false;
        int start = Mathf.Max(0, marker.startIndex);
        int end = Mathf.Min(textInfo.characterCount - 1, marker.endIndex);
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
        var activeMarkerType = MarkerManager.Instance.activeMarkerType;

        if(hoveredMarker != null && currentSelectionStart == currentSelectionEnd && activeMarkerType == null)
        {
            Rebuild(hoverColour);
            return;
        }

        Color highlightColour = activeMarkerType != null ? activeMarkerType.colour : activeColour;
        Rebuild(highlightColour);
    }

    public void OnMouseUp()
    {
        MarkerType activeMarkerType = MarkerManager.Instance.activeMarkerType;
        currentSelectionEnd = GetClosestCharIndex();

        if(currentSelectionStart == currentSelectionEnd && activeMarkerType == null)
        {
            MarkerManager.Instance.RemoveMarker(hoveredMarker);
            Rebuild(Color.clear);
            hoveredMarker = previousHoveredMarker = null;
        }
        else
        {
            HighlightMouseUp(activeMarkerType);
        }

        CanMarkMouseUp(activeMarkerType);
    }
    
    public void ClearPersistentSelection()
    {
        hasTemporaryHighlight = false;
        persistentTemporaryStart = persistentTemporaryEnd = -1;
        Rebuild(Color.clear);
    }

    private void CanMarkMouseUp(MarkerType activeMarkerType)
    {
        if(!canMark || activeMarkerType == null) return;
        MarkerData marker = null;

        if(canMark && currentSelectionStart >= 0 && currentSelectionEnd >= 0)
        {
            int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
            int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);

            if(start != end)
            {
                MarkerManager.Instance.AddMarker(chatLog, chatBubble, start, end);
            }
        }

        currentSelectionStart = currentSelectionEnd = -1;
        Color newMarkerColour = marker != null ? marker.ResolvedMarkerType.colour : Color.clear;
        Rebuild(newMarkerColour);
    }

    private void HighlightMouseUp(MarkerType activeMarkerType)
    {
        if(canMark && activeMarkerType != null) return;
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

    private string GetMarkedText(Color overrideColor, List<MarkerData> markers)
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
        else if(hoveredMarker != null && overrideColor == hoverColour)
        {
            int start = Mathf.Max(0, hoveredMarker.startIndex);
            int end = Mathf.Min(length - 1, hoveredMarker.endIndex);

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

        foreach(var marker in markers)
        {
            int start = Mathf.Max(0, marker.startIndex);
            int end = Mathf.Min(length - 1, marker.endIndex);

            for(int i = start; i <= end; i++)
            {
                if(hasMark[i]) continue;
                markColor[i] += marker.ResolvedMarkerType.colour;
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
        var markers = MarkerManager.Instance.GetMarkersForChatBubble(chatBubble);
        myText.text = GetMarkedText(overrideColor, markers);
        myText.ForceMeshUpdate();
    }
}