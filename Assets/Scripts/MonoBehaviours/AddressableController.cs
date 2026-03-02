using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressableController : MonoBehaviour
{
    private static AddressableController _instance;

    public static AddressableController Instance()
    {
        if(_instance == null)
        {
            var obj = new GameObject("AddressableController");
            _instance = obj.AddComponent<AddressableController>();
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
