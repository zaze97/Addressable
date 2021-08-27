using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
