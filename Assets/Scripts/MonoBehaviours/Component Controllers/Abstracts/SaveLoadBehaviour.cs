using UnityEngine;

public abstract class SaveLoadBehaviour : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        if(this is IDaySavable ds) SaveManager.Instance.AddDaySavable(ds);
        if(this is IDayLoadable dl) SaveManager.Instance.AddDayLoadable(dl);
        if(this is IRunSavable rs) SaveManager.Instance.AddRunSavable(rs);
        if(this is IRunLoadable rl) SaveManager.Instance.AddRunLoadable(rl);
    }

    protected virtual void OnDisable()
    {
        if(this is IDaySavable ds) SaveManager.Instance.RemoveDaySavable(ds);
        if(this is IDayLoadable dl) SaveManager.Instance.RemoveDayLoadable(dl);
        if(this is IRunSavable rs) SaveManager.Instance.RemoveRunSavable(rs);
        if(this is IRunLoadable rl) SaveManager.Instance.RemoveRunLoadable(rl);
    }
}
