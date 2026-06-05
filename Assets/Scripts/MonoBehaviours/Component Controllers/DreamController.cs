using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class DreamController : MonoBehaviour
{
    protected async void EndDream()
    {
        SceneManager.LoadSceneAsync(Constants.SceneNames.Desktop, LoadSceneMode.Single).completed += (asyncOperation) => GameManager.Instance.StartDay();
    }
}
