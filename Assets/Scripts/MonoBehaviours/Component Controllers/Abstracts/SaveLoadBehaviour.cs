using UnityEngine;

public abstract class SaveLoadBehaviour : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        if(this is ISavable s) SaveManager.Instance.AddSavable(s);
        if(this is ILoadable l) SaveManager.Instance.AddLoadable(l);
    }
    protected virtual void OnDisable()
    {
        if(this is ISavable s) SaveManager.Instance.RemoveSavable(s);
        if(this is ILoadable l) SaveManager.Instance.RemoveLoadable(l);
    }
}
