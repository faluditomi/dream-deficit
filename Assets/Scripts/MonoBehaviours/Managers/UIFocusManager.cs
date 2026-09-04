using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class UIFocusManager : Singleton<UIFocusManager>
{
    public BaseWindowController focusedWindow;
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
            // check if the clicked object is a window and if it's a complex window, take the top-most parent window
            BaseWindowController clickedWindow = lastClickedObject.GetComponentsInParent<BaseWindowController>().LastOrDefault(focusedWindow => focusedWindow != null);
            
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

    public void SetFocusedWindow(BaseWindowController window)
    {
        if(focusedWindow == window) return;
        focusedWindow?.OnLostFocus();
        focusedWindow = window;
        focusedWindow?.OnGainedFocus();
        // bring the focused window to the front of the Canvas
        if(focusedWindow != null) focusedWindow.transform.SetAsLastSibling();
    }

    public void ClearFocusedWindow()
    {
        focusedWindow?.OnLostFocus();
        focusedWindow = null;
    }
}