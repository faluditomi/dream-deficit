using UnityEngine;
using UnityEngine.UI;

public class WindowOpenButtonController : MonoBehaviour
{
    public GameObject windowPrefab;
    public Transform windowContainer;

    // TODO: if we put this script on a button from code (like for files/folders), can we parameterise it before this runs?
    public void Start()
    {
        if(windowPrefab == null)
        {
            Debug.LogError("WindowOpenButtonController: Cannot run without a valid windowPrefab.");
            Destroy(this);
            return;
        }

        if(windowContainer == null) windowContainer = FindFirstObjectByType<Canvas>().transform.Find(Constants.GameObjectNames.WindowContainer);
        GameObject newWindowGameObject = Instantiate(windowPrefab, windowContainer);

        if(newWindowGameObject == null || newWindowGameObject.GetComponent<BaseWindowController>() == null)
        {
            Debug.LogError("WindowOpenButtonController: " + windowPrefab.name + " didn't instantiate or is not a valid BaseWindowController.");
            Destroy(newWindowGameObject);
            Destroy(this);
            return;
        }

        BaseWindowController baseWindowController = newWindowGameObject.GetComponent<BaseWindowController>();
        GetComponent<Button>().onClick.AddListener(() => baseWindowController.Open());
    }
}
