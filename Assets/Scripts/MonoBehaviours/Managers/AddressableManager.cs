using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

public class AddressableManager : Singleton<AddressableManager>
{
    public async Task<T> RetrieveAddressable<T>(string address)
    {
        if(string.IsNullOrWhiteSpace(address)) return default;
        var handle = Addressables.LoadAssetAsync<T>(address);
        await handle.Task;
        return handle.Result;
    }
}
