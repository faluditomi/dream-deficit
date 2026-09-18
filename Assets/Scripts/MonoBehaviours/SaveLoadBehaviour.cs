using UnityEngine;

/// <summary>
/// Inherited by all scripts that derive from BaseWindowController or Singleton.
/// If the inheriting script implements any of the ILoadable or ISavable interfaces, this
/// makes sure they get added to SaveManager's loadable and savable lists, so they get
/// prompted to load/save.
/// The Save System should only be interacted with through inheriting this class.
/// </summary>
public abstract class SaveLoadBehaviour : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        if(this is IDaySavable ds) SaveManager.AddDaySavable(ds);
        if(this is IDayLoadable dl) SaveManager.AddDayLoadable(dl);
        if(this is IRunSavable rs) SaveManager.AddRunSavable(rs);
        if(this is IRunLoadable rl) SaveManager.AddRunLoadable(rl);
    }

    protected virtual void OnDisable()
    {
        if(this is IDaySavable ds) SaveManager.RemoveDaySavable(ds);
        if(this is IDayLoadable dl) SaveManager.RemoveDayLoadable(dl);
        if(this is IRunSavable rs) SaveManager.RemoveRunSavable(rs);
        if(this is IRunLoadable rl) SaveManager.RemoveRunLoadable(rl);
    }
}
