using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressableManager : MonoBehaviour
{
    private static AddressableManager _instance;

    public static AddressableManager Instance()
    {
        if(_instance == null)
        {
            var obj = new GameObject("AddressableController");
            _instance = obj.AddComponent<AddressableManager>();
            DontDestroyOnLoad(obj);
        }

        return _instance;
    }

    public async Task<T> RetrieveAddressable<T>(string address)
    {
        if(string.IsNullOrWhiteSpace(address)) return default;
        var handle = Addressables.LoadAssetAsync<T>(address);
        await handle.Task;
        return handle.Result;
    }
}
