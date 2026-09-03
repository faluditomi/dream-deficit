using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarHandler : MonoBehaviour
{
    private GameObject myWindow;
    private BaseWindowController myBaseWindowController;
    private NoDragScrollRect myNoDragScrollRect;
    private GameObject topBar;

    private bool isSetUp = false;

    public float TopBarHeight
    {
        get
        {
            if(topBar == null) return 0f;
            RectTransform topBarRectTransform = topBar.GetComponent<RectTransform>();
            return topBarRectTransform != null ? topBarRectTransform.rect.height : 0f;
        }
    }

    public void Setup(GameObject window, string windowName, BaseWindowController baseWindowController, bool isTopBarVisible)
    {
        myWindow = window;
        myBaseWindowController = baseWindowController;
        if(myWindow.GetComponent<NoDragScrollRect>()) myNoDragScrollRect = myWindow.GetComponent<NoDragScrollRect>();

        if(isTopBarVisible)
        {
            GameObject topBarPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.TopBar);
            topBar = Instantiate(topBarPrefab, myWindow.transform);
            topBar.AddComponent<DragHandler>().Setup(transform);
            topBar.transform.Find(Constants.GameObjectNames.CloseButton).GetComponent<Button>().onClick.AddListener(() => Close());
            RectTransform myRectTransform = topBar.GetComponent<RectTransform>();
            myRectTransform.anchoredPosition = new Vector2(0, myRectTransform.rect.height);
            myRectTransform.offsetMin = new Vector2(0, myRectTransform.offsetMin.y);
            myRectTransform.offsetMax = new Vector2(0, myRectTransform.offsetMax.y);
            TMP_Text myWindowNameText = topBar.transform.Find(Constants.GameObjectNames.WindowName).GetComponent<TMP_Text>();
            myWindowNameText.text = windowName;
        }

        isSetUp = true;
    }

    public void Open()
    {
        if(!isSetUp) return;
        myBaseWindowController.SetIsOpen(true);
        if(myNoDragScrollRect != null) myNoDragScrollRect.enabled = true;
        
        // TODO: there has to be a better way
        foreach(Transform transform in transform)
        {
            if(transform.gameObject.name == Constants.GameObjectNames.TypingIndicator) continue;
            transform.gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        if(!isSetUp) return;
        myBaseWindowController.SetIsOpen(false);
        if(myNoDragScrollRect != null) myNoDragScrollRect.enabled = false;
        
        // TODO: there has to be a better way
        foreach(Transform transform in transform)
        {
            transform.gameObject.SetActive(false);
        }
    }
}
