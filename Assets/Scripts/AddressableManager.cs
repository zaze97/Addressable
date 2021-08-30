using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager : MonoBehaviour
{
    private Dictionary<string, AsyncOperationHandle> nameCaches = new Dictionary<string, AsyncOperationHandle>();
    private Dictionary<string, GameObject> InstantiateCaches = new Dictionary<string, GameObject>();
    /// <summary>
    /// 读取指定的Addressable类型的数据
    /// </summary>
    /// <param name="address">路径</param>
    /// <param name="onComplete">执行成功的回调</param>
    /// <param name="onFailed">执行失败的回调</param>
    public void LoadAsset<T>(string addressName, Action<T> onComplete, Action onFailed = null) where T : UnityEngine.Object
    {
        if (nameCaches.ContainsKey(addressName))
        {
            var handle = this.nameCaches[addressName];
            if (handle.IsDone)
            {
                if (onComplete != null)
                {
                    onComplete(nameCaches[addressName].Result as T);
                }
            }
            else
            {
                AddCompleted<T>(addressName,handle, onComplete, onFailed);
            }
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<T>(addressName);
            AddCompleted<T>(addressName,handle, onComplete, onFailed);
            nameCaches.Add(addressName, handle);
        }
    }
    /// <summary>
    /// 生成指定的Addressable类型的预设体
    /// </summary>
    /// <param name="address">名字</param>
    /// <param name="onComplete">执行成功的回调</param>
    /// <param name="onFailed">执行失败的回调</param>
    public GameObject InstantiateAsset(string addressName, Action<GameObject> onComplete, Action onFailed = null) 
    {
        if (InstantiateCaches.ContainsKey(addressName))
        {
            return this.InstantiateCaches[addressName];
        }
        else
        {
            var handle = Addressables.InstantiateAsync(addressName);
            AddCompleted(addressName,handle, onComplete, onFailed);
            InstantiateCaches.Add(addressName, handle.Result);
            return handle.Result;
        }
    }
    /// <summary>
    ///  读取指定的Addressable类型的名字或标签数据的集合
    /// </summary>
    /// <param name="address">名字</param>
    /// <param name="onComplete">单个执行成功的回调</param>
    /// <param name="allOnComplete">全部执行成功的回调</param>
    /// <param name="onFailed">执行失败的回调</param>
    /// <typeparam name="T"></typeparam>
    public void LoadTagAsset<T>(string addressTag, Action<T> onComplete, Action<T> allOnComplete, Action onFailed = null)where T : UnityEngine.Object
    {
        if (nameCaches.ContainsKey(addressTag))
        {
            var handle = this.nameCaches[addressTag];
            if (handle.IsDone)
            {
                if (onComplete != null)
                {
                    onComplete(nameCaches[addressTag].Result as T);
                }
            }
            else
            {
                AddCompleted(addressTag,handle, allOnComplete, onFailed);
            }
        }
        else
        {
     
            var handle = Addressables.LoadAssetsAsync(addressTag,onComplete);
            AddTagCompleted(addressTag,handle, allOnComplete, onFailed);
            nameCaches.Add(addressTag, handle);
        }
    }
    private void AddTagCompleted<T>(string addressTag,AsyncOperationHandle handle,Action<T> allOnComplete, Action onFailed = null)where T : UnityEngine.Object
    {
        handle.Completed += (result) =>
        {
            if (result.Status == AsyncOperationStatus.Succeeded)
            {
                var obj = result.Result as T;
                if (allOnComplete != null)
                {
                    allOnComplete(obj);
                }
            }
            else
            {
                if (onFailed != null)
                {
                    onFailed();
                }

                Debug.LogErrorFormat("读取地址为-【{0}】-的ab包资源失败!",addressTag);
            }
        };
    }
    
    private void AddCompleted<T>(string addressName,AsyncOperationHandle handle,Action<T> onComplete, Action onFailed = null)where T : UnityEngine.Object
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

                Debug.LogErrorFormat("读取地址为-【{0}】-的ab包资源失败!",addressName);
            }
        };
    }

    private void LoadRelease(string name)
    {
        if (nameCaches.ContainsKey(name))
        {
            Addressables.Release( nameCaches[name]); 
            nameCaches.Remove(name);
        }
    }
    private void InstantiateRelease(string name)
    {
        if (InstantiateCaches.ContainsKey(name))
        {
            Addressables.ReleaseInstance(InstantiateCaches[name]); 
            InstantiateCaches.Remove(name);
        }
    }
    private void OnDestroy()
    {
        for (int i = 0; i < nameCaches.Count; i++)
        {
            KeyValuePair<string, AsyncOperationHandle> kv = nameCaches.ElementAt(i);
            LoadRelease(kv.Key);
        }

        for (int i = 0; i < InstantiateCaches.Count; i++)
        {
            KeyValuePair<string, GameObject> kv = InstantiateCaches.ElementAt(i);
            InstantiateRelease(kv.Key);
        }
    }
}