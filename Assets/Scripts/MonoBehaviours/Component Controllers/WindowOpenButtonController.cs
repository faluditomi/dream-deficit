using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A centralised controller for buttons with which we want to open windows. It can be configured in the editor by 
/// placing it on a button and assigning the prefab we want to instantiate along with a transform to parent it to.
/// It can also be assigned from code. windowPrefab is required, but windowContainer is optional. If the windowContainer
/// is not assigned, it will default to the first Canvas found in the scene and its child named "WindowContainer".
/// Only thing we have to watch out for is that windowPrefab should have a script that is, or derives from, BaseWindowController.
/// This script is not suitable for opening ChatLogs.
/// </summary>
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
