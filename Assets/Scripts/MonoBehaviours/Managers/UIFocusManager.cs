using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class UIFocusManager : Singleton<UIFocusManager>
{
    public BaseWindowController FocusedWindow;
    public GameObject lastClickedObject;

    private void Update()
    {
        if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) EvaluateClick();
    }

    private void EvaluateClick()
    {
        if(EventSystem.current == null) return;
        // create pointer data at the current mouse position
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();
        // raycast against all UI elements
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if(results.Count > 0)
        {
            // the top-most UI element hit by the raycast
            lastClickedObject = results[0].gameObject;
            // check if the clicked object (or any of its parents) is a tracked window
            BaseWindowController clickedWindow = lastClickedObject.GetComponentInParent<BaseWindowController>();
            
            if(clickedWindow != null)
            {
                SetFocusedWindow(clickedWindow);
            }
            else
            {
                // clicked a UI element, but NOT a window
                ClearFocusedWindow();
            }
        }
        else
        {
            // clicked empty space (no UI raycast hit)
            lastClickedObject = null;
            ClearFocusedWindow();
        }
    }

    private void SetFocusedWindow(BaseWindowController window)
    {
        if (FocusedWindow == window) return;
        FocusedWindow?.OnLostFocus();
        FocusedWindow = window;
        FocusedWindow?.OnGainedFocus();
        // bring the focused window to the front of the Canvas
        if(FocusedWindow != null) FocusedWindow.transform.SetAsLastSibling();
    }

    public void ClearFocusedWindow()
    {
        FocusedWindow?.OnLostFocus();
        FocusedWindow = null;
    }
}