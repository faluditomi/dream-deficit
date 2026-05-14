using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static Transform _systemsRoot;

    private static bool _isQuitting;

    public static T Instance
    {
        get
        {
            if(_isQuitting)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Instance requested while application is quitting.");
                return null;
            }

            if(_instance == null)
            {
                // First try to find an existing instance in the scene
                _instance = FindFirstObjectByType<T>();

                // Create one if none exists
                if(_instance == null)
                {
                    CreateInstance();
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Debug.LogWarning($"Duplicate singleton of type {typeof(T).Name} detected on {gameObject.name}. Destroying duplicate component.");
            Destroy(this);
            return;
        }

        _instance = this as T;
        EnsureSystemsRootExists();

        // Parent under Systems root if not already parented
        if(transform.parent != _systemsRoot)
        {
            transform.SetParent(_systemsRoot);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private static void CreateInstance()
    {
        EnsureSystemsRootExists();
        GameObject managerObject = new GameObject(typeof(T).Name);
        managerObject.transform.SetParent(_systemsRoot);
        _instance = managerObject.AddComponent<T>();
    }

    private static void EnsureSystemsRootExists()
    {
        if(_systemsRoot != null)
        {
            return;
        }

        GameObject systemsObject = GameObject.Find("Systems");

        if(systemsObject == null)
        {
            systemsObject = new GameObject("Systems");
            DontDestroyOnLoad(systemsObject);
        }

        _systemsRoot = systemsObject.transform;
    }
}