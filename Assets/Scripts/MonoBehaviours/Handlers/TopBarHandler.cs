using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TopBarHandler : MonoBehaviour
{
    private GameObject myWindow;
    private BaseWindowController myBaseWindowController;

    private bool isSetUp = false;

    public void Setup(GameObject window, BaseWindowController baseWindowController)
    {
        myWindow = window;
        myBaseWindowController = baseWindowController;
        GameObject topBarPrefab = AddressableManager.Instance.RetrieveAddressable<GameObject>(Constants.AddressablePrefabs.TopBar);
        GameObject topBar = Instantiate(topBarPrefab, myWindow.transform);
        topBar.AddComponent<DragHandler>().Setup(transform);
        topBar.transform.Find(Constants.GameObjectNames.CloseButton).GetComponent<Button>().onClick.AddListener(() => Close());
        RectTransform myRectTransform = topBar.GetComponent<RectTransform>();
        myRectTransform.anchoredPosition = new Vector2(0, myRectTransform.rect.height);
        myRectTransform.offsetMin = new Vector2(0, myRectTransform.offsetMin.y);
        myRectTransform.offsetMax = new Vector2(0, myRectTransform.offsetMax.y);
        isSetUp = true;
    }

    public void Open()
    {
        if(!isSetUp) return;
        myBaseWindowController.SetIsOpen(true);
        
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

        // TODO: there has to be a better way
        foreach(Transform transform in transform)
        {
            transform.gameObject.SetActive(false);
        }
    }
}
