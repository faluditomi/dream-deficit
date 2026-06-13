using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

    public List<T> RetrieveAddressablesByLabel<T>(string label)
    {
        if(string.IsNullOrWhiteSpace(label)) return default;
        var handle = Addressables.LoadAssetsAsync<T>(label);
        IList<T> loadedAddressables = handle.WaitForCompletion();
        List<T> retrievedAssets = new List<T>();

        if(handle.Status == AsyncOperationStatus.Succeeded)
        {
            foreach(var addressable in loadedAddressables)
            {
                if(addressable != null) retrievedAssets.Add(addressable);
            }
        }
        else
        {
            Debug.LogError($"EventSequenceManager: Failed to load addressables with label: {label}. {handle.OperationException}");
        }

        return retrievedAssets;
    }
}
