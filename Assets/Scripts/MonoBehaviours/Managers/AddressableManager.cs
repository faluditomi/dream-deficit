using UnityEngine.AddressableAssets;

public class AddressableManager : Singleton<AddressableManager>
{
    // TODO: this had to be turned synchronous in order to avoid race conditions in other scripts, but this is not
    //       a long term solution. we'll have to load assets asynchronously during off times like between the start
    //       of the day and the dream before
    public T RetrieveAddressable<T>(string address)
{
    if(string.IsNullOrWhiteSpace(address)) return default;
    var handle = Addressables.LoadAssetAsync<T>(address);
    return handle.WaitForCompletion();
}
}
