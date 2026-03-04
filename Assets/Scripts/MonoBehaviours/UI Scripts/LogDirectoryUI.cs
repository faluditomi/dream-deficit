using UnityEngine;
using UnityEngine.UI;

public class LogDirectoryUI : MonoBehaviour
{
    private LogDirectoryController logDirectoryController;

    private bool isSetUp = false;

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        if(isSetUp || !FindElements()) return;

        Transform content = GetComponentInChildren<ContentSizeFitter>().transform;
        logDirectoryController.Setup(content);

        isSetUp = true;
    }

    private bool FindElements()
    {
        logDirectoryController = GetComponent<LogDirectoryController>();
        
        if(logDirectoryController  == null)
        {
            Debug.LogError("Setup of LogDirectory failed. A necessary component wasn't found during setup.");
            return false;
        }

        return true;
    }
}
