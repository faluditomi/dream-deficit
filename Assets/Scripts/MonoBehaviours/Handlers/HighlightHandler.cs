using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class HighlightHandler : MonoBehaviour, IPointerDownHandler,IPointerMoveHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler
{
    public Color hoverColour = Color.red;
    public Color activeColour = Color.lightBlue;
    public TMP_Text myText;
    private ChatLog chatLog;
    private ChatBubble chatBubble;
    private MarkerData previousHoveredMarker;
    private MarkerData hoveredMarker;

    private int currentSelectionStart = -1;
    private int currentSelectionEnd = -1;
    public bool canTag;

    private void Awake()
    {
        myText = GetComponent<TMP_Text>();
    }

    public void Init(ChatLog chatLog, ChatBubble chatBubble, bool canTag)
    {
        this.chatLog = chatLog;
        this.chatBubble = chatBubble;
        this.canTag = canTag;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentSelectionStart = GetCharIndex(eventData);
        currentSelectionEnd = currentSelectionStart;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        int hoveredCharIndex = GetCharIndex(eventData);
        var markers = MarkerManager.Instance().GetMarkersForChatBubble(chatBubble);
        List<MarkerData> overlapping = markers.FindAll(m => hoveredCharIndex >= m.startIndex && hoveredCharIndex <= m.endIndex);

        if(overlapping.Count > 0)
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

    public void OnDrag(PointerEventData eventData)
    {
        currentSelectionEnd = GetCharIndex(eventData);
        Rebuild(activeColour);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        int endIndex = GetCharIndex(eventData);
        currentSelectionEnd = endIndex;
        MarkerData marker = null;

        if(canTag && currentSelectionStart >= 0 && currentSelectionEnd >= 0)
        {
            int start = Mathf.Min(currentSelectionStart, currentSelectionEnd);
            int end = Mathf.Max(currentSelectionStart, currentSelectionEnd);

            if(start != end)
            {
                // TODO: adding default marker type for now
                marker = new MarkerData(
                    Markers.CopyPasteSpam,
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if(hoveredMarker == null) return;
        MarkerManager.Instance().RemoveMarker(hoveredMarker);
        Rebuild(Color.clear);
        hoveredMarker = previousHoveredMarker = null;
    }

    private int GetCharIndex(PointerEventData eventData)
    {
        return TMP_TextUtilities.FindIntersectingCharacter(
            myText,
            eventData.position,
            eventData.pressEventCamera,
            true
        );
    }

    public void Rebuild(Color overrideColor)
    {
        myText.ForceMeshUpdate();
        TMP_TextInfo textInfo = myText.textInfo;
        var markers = MarkerManager.Instance().GetMarkersForChatBubble(chatBubble);
        int selectionStart = Mathf.Min(currentSelectionStart, currentSelectionEnd);
        int selectionEnd = Mathf.Max(currentSelectionStart, currentSelectionEnd);

        for(int i = 0; i < textInfo.characterCount; i++)
        {
            if(!textInfo.characterInfo[i].isVisible) continue;
            Color finalColor = myText.color;

            // Active selection preview
            if(selectionStart >= 0 && i >= selectionStart && i <= selectionEnd)
            {
                finalColor = overrideColor;
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