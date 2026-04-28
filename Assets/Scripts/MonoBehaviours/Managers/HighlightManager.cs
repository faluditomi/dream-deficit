using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HighlightManager : MonoBehaviour
{
    private static HighlightManager _instance;
    private IHighlightable currentHighlightable;
    private IHighlightable previousHighlightable;
    private List<RaycastResult> results = new List<RaycastResult>();
    private PointerEventData pointerData;

    void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if(_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        pointerData = new PointerEventData(EventSystem.current);
    }

    void Update()
    {
        if(Mouse.current == null) return;
        OnMouseHover();
        OnMouseDown();
        OnMouseHeld();
        OnMouseUp();
    }

    private void OnMouseHover()
    {
        if(Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasReleasedThisFrame) return;
        currentHighlightable = GetTopHighlightableUnderMouse();
        if(currentHighlightable != null) currentHighlightable.OnMouseHover();
    }

    private void OnMouseDown()
    {
        if(!Mouse.current.leftButton.wasPressedThisFrame) return;
        var newHighlightable = GetTopHighlightableUnderMouse();

        if(previousHighlightable != null)
        {
            previousHighlightable.ClearPersistentSelection();
        }

        currentHighlightable = newHighlightable;
        previousHighlightable = newHighlightable;
        currentHighlightable?.OnMouseDown();
    }

    private void OnMouseHeld()
    {
        if(!Mouse.current.leftButton.isPressed) return;
        if(currentHighlightable != null) currentHighlightable.OnMouseHeld();
    }

    private void OnMouseUp()
    {
        if(currentHighlightable == null) return;
        if(!Mouse.current.leftButton.wasReleasedThisFrame) return;
        currentHighlightable.OnMouseUp();
        currentHighlightable = null;
    }

    private IHighlightable GetTopHighlightableUnderMouse()
    {
        pointerData.position = Mouse.current.position.ReadValue();
        results.Clear();
        EventSystem.current.RaycastAll(pointerData, results);
        if(results.Count == 0) return null;
        return results[0].gameObject.TryGetComponent<IHighlightable>(out var highlightable) ? highlightable : null;
    }
}