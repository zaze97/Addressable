using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager<T> : MonoBehaviour
{
    private Dictionary<string, AsyncOperationHandle> caches = new Dictionary<string, AsyncOperationHandle>();

    public void LoadAsset<T>(string address, Action<T> onComplete, Action onFailed = null) where T : UnityEngine.Object
    {
        if (caches.ContainsKey(address))
        {
            var handle = this.caches[address];
            if (handle.IsDone)
            {
                if (onComplete != null)
                {
                    onComplete(caches[address].Result as T);
                }
            }
            else
            {
                handle.Completed += (result) =>
                {
                    if (result.Status == AsyncOperationStatus.Succeeded)
                    {
                        var obj = result.Result as T;
                        if (onComplete != null)
                        {
                            onComplete(obj);
                        }
                    }
                    else
                    {
                        if (onFailed != null)
                        {
                            onFailed();
                        }

                        Debug.LogError("Load " + address + " failed!");
                    }
                };
            }
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            handle.Completed += (result) =>
            {
                if (result.Status == AsyncOperationStatus.Succeeded)
                {
                    var obj = result.Result as T;
                    if (onComplete != null)
                    {
                        onComplete(obj);
                    }
                }
                else
                {
                    if (onFailed != null)
                    {
                        onFailed();
                    }

                    Debug.LogError("Load " + address + " failed!");
                }
            };
            caches.Add(address, handle);
        }
    }

    private void AddCompleted<T>(AsyncOperationHandle handle,Action<T> onComplete, Action onFailed = null)where T : UnityEngine.Object
    {
        handle.Completed += (result) =>
        {
            if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                var obj = result.Result as T;
                if (onComplete != null)
                {
                    onComplete(obj);
                }
            }
            else
            {
                if (onFailed != null)
                {
                    onFailed();
                }

                Debug.LogError("Load "  + " failed!");
            }
        };
    }
}