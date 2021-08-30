using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 记录所有资源MD5信息
/// </summary>
public class MD5Info
{
    public string AssetPath;
    public string MD5;
    public int Size;

    /// <summary>
    /// 下载时使用，记录已经下载了的大小
    /// </summary>
    public int LoadSize = 0;
}

public enum BuildEnvironment
{
    // Local,
    Debug,
    Release
}
public class GlobalPath {

    public  static string AtlasLocalRoot
    {
        get
        {
            return Application.dataPath + "/AddressableAssets/Local/Atlas";
        }
    }
    public static string SpriteLocalAtlas
    {
        get
        {
            return Application.dataPath + "/AddressableAssets/Local/SpriteAtlas";
        }
    }
    public static string AtlasRemotedRoot
    {
        get
        {
            return Application.dataPath + "/AddressableAssets/Remoted/Atlas";
        }
    }
    public static string SpriteRemotedAtlas
    {
        get
        {
            return Application.dataPath + "/AddressableAssets/Remoted/SpriteAtlas";
        }
    }

    public static string LocalBuildPath
    {
        get
        {
            return "AssetsBundle/LocalData/[BuildTarget]";
            //return "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]";
        }
    }
    public static string LocalLoadPath
    {
        get
        {
            return "Assets/StreamingAssets/AssetsBundle/LocalData/[BuildTarget]";
            //return "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/AssetsBundle/LocalData/[BuildTarget]";
        }
    }
    public static string RemoteBuildPath
    {
        get
        {
            return "AssetsBundle/ServerData/[BuildTarget]";
        }
    }
    public static string RemoteLoadPath
    {
        get
        {
            return "AssetsBundle/ServerData/[BuildTarget]";
        }
    }
}
