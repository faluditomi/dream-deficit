using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TMP_Text))]
public class HighlightHandler : MonoBehaviour, IHighlightable
{
    public Color hoverColour = Color.red;
    public Color activeColour = Color.lightBlue;
    public TMP_Text myText;
    private ChatLog chatLog;
    private ChatBubble chatBubble;
    private MarkerData previousHoveredMarker;
    private MarkerData hoveredMarker;

    public bool canMark;
    private bool hasPersistentHighlight = false;
    private int persistentSelectionStart = -1;
    private int persistentSelectionEnd = -1;
    private int currentSelectionStart = -1;
    private int currentSelectionEnd = -1;

    private void Awake()
    {
        myText = GetComponent<TMP_Text>();
    }

    public void Init(ChatLog chatLog, ChatBubble chatBubble, bool canTag)
    {
        this.chatLog = chatLog;
        this.chatBubble = chatBubble;
        this.canMark = canTag;
    }

    public void OnMouseHover()
    {
        int hoveredCharIndex = GetCurrentCharIndex();
        // NOTE: would it be better if we placed trigger colliders on the markers for hover detection?
        var markers = MarkerManager.Instance().GetMarkersForChatBubble(chatBubble);
        List<MarkerData> overlapping = markers.FindAll(m => hoveredCharIndex >= m.startIndex && hoveredCharIndex <= m.endIndex);

        if(overlapping.Count > 0 && MarkerManager.Instance().activeMarkerType == null)
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
            Rebuild(previousHoveredMarker.markerType.colour);
            previousHoveredMarker = hoveredMarker = null;
        }
    }

    public void OnMouseDown()
    {
        currentSelectionStart = currentSelectionEnd = GetClosestCharIndex();
    }

    public void OnMouseHeld()
    {
        currentSelectionEnd = GetClosestCharIndex();
        Rebuild(activeColour);
    }

    public void OnMouseUp()
    {
        MarkerType activeMarkerType = MarkerManager.Instance().activeMarkerType;
        currentSelectionEnd = GetClosestCharIndex();

        if(hoveredMarker != null && currentSelectionStart == currentSelectionEnd && activeMarkerType == null)
        {
            MarkerManager.Instance().RemoveMarker(hoveredMarker);
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
        hasPersistentHighlight = false;
        persistentSelectionStart = persistentSelectionEnd = -1;
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
                marker = new MarkerData(
                    activeMarkerType,
                    chatLog,
                    chatBubble,
                    start,
                    end
                );

                MarkerManager.Instance().AddMarker(marker);
            }
        }

        currentSelectionStart = currentSelectionEnd = -1;
        Color newMarkerColour = marker != null ? marker.markerType.colour : Color.clear;
        Rebuild(newMarkerColour);
    }

    private void HighlightMouseUp(MarkerType activeMarkerType)
    {
        if(canMark && activeMarkerType != null) return;
        int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);

        if(start != end)
        {
            hasPersistentHighlight = true;
            persistentSelectionStart = start;
            persistentSelectionEnd = end;
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

    public void Rebuild(Color overrideColor)
    {
        myText.ForceMeshUpdate();
        TMP_TextInfo textInfo = myText.textInfo;
        var markers = MarkerManager.Instance().GetMarkersForChatBubble(chatBubble);
        int selectionStart = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int selectionEnd = Mathf.Max(currentSelectionStart, currentSelectionEnd);
        bool hasActiveSelection = selectionStart >= 0 && selectionEnd > selectionStart;

        for(int i = 0; i < textInfo.characterCount; i++)
        {
            if(!textInfo.characterInfo[i].isVisible) continue;
            Color finalColor = myText.color;

            // Active selection preview
            if(hasActiveSelection && i >= selectionStart && i <= selectionEnd)
            {
                finalColor = overrideColor;
            }
            else if(hasPersistentHighlight && i >= persistentSelectionStart && i <= persistentSelectionEnd)
            {
                finalColor = activeColour;
            }
            else
            {
                // Existing markers
                List<MarkerData> overlapping = markers.FindAll(m => i >= m.startIndex && i <= m.endIndex);

                if(overlapping.Count > 0)
                {
                    Color blended = Color.clear;

                    foreach(var m in overlapping) blended += m.markerType.colour;

                    blended.r = Mathf.Clamp01(blended.r);
                    blended.g = Mathf.Clamp01(blended.g);
                    blended.b = Mathf.Clamp01(blended.b);
                    blended.a = 1f;

                    finalColor = blended;
                }
            }

            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;
            var colors = textInfo.meshInfo[matIndex].colors32;

            colors[vertIndex + 0] = finalColor;
            colors[vertIndex + 1] = finalColor;
            colors[vertIndex + 2] = finalColor;
            colors[vertIndex + 3] = finalColor;
        }

        for(int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            myText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}