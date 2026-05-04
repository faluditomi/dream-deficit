using UnityEngine;
using UnityEngine.UI;

public class TopBarHandler : MonoBehaviour
{
    private GameObject myWindow;
    private ITopBar myWindowInterface;

    private bool isSetUp = false;

    public async void Setup(GameObject window, ITopBar windowInterface)
    {
        myWindow = window;
        myWindowInterface = windowInterface;
        GameObject topBarPrefab = await AddressableManager.Instance().RetrieveAddressable<GameObject>(Constants.AddressablePaths.TopBarPrefab);
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
        myWindowInterface.SetIsOpen(true);
        myWindow.SetActive(true);
    }

    public void Close()
    {
        if(!isSetUp) return;
        myWindowInterface.SetIsOpen(false);
        myWindow.SetActive(false);
    }
}
