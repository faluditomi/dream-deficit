using UnityEngine;

public abstract class DreamController : MonoBehaviour
{
    protected async void EndDream()
    {
        StartCoroutine(GameManager.Instance.LoadSceneBehaviour(Constants.SceneNames.Desktop, () => GameManager.Instance.StartDay()));
    }
}
