using System;
using UnityEngine;

public abstract class BaseWindowController : MonoBehaviour
{
    private TopBarHandler topBarHandler;
    protected GameObject windowGameObject;
    public event Action<GameObject> OnGainedFocusEvent;
    public event Action<GameObject> OnLostFocusEvent;
    protected bool isOpen = false;
    protected bool isTopBarVisible = false;

    protected void SetupBaseWindow(string windowName, bool isTopBarVisible = true)
    {
        SetupBaseWindow(gameObject, windowName, isTopBarVisible);
    }

    protected void SetupBaseWindow(GameObject targetWindow, string windowName, bool isTopBarVisible = true)
    {
        windowGameObject = targetWindow;
        topBarHandler = windowGameObject.AddComponent<TopBarHandler>();
        topBarHandler.Setup(windowGameObject, windowName, this, isTopBarVisible);

        if(isTopBarVisible)
        {
            GameObject windowShadowPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.WindowShadow);
            GameObject windowShadowInstance = Instantiate(windowShadowPrefab, windowGameObject.transform);
            windowShadowInstance.transform.SetAsFirstSibling();
            float topBarHeight = topBarHandler.TopBarHeight;
            RectTransform windowShadowRect = windowShadowInstance.GetComponent<RectTransform>();
            windowShadowRect.offsetMax = new Vector2(windowShadowRect.offsetMax.x, windowShadowRect.offsetMax.y + topBarHeight);
        }
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
        windowGameObject.transform.SetAsLastSibling();
        OnGainedFocus();
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
