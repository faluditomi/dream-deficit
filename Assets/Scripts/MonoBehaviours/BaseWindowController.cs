using UnityEngine;

public abstract class BaseWindowController : MonoBehaviour
{
    private TopBarHandler topBarHandler;
    private PointerHandler pointerHandler;
    protected GameObject windowGameObject;
    protected bool isOpen = false;
    private bool isTopBarSetup = false;

    protected void SetupTopBar()
    {
        SetupTopBar(gameObject);
    }

    protected void SetupTopBar(GameObject targetWindow)
    {
        windowGameObject = targetWindow;
        topBarHandler = windowGameObject.AddComponent<TopBarHandler>();
        topBarHandler.Setup(windowGameObject, this);
        pointerHandler = windowGameObject.AddComponent<PointerHandler>();
        pointerHandler.OnPointerDownEvent += (pointerData) => windowGameObject.transform.SetAsLastSibling();
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

    private void OnDestroy()
    {
        if(pointerHandler)
        {
            pointerHandler.OnPointerDownEvent -= (pointerData) => windowGameObject.transform.SetAsLastSibling();
        }
    }
}
