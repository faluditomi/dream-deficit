using System;
using UnityEngine;

public abstract class BaseWindowController : MonoBehaviour
{
    private TopBarHandler topBarHandler;
    protected GameObject windowGameObject;
    public event Action<GameObject> OnGainedFocusEvent;
    public event Action<GameObject> OnLostFocusEvent;
    protected bool isOpen = false;
    private bool isTopBarSetup = false;

    protected void SetupTopBar(string windowName)
    {
        SetupTopBar(gameObject, windowName);
    }

    protected void SetupTopBar(GameObject targetWindow, string windowName)
    {
        windowGameObject = targetWindow;
        topBarHandler = windowGameObject.AddComponent<TopBarHandler>();
        topBarHandler.Setup(windowGameObject, windowName, this);
        isTopBarSetup = true;
    }

    public bool GetIsOpen()
    {
        return isOpen;
    }

    public void SetIsOpen(bool isOpen)
    {
        this.isOpen = isOpen;
    }

    public void Open()
    {
        if(!isTopBarSetup) return;
        windowGameObject.transform.SetAsLastSibling();
        topBarHandler.Open();
    }

    public void OnGainedFocus()
    {
        OnGainedFocusEvent?.Invoke(windowGameObject);
    }

    public void OnLostFocus()
    {
        OnLostFocusEvent?.Invoke(windowGameObject);
    }
}
