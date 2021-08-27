using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager : MonoBehaviour
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
                AddCompleted<T>(address,handle, onComplete, onFailed);
                // handle.Completed += (result) =>
                // {
                //     if (result.Status == AsyncOperationStatus.Succeeded)
                //     {
                //         var obj = result.Result as T;
                //         if (onComplete != null)
                //         {
                //             onComplete(obj);
                //         }
                //     }
                //     else
                //     {
                //         if (onFailed != null)
                //         {
                //             onFailed();
                //         }
                //
                //         Debug.LogError("Load " + address + " failed!");
                //     }
                // };
            }
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            AddCompleted<T>(address,handle, onComplete, onFailed);
            // handle.Completed += (result) =>
            // {
            //     if (result.Status == AsyncOperationStatus.Succeeded)
            //     {
            //         var obj = result.Result as T;
            //         if (onComplete != null)
            //         {
            //             onComplete(obj);
            //         }
            //     }
            //     else
            //     {
            //         if (onFailed != null)
            //         {
            //             onFailed();
            //         }
            //
            //         Debug.LogError("Load " + address + " failed!");
            //     }
            // };
            caches.Add(address, handle);
        }
    }

    private void AddCompleted<T>(string address,AsyncOperationHandle handle,Action<T> onComplete, Action onFailed = null)where T : UnityEngine.Object
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

                Debug.LogErrorFormat("读取地址为-【{0}】-的ab包资源失败!",address);
            }
        };
    }
}