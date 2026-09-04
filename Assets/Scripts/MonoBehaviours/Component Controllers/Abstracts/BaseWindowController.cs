using System;
using UnityEngine;

public abstract class BaseWindowController : MonoBehaviour
{
    private TopBarHandler topBarHandler;
    protected GameObject windowGameObject;
    public event Action<GameObject, int> OnGainedFocusEvent;
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

    public virtual void WindowSpecificSetup(Transform initialiser)
    {
        // This can be implemented by inheriting window scripts in case they want to do some extra setup.
        // It has to be called by the script that initialises the window that wants to use it.
        // It was created so that the ChatClientWindow can set up it's own MessageNotificationController.
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
        int unreadMessages = 0;
        if(this is ChatLogController chatLogController) unreadMessages = chatLogController.unreadMessages;
        OnGainedFocusEvent?.Invoke(windowGameObject, unreadMessages);
    }

    public void OnLostFocus()
    {
        OnLostFocusEvent?.Invoke(windowGameObject);
    }
}
