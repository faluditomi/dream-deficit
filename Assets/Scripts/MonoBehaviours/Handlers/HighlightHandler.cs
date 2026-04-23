using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TMP_Text))]
public class HighlightHandler : MonoBehaviour, IPointerDownHandler
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
    private bool isMouseDown = false;
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

    private void Update()
    {
        if(Mouse.current == null) return;


        if(isMouseDown)
        {
            currentSelectionEnd = GetClosestCharIndex();
            Rebuild(activeColour);

            if(Mouse.current.leftButton.wasReleasedThisFrame) OnMouseUp();
        }
        else
        {
            int hoveredCharIndex = GetCurrentCharIndex();
            // NOTE: would it be better if we placed trigger colliders on the markers for hover detection?
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
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnMouseDown();
    }

    public void OnMouseDown()
    {
        isMouseDown = true;
        currentSelectionStart = currentSelectionEnd = GetClosestCharIndex();
    }

    private void OnMouseUp()
    {
        isMouseDown = false;
        currentSelectionEnd = GetClosestCharIndex();
        MarkerData marker = null;
        
        // TODO: "currentSelectionStart == currentSelectionEnd" check can later turn into 
        // "is the user holding down a marker button" check
        if(hoveredMarker != null && currentSelectionStart == currentSelectionEnd)
        {
            MarkerManager.Instance().RemoveMarker(hoveredMarker);
            Rebuild(Color.clear);
            hoveredMarker = previousHoveredMarker = null;
        }

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