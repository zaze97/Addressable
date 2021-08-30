using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class AddressableWindow : OdinEditorWindow
{
    private static AddressableAssetSettings setting;
    

    [MenuItem("BuildTools/Addressable资源管理")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AddressableWindow)).Show();
        //EditorWindow.GetWindow(typeof(AddressableWindow));
        setting = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(
            "Assets/AddressableAssetsData/AddressableAssetSettings.asset");
    }

    private void OnEnable()
    {
        this.titleContent = new GUIContent("Addressable资源管理");
        isChooseTarget = false;
        version = Application.version;
        setting = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
        url = PlayerPrefs.GetString(setting.profileSettings.GetProfileName(setting.activeProfileId) + "_BuildUrl",
            @"http://[PrivateIpAddress]:[HostingServicePort]");
        environment = (BuildEnvironment) System.Enum.Parse(typeof(BuildEnvironment),
            PlayerPrefs.GetString("BuildEnvironment", BuildEnvironment.Debug.ToString()));
        
        if (!isChooseTarget)
        {
            target = EditorUserBuildSettings.activeBuildTarget;
            isChooseTarget = true;
        }
    }

    private void OnDisable()
    {
        PlayerPrefs.SetString(environment.ToString() + "_BuildUrl", url);
        PlayerPrefs.SetString("BuildEnvironment", environment.ToString());
    }

    #region 自动标记

    public static Dictionary<string, List<string>> addressDic = new Dictionary<string, List<string>>();

    public static void Mark()
    {
        addressDic.Clear();
        ///创建分组
        string loaclRoot = Application.dataPath + "/AddressableAssets/Local";
        string remotedRoot = Application.dataPath + "/AddressableAssets/Remoted";
        DirectoryInfo[] dirs = new DirectoryInfo(loaclRoot).GetDirectories();
        foreach (var info in dirs)
        {
            string group_name = "Local_" + info.Name;
            var group = setting.FindGroup(group_name);
            if (group == null && info.Name != "Atlas")
            {
                group = setting.CreateGroup(group_name, false, false, false,
                    new List<AddressableAssetGroupSchema>
                        {setting.DefaultGroup.Schemas[0], setting.DefaultGroup.Schemas[1]});
            }

            AutoMarkRootAddress("Local", info);
            if (info.Name != "SpriteAtlas" && info.Name != "Atlas")
                AutoMark(info.Name);
        }

        dirs = new DirectoryInfo(remotedRoot).GetDirectories();
        foreach (var info in dirs)
        {
            string group_name = "Remoted_" + info.Name;
            var group = setting.FindGroup(group_name);
            if (group == null && info.Name != "Atlas")
            {
                group = setting.CreateGroup(group_name, false, false, false,
                    new List<AddressableAssetGroupSchema>
                        {setting.DefaultGroup.Schemas[0], setting.DefaultGroup.Schemas[1]});
            }

            AutoMarkRootAddress("Remoted", info);
            if (info.Name != "SpriteAtlas" && info.Name != "Atlas")
                AutoMark(info.Name, false);
        }

        ///自动创建图集
        Debug.Log("开始创建图集");
        AutoCreateSpriteAtlas();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("MarkAsset Successful");
    }

    public static void AutoMark(string name, bool local = true)
    {
        string path = local ? "Local" : "Remoted";
        string root = Application.dataPath + "/AddressableAssets/" + path + "/" + name;
        DirectoryInfo dir = new DirectoryInfo(root);
        DirectoryInfo[] dirs = dir.GetDirectories();
        string group_name = path + "_" + name;
        foreach (var info in dirs)
        {
            string assetPath = "Assets/AddressableAssets/" + path + "/" + name + "/" + info.Name;
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var group = setting.FindGroup(group_name);
            var entry = setting.CreateOrMoveEntry(guid, group);
            List<string> list;
            if (addressDic.TryGetValue(group_name, out list))
            {
                if (list.Contains(info.Name))
                {
                    list.Remove(info.Name);
                }
            }

            group.RemoveAssetEntry(entry);
            mark("Assets/AddressableAssets/" + path + "/" + name, name, info, local);
        }
    }

    private static void mark(string path, string name, DirectoryInfo dir, bool local = true)
    {
        var dirs = dir.GetDirectories();
        if (dirs != null && dirs.Length > 0)
        {
            foreach (var info in dirs)
            {
                mark(path + "/" + dir.Name, name, info, local);
            }
        }

        markFiles(path, name, dir, local);
    }

    private static void markFiles(string path, string name, DirectoryInfo dir, bool local = true)
    {
        var files = dir.GetFiles();
        if (files != null && files.Length > 0)
        {
            foreach (var file in files)
            {
                if (file.Extension != ".meta")
                {
                    int index = file.Name.IndexOf(".");
                    string address = file.Name.Remove(index, file.Name.Length - index);
                    string group_name = local ? "Local_" + name : "Remoted_" + name;
                    string assetPath = path + "/" + dir.Name + "/" + file.Name;
                    List<string> label = new List<string>();
                    string[] allDirs;
                    if (local)
                    {
                        allDirs = (path + "/" + dir.Name)
                            .Replace("Assets/AddressableAssets/Local/" + name + "/", string.Empty).Split('/');
                    }
                    else
                    {
                        allDirs = (path + "/" + dir.Name)
                            .Replace("Assets/AddressableAssets/Remoted/" + name + "/", string.Empty).Split('/');
                    }

                    label.Add(name);
                    for (int i = 0; i < allDirs.Length; i++)
                    {
                        label.Add(allDirs[i]);
                    }

                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var group = setting.FindGroup(group_name);
                    if (group != null)
                    {
                        var entry = setting.CreateOrMoveEntry(guid, group);
                        if (entry.address != address)
                        {
                            entry.SetAddress(address);
                            addAddressInfo(group_name, address + file.Extension);

                            List<string> oldLabels = new List<string>();
                            foreach (var item in entry.labels)
                            {
                                if (!label.Contains(item))
                                    oldLabels.Add(item);
                            }

                            for (int i = 0; i < oldLabels.Count; i++)
                            {
                                entry.SetLabel(oldLabels[i], false);
                                setting.RemoveLabel(oldLabels[i]);
                            }

                            for (int i = 0; i < label.Count; i++)
                            {
                                var _label = label[i];
                                if (!setting.GetLabels().Contains(_label))
                                {
                                    setting.AddLabel(_label);
                                }

                                entry.SetLabel(_label, true);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("分组 = " + group_name + "不存在");
                    }
                }
            }
        }
    }

    public static void AutoMarkRootAddress(string path, DirectoryInfo dir)
    {
        var dirs = dir.GetDirectories();
        var files = dir.GetFiles();
        if (files != null && files.Length > 0)
        {
            foreach (var file in files)
            {
                if (file.Extension != ".meta" && file.Extension != ".spriteatlas")
                {
                    string[] dirSplit = dir.ToString().Split(new string[] {"AddressableAssets"},
                        StringSplitOptions.RemoveEmptyEntries);
                    string address = file.Name;
                    address = (dirSplit[dirSplit.Length - 1]) +"/"+ address;
                    address = address.Substring(1);
                    address = address.Replace("\\", "/");
                    Debug.Log("address：" + address);
                    int index = address.IndexOf(".");
                    address = address.Remove(index, address.Length - index);
                    string group_name = path + "_" + dir.Name;
                    string assetPath = "Assets/AddressableAssets/" + path + "/" + dir.Name + "/" + file.Name;
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var group = setting.FindGroup(group_name);
                    if (group != null)
                    {
                        var entry = setting.CreateOrMoveEntry(guid, group);
                        if (entry.address != address)
                        {
                            entry.SetAddress(address);
                            addAddressInfo(group_name, address);
                            List<string> oldLabels = new List<string>();
                            foreach (var item in entry.labels)
                            {
                                oldLabels.Add(item);
                            }

                            for (int i = 0; i < oldLabels.Count; i++)
                            {
                                entry.SetLabel(oldLabels[i], false);
                                setting.RemoveLabel(oldLabels[i]);
                            }

                            if (!setting.GetLabels().Contains(dir.Name))
                            {
                                setting.AddLabel(dir.Name);
                            }

                            entry.SetLabel(dir.Name, true);
                        }
                    }
                    else
                    {
                        Debug.LogError("分组 = " + group_name + "不存在");
                    }
                }
            }
        }
    }

    #region 图集

    public static void AutoCreateSpriteAtlas()
    {
        DirectoryInfo[] Locadirs = new DirectoryInfo(GlobalPath.AtlasLocalRoot).GetDirectories();
        foreach (var info in Locadirs)
        {
            addSpriteAtlas(GlobalPath.AtlasLocalRoot + "/" + info.Name, GlobalPath.AtlasLocalRoot,
                GlobalPath.SpriteLocalAtlas, "Local_SpriteAtlas", info);
        }

        DirectoryInfo[] Remotedirs = new DirectoryInfo(GlobalPath.AtlasRemotedRoot).GetDirectories();
        foreach (var info in Remotedirs)
        {
            addSpriteAtlas(GlobalPath.AtlasRemotedRoot + "/" + info.Name, GlobalPath.AtlasRemotedRoot,
                GlobalPath.SpriteRemotedAtlas, "Remoted_SpriteAtlas", info);
        }
    }

    /// <summary>
    /// 自动创建图集
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="dir">文件夹</param>
    private static void addSpriteAtlas(string path, string AtlasRoot, string SpriteAtlas, string groupname,
        DirectoryInfo dir)
    {
        var dirs = dir.GetDirectories();
        if (dirs == null || dirs.Length == 0)
        {
            string name = path.Replace(AtlasRoot + "/", string.Empty).Replace("/", "_");
            string filePath = SpriteAtlas + "/" + name + ".spriteatlas";
            
            string[] dirSplit = filePath.Split(new string[] {"AddressableAssets"},StringSplitOptions.RemoveEmptyEntries);
            string address;
            address = (dirSplit[dirSplit.Length - 1]);
            address = address.Substring(1);
            address=address.Split('.')[0];
            address = address.Replace("\\", "/");
            Debug.Log("spriteatlasaddress：" + address);
            
            if (File.Exists(filePath))
            {
                int assetIndex = filePath.IndexOf("Assets");
                string guidPath = filePath.Remove(0, assetIndex);
                var guid = AssetDatabase.AssetPathToGUID(guidPath);
                var group = setting.FindGroup(groupname);
                var entry = setting.CreateOrMoveEntry(guid, group);
                var label = name + ".spriteatlas";

                if (entry.address != address)
                {

                    
                    entry.SetAddress(address);
                    addAddressInfo(groupname, address);
                }

                List<string> oldLabels = new List<string>();
                foreach (var item in entry.labels)
                {
                    if (item != label)
                        oldLabels.Add(item);
                }

                for (int i = 0; i < oldLabels.Count; i++)
                {
                    entry.SetLabel(oldLabels[i], false);
                    setting.RemoveLabel(oldLabels[i]);
                }

                if (!setting.GetLabels().Contains("SpriteAtlas"))
                {
                    setting.AddLabel("SpriteAtlas");
                }

                entry.SetLabel("SpriteAtlas", true);
                if (!setting.GetLabels().Contains(label))
                {
                    setting.AddLabel(label);
                }

                entry.SetLabel(label, true);
                return;
            }
            else
            {
                SpriteAtlas atlas = new SpriteAtlas();
                //设置打包参数
                SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
                {
                    blockOffset = 1,
                    enableRotation = true,
                    enableTightPacking = false,
                    padding = 2,
                };
                atlas.SetPackingSettings(packSetting);

                //设置打包后Texture图集信息
                SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings()
                {
                    readable = false,
                    generateMipMaps = false,
                    sRGB = true,
                    filterMode = FilterMode.Bilinear,
                };
                atlas.SetTextureSettings(textureSettings);

                //设置平台图集大小压缩等信息
                TextureImporterPlatformSettings platformSettings = new TextureImporterPlatformSettings()
                {
                    maxTextureSize = 4096,
                    format = TextureImporterFormat.Automatic,
                    crunchedCompression = true,
                    textureCompression = TextureImporterCompression.Compressed,
                    compressionQuality = 50,
                };
                atlas.SetPlatformSettings(platformSettings);
                int index = filePath.IndexOf("Assets");
                string atlasPath = filePath.Remove(0, index);
                AssetDatabase.CreateAsset(atlas, atlasPath);
                index = path.IndexOf("Assets");
                string spritePath = path.Remove(0, index);
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(spritePath, typeof(UnityEngine.Object));
                atlas.Add(new[] {obj});
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                int assetIndex = filePath.IndexOf("Assets");
                string guidPath = filePath.Remove(0, assetIndex);
                var guid = AssetDatabase.AssetPathToGUID(guidPath);
                var group = setting.FindGroup(groupname);
                var entry = setting.CreateOrMoveEntry(guid, group);
                var label = name + ".spriteatlas";
                if (entry.address != address)
                {

                    
                    entry.SetAddress(address);
                    addAddressInfo(groupname, address);
                }

                List<string> oldLabels = new List<string>();
                foreach (var item in entry.labels)
                {
                    if (item != label)
                        oldLabels.Add(item);
                }

                for (int i = 0; i < oldLabels.Count; i++)
                {
                    entry.SetLabel(oldLabels[i], false);
                    setting.RemoveLabel(oldLabels[i]);
                }

                if (!setting.GetLabels().Contains(label))
                {
                    setting.AddLabel(label);
                }

                entry.SetLabel(label, true);
                if (!setting.GetLabels().Contains("SpriteAtlas"))
                {
                    setting.AddLabel("SpriteAtlas");
                }

                entry.SetLabel("SpriteAtlas", true);
                AssetDatabase.Refresh();
            }
        }
        else
        {
            if (dirs.Length > 0)
            {
                foreach (var info in dirs)
                {
                    addSpriteAtlas(path + "/" + info.Name, AtlasRoot, SpriteAtlas, groupname, info);
                }
            }
        }
    }

    #endregion

    private static void addAddressInfo(string group, string _address)
    {
        List<string> list;
        if (addressDic.TryGetValue(group, out list))
        {
            if (!list.Contains(_address))
            {
                list.Add(_address);
            }
            else
            {
                Debug.LogError("命名重复\n在" + group + "中已经存在" + _address);
            }
        }
        else
        {
            list = new List<string>();
            list.Add(_address);
            addressDic.Add(group, list);
        }
    }

    #endregion

    #region 资源打包
    /// <summary>
    /// 目标平台
    /// </summary>
    private BuildTarget target;
    /// <summary>
    /// 标记是否有打包报错信息
    /// </summary>
    private bool isBuildSuccess = true;

    private bool isChooseTarget = false;
    
    private static Dictionary<string, MD5Info> md5files = new Dictionary<string, MD5Info>();
    private static Dictionary<string, MD5Info> md5Newfiles = new Dictionary<string, MD5Info>();
    
    
    public string GetContentStateDataPath()
    {
        var plat = UnityEngine.AddressableAssets.PlatformMappingService.GetPlatform();
        int index = Application.dataPath.LastIndexOf('/');
        string rootPath = Application.dataPath.Remove(index);
        string[] ver = version.Split('.');
        string name = setting.profileSettings.GetProfileName(setting.activeProfileId);
        string versionPath = ver[0] + "." + ver[1] + "/" + name;
        string targetPath = rootPath + "/BuildAddressableData/" + plat + "/" + versionPath;
        return targetPath + "/addressables_content_state.bin";
    }

    public void CopyBuildData()
    {
        var plat = UnityEngine.AddressableAssets.PlatformMappingService.GetPlatform();
        int index = Application.dataPath.LastIndexOf('/');
        string rootPath = Application.dataPath.Remove(index);
        string sourcePath = rootPath + "/Library/com.unity.addressables/StreamingAssetsCopy/" + plat;
        setting = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(
            "Assets/AddressableAssetsData/AddressableAssetSettings.asset");
        string[] ver = version.Split('.');
        string name = setting.profileSettings.GetProfileName(setting.activeProfileId);
        string versionPath = ver[0] + "." + ver[1] + "/" + name;
        string targetPath = rootPath + "/BuildAddressableData/" + plat + "/" + versionPath;
        BuildTools.CopyDirectory(sourcePath, targetPath, false);
    }

    private void setActiveProfileId()
    {
        var names = setting.profileSettings.GetAllProfileNames();
        if (!names.Contains(environment.ToString()))
        {
            setting.profileSettings.AddProfile(environment.ToString(), setting.activeProfileId);
        }

        var id = setting.profileSettings.GetProfileId(environment.ToString());
        if (setting.activeProfileId != id)
            setting.activeProfileId = id;
        if (environment == BuildEnvironment.Debug)
        {
            setting.BuildRemoteCatalog = false;
        }
        else
        {
            setting.BuildRemoteCatalog = true;
        }

        setting.profileSettings.SetValue(setting.activeProfileId, "LocalBuildPath", GlobalPath.LocalBuildPath);
        setting.profileSettings.SetValue(setting.activeProfileId, "LocalLoadPath", GlobalPath.LocalLoadPath);

        string[] ver = version.Split('.');
        string name = setting.profileSettings.GetProfileName(setting.activeProfileId);
        string buildPath = GlobalPath.RemoteBuildPath + "/" + ver[0] + "." + ver[1] + "/" + name;
        if (setting.profileSettings.GetValueByName(setting.activeProfileId, "RemoteBuildPath") != buildPath)
            setting.profileSettings.SetValue(setting.activeProfileId, "RemoteBuildPath", buildPath);
        //string loadPath = url + "/poetry/" + buildPath;
        string loadPath = url + buildPath;
        if (setting.profileSettings.GetValueByName(setting.activeProfileId, "RemoteLoadPath") != loadPath)
            setting.profileSettings.SetValue(setting.activeProfileId, "RemoteLoadPath", loadPath);
    }

    /// <summary>
    /// 打包参数设置
    /// </summary>
    private void SetParameters()
    {
        if (UnityEditor.PlayerSettings.bundleVersion != version)
            UnityEditor.PlayerSettings.bundleVersion = version;
        if (UnityEditor.PlayerSettings.companyName != "mgc")
            UnityEditor.PlayerSettings.companyName = "mgc";
        if (UnityEditor.PlayerSettings.productName != "Poetry")
            UnityEditor.PlayerSettings.productName = "Poetry";
        if (!UnityEditor.PlayerSettings.allowUnsafeCode)
            UnityEditor.PlayerSettings.allowUnsafeCode = true;
        if (UnityEditor.PlayerSettings.stripEngineCode)
            UnityEditor.PlayerSettings.stripEngineCode = false;
        if (UnityEditor.PlayerSettings.applicationIdentifier != "com.mgc.poetry")
            UnityEditor.PlayerSettings.applicationIdentifier = "com.mgc.poetry";
    }


    /// <summary>
    /// 标记为资源分组
    /// 0 小包，所有资源存放资源服务器
    /// 1 分包 ，Local资源存本地，Remoted资源存资源服务器
    /// 2 整包，所有资源存本地
    /// </summary>
    private void markStatus(int status)
    {
        List<AddressableAssetGroup> deleteList = new List<AddressableAssetGroup>();
        for (int i = 0; i < setting.groups.Count; i++)
        {
            var group = setting.groups[i];
            if (group.name != "Default Local Group" && group.name != "Built In Data")
            {
                if (group.entries.Count <= 0)
                {
                    ///删除没有资源的分组
                    deleteList.Add(group);
                }
                else
                {
                    foreach (var schema in group.Schemas)
                    {
                        if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas
                            .BundledAssetGroupSchema)
                        {
                            bool bundleCrc = true;
                            string buildPath = AddressableAssetSettings.kLocalBuildPath;
                            string loadPath = AddressableAssetSettings.kLocalLoadPath;
                            if (group.name.Contains("Local_"))
                            {
                                bundleCrc = status == 0;
                                buildPath = status == 0
                                    ? AddressableAssetSettings.kRemoteBuildPath
                                    : AddressableAssetSettings.kLocalBuildPath;
                                loadPath = status == 0
                                    ? AddressableAssetSettings.kRemoteLoadPath
                                    : AddressableAssetSettings.kLocalLoadPath;
                            }
                            else if (group.name.Contains("Remoted_"))
                            {
                                bundleCrc = !(status == 2);

                                buildPath = status == 2
                                    ? AddressableAssetSettings.kLocalBuildPath
                                    : AddressableAssetSettings.kRemoteBuildPath;
                                loadPath = status == 2
                                    ? AddressableAssetSettings.kLocalLoadPath
                                    : AddressableAssetSettings.kRemoteLoadPath;
                            }
                            else if (group.name.Contains("UpdateGroup_"))
                            {
                                bundleCrc = true;
                                buildPath = AddressableAssetSettings.kRemoteBuildPath;
                                loadPath = AddressableAssetSettings.kRemoteLoadPath;
                            }

                            var bundledAssetGroupSchema =
                                (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema);
                            bundledAssetGroupSchema.BuildPath.SetVariableByName(group.Settings,
                                buildPath);
                            bundledAssetGroupSchema.LoadPath.SetVariableByName(group.Settings,
                                loadPath);

                            bundledAssetGroupSchema.UseAssetBundleCrc = bundleCrc;
                            bundledAssetGroupSchema.BundleNaming = UnityEditor.AddressableAssets.Settings.GroupSchemas
                                .BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                            bundledAssetGroupSchema.BundleMode = UnityEditor.AddressableAssets.Settings.GroupSchemas
                                .BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
                        }
                        else if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema)
                        {
                            var updateGroupSchema =
                                (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.
                                    ContentUpdateGroupSchema);

                            if (group.name.Contains("Local_"))
                            {
                                updateGroupSchema.StaticContent = !(status == 0);
                            }
                            else if (group.name.Contains("Remoted_"))
                            {
                                updateGroupSchema.StaticContent = (status == 2);
                            }
                            else if (group.name.Contains("UpdateGroup_"))
                            {
                                updateGroupSchema.StaticContent = false;
                            }
                        }
                    }
                }
            }
        }

        for (int i = 0; i < deleteList.Count; i++)
        {
            setting.RemoveGroup(deleteList[i]);
        }
    }

    private void SetMD5Info(bool old = true)
    {
        if (old)
        {
            md5files.Clear();
            md5Newfiles.Clear();
        }
        else
        {
            md5Newfiles.Clear();
        }

        string[] ver = version.Split('.');
        string path = Application.dataPath.Replace("Assets",
            "ServerData/" + BuildTools.Platform + "/" + ver[0] + "." + ver[1] + "/" + environment.ToString());
        if (Directory.Exists(path))
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            md5File(dir, old);
        }

        if (!old)
        {
            if (md5files.Count > 0)
            {
                if (md5Newfiles.Count > 2)
                {
                    string targetPath = Application.dataPath.Replace("Assets",
                        "BuildAssets/Assets/" + BuildTools.Platform + "/" + version + "/" + environment.ToString());
                    foreach (var info in md5Newfiles)
                    {
                        MD5Info md5;
                        if (md5files.TryGetValue(info.Key, out md5))
                        {
                            if (info.Value.MD5 != md5.MD5)
                            {
                                BuildTools.CopyFile(info.Value.AssetPath, targetPath);
                            }
                        }
                        else
                        {
                            BuildTools.CopyFile(info.Value.AssetPath, targetPath);
                        }
                    }
                }
            }
        }

        AssetDatabase.Refresh();
    }

    private static void md5File(DirectoryInfo info, bool old = true)
    {
        FileInfo[] files = info.GetFiles();
        if (files.Length > 0)
        {
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (file == null)
                {
                    continue;
                }

                if (file.Extension == ".meta")
                {
                    continue;
                }

                string FilePath = file.FullName.Replace(@"\", "/");
                MD5Info md5 = new MD5Info();
                md5.AssetPath = FilePath;
                md5.MD5 = BuildTools.CalculateMD5(file.FullName);
                if (old)
                {
                    md5files.Add(md5.AssetPath, md5);
                }
                else
                {
                    if (md5files.ContainsKey(md5.AssetPath))
                    {
                        if (md5files[md5.AssetPath].MD5 != md5.MD5)
                        {
                            md5Newfiles.Add(md5.AssetPath, md5);
                        }
                    }
                    else
                    {
                        md5Newfiles.Add(md5.AssetPath, md5);
                    }
                }
            }
        }

        DirectoryInfo[] dirs = info.GetDirectories();
        if (dirs != null && dirs.Length > 0)
        {
            for (int i = 0; i < dirs.Length; i++)
            {
                md5File(dirs[i]);
            }
        }
    }

    private void build()
    {
        BuildTools.DeleteFolder(Application.streamingAssetsPath);
        string[] outScenes = GetBuildScenes();
        Debug.Log(target);
        BuildPipeline.BuildPlayer(outScenes, BuildTools.OutPath, target, BuildOptions.None);
    }

    private string[] GetBuildScenes()
    {
        List<string> pathList = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                pathList.Add(scene.path);
            }
        }

        return pathList.ToArray();
    }

    private void buildByStatus(int status, bool buildApp = true)
    {
        isBuildSuccess = true;
        BuildTools.ClearConsole();
        Application.logMessageReceived += onLogMessage;
        if (buildApp)
        {
/*            Generator.ClearAll();
            Generator.GenAll();*/
        }

        AssetDatabase.Refresh();
        setActiveProfileId();
        AssetDatabase.Refresh();
        markStatus(status);
        SetMD5Info();
        AddressableAssetSettings.BuildPlayerContent();
        AssetDatabase.Refresh();
        CopyBuildData();
        AssetDatabase.Refresh();
        SetMD5Info(false);
        if (buildApp)
            build();
        Application.logMessageReceived -= onLogMessage;
        if (isBuildSuccess)
        {
            string showMessage = string.Empty;
            if (status == 0)
            {
                showMessage = buildApp ? "打包小包成功" : "打包小包资源完成";
            }
            else if (status == 1)
            {
                showMessage = buildApp ? "打包分包成功" : "打包分包资源完成";
            }
            else if (status == 2)
            {
                showMessage = buildApp ? "打包整包成功" : "打包整包资源完成";
            }

            if (EditorUtility.DisplayDialog(buildApp ? "打包完成" : "打包资源", showMessage, "确定"))
            {
                if (buildApp)
                {
                    EditorUtility.RevealInFinder(BuildTools.OutPath);
                    BuildTools.OutPath = string.Empty;
                }
            }
        }
        else
        {
            if (EditorUtility.DisplayDialog("打包失败", "请检测报错信息", "确定"))
            {
                EditorUtility.RevealInFinder(BuildTools.OutPath);
                BuildTools.OutPath = string.Empty;
            }
        }
    }

    /// <summary>
    /// 更新版本号，需要强制更新
    /// </summary>
    private void updateBuildVersion(bool isAdd = true)
    {
        int index = isAdd ? 1 : -1;
        string[] ver = version.Split('.');
        int intver = int.Parse(ver[1]) + index;
        version = ver[0] + "." + intver + ".0";
        if (intver < 1)
        {
            version = ver[0] + "." + 1 + ".0";
        }

        SetParameters();
    }

    /// <summary>
    /// 更新资源号，不用强制更新安装包
    /// </summary>
    private void updateAssetVersion(bool isAdd = true)
    {
        int index = isAdd ? 1 : -1;
        string[] ver = version.Split('.');
        int intver = int.Parse(ver[2]) + index;
        version = ver[0] + "." + ver[1] + "." + (intver);
        if (intver < 0)
            version = ver[0] + "." + ver[1] + "." + (0);
        SetParameters();
    }
    #endregion
    
    #region 编辑器按钮相关
    [HorizontalGroup("版本信息", 0.1f, LabelWidth = 20)] //设置一个父节点，然后水平排列，然后Box子节点垂直排列
    /// <summary>
    /// 版本信息
    /// </summary>
    [ReadOnly, HideLabel, GUIColor(0, 1, 0), BoxGroup("版本信息/版本信息"), PropertySpace(SpaceAfter = 20)]
    public string version = string.Empty;

    /// <summary>
    /// 远程资源地址
    /// </summary>
    [HideLabel, BoxGroup("版本信息/远端链接", true, true), PropertySpace(SpaceAfter = 20)]
    public string url;

    [HideLabel, BoxGroup("发布信息", true, true), EnumToggleButtons, OnValueChanged(nameof(AlterEnvironment))]
    public BuildEnvironment environment;

    private void AlterEnvironment()
    {
        var oldenvironment = environment;
        //environment = (BuildEnvironment) EditorGUILayout.EnumPopup("Build Environment", environment);
        setActiveProfileId();
        if (oldenvironment != environment)
        {
            PlayerPrefs.SetString(oldenvironment.ToString() + "_BuildUrl", url);
            url = PlayerPrefs.GetString(environment.ToString() + "_BuildUrl",
                @"http://[PrivateIpAddress]:[HostingServicePort]");
            oldenvironment = environment;
        }
    }


    [HorizontalGroup("版本相关", 0.5f)] //设置一个父节点，然后水平排列，然后Box子节点垂直排列  
    [BoxGroup("版本相关/项目版本", true, true), ButtonGroup("版本相关/项目版本/Button"), Button("+", ButtonSizes.Large)]
    private void AddProject()
    {
        updateBuildVersion(true);
    }

    [BoxGroup("版本相关/项目版本", true, true), ButtonGroup("版本相关/项目版本/Button"), Button("-", ButtonSizes.Large)]
    private void SubProject()
    {
        updateBuildVersion(false);
    }

    [BoxGroup("版本相关/资源版本", true, true), ButtonGroup("版本相关/资源版本/Button"), Button("+", ButtonSizes.Large)]
    //[HorizontalGroup("Change/ver"),Button("+",ButtonSizes.Large)]
    private void AddAsset()
    {
        updateAssetVersion(true);
    }

    [BoxGroup("版本相关/资源版本", true, true), ButtonGroup("版本相关/资源版本/Button"), Button("-", ButtonSizes.Large)]
    private void SubAsset()
    {
        updateAssetVersion(false);
    }

    [HorizontalGroup("Change", 0.5f)] //设置一个父节点，然后水平排列，然后Box子节点垂直排列  
    [Title("检查更新")]
    [BoxGroup("Change/版本相关", true, true), Button("手动更新", ButtonSizes.Large)]
    private void ManualUpdateProject()
    {
        string buildPath = ContentUpdateScript.GetContentStateDataPath(true);
        Debug.Log("buildPath = " + buildPath);
        var m_Settings = AddressableAssetSettingsDefaultObject.Settings;
        List<AddressableAssetEntry> entrys =
            ContentUpdateScript.GatherModifiedEntries(
                m_Settings, buildPath);
        if (entrys.Count == 0) return;
        StringBuilder sbuider = new StringBuilder();
        sbuider.AppendLine("Need Update Assets:");
        foreach (var _ in entrys)
        {
            sbuider.AppendLine(_.address);
        }

        Debug.Log(sbuider.ToString());
        string[] ver = version.Split('.');
        version = ver[0] + "." + ver[1] + "." + (int.Parse(ver[2]) + 1).ToString();
        //将被修改过的资源单独分组
        var groupName = string.Format("UpdateGroup_{0}", version);
        ContentUpdateScript.CreateContentUpdateGroup(m_Settings, entrys, groupName);
        AssetDatabase.Refresh();
        var updateGroup = setting.FindGroup(groupName);
        if (updateGroup != null)
        {
            foreach (var schema in updateGroup.Schemas)
            {
                if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas
                    .BundledAssetGroupSchema)
                {
                    var bundledAssetGroupSchema =
                        (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema);
                    bundledAssetGroupSchema.BuildPath.SetVariableByName(updateGroup.Settings,
                        AddressableAssetSettings.kRemoteBuildPath);
                    bundledAssetGroupSchema.LoadPath.SetVariableByName(updateGroup.Settings,
                        AddressableAssetSettings.kRemoteLoadPath);
                    bundledAssetGroupSchema.BundleMode = UnityEditor.AddressableAssets.Settings.GroupSchemas
                        .BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
                    bundledAssetGroupSchema.BundleNaming = UnityEditor.AddressableAssets.Settings.GroupSchemas
                        .BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                }
            }
        }
    }

    [BoxGroup("Change/版本相关", true, true), Button("自动更新", ButtonSizes.Large), PropertySpace(SpaceAfter = 49)]
    private void AutomaticUpdateProject()
    {
        string buildPath = GetContentStateDataPath();
        Debug.Log("buildPath = " + buildPath);
        var m_Settings = AddressableAssetSettingsDefaultObject.Settings;
        List<AddressableAssetEntry> entrys =
            ContentUpdateScript.GatherModifiedEntries(
                m_Settings, buildPath);
        if (entrys.Count == 0) return;
        StringBuilder sbuider = new StringBuilder();
        sbuider.AppendLine("Need Update Assets:");
        foreach (var _ in entrys)
        {
            sbuider.AppendLine(_.address);
        }

        Debug.Log(sbuider.ToString());
        string[] ver = version.Split('.');
        version = ver[0] + "." + ver[1] + "." + (int.Parse(ver[2]) + 1).ToString();
        //将被修改过的资源单独分组
        var groupName = string.Format("UpdateGroup_{0}", version);
        ContentUpdateScript.CreateContentUpdateGroup(m_Settings, entrys, groupName);
        AssetDatabase.Refresh();
        var updateGroup = setting.FindGroup(groupName);
        if (updateGroup != null)
        {
            foreach (var schema in updateGroup.Schemas)
            {
                if (schema is UnityEditor.AddressableAssets.Settings.GroupSchemas
                    .BundledAssetGroupSchema)
                {
                    var bundledAssetGroupSchema =
                        (schema as UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema);
                    bundledAssetGroupSchema.BuildPath.SetVariableByName(updateGroup.Settings,
                        AddressableAssetSettings.kRemoteBuildPath);
                    bundledAssetGroupSchema.LoadPath.SetVariableByName(updateGroup.Settings,
                        AddressableAssetSettings.kRemoteLoadPath);
                    bundledAssetGroupSchema.BundleMode = UnityEditor.AddressableAssets.Settings.GroupSchemas
                        .BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
                    bundledAssetGroupSchema.BundleNaming = UnityEditor.AddressableAssets.Settings.GroupSchemas
                        .BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                }
            }
        }
    }

    [Title("增量更新"), PropertySpace(SpaceBefore = 2)]
    [BoxGroup("Change/版本相关", true, true), Button("手动更新", ButtonSizes.Large)]
    private void ManualIncrement()
    {
        isBuildSuccess = true;
        BuildTools.ClearConsole();
        Application.logMessageReceived += onLogMessage;
        AssetDatabase.Refresh();
        SetMD5Info();
        AssetDatabase.Refresh();
        string path = ContentUpdateScript.GetContentStateDataPath(true);
        Debug.Log(path);
        var m_Settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressablesPlayerBuildResult result =
            ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, path);
        Debug.Log("BuildFinish path = " + m_Settings.RemoteCatalogBuildPath.GetValue(m_Settings));
        AssetDatabase.Refresh();
        CopyBuildData();
        AssetDatabase.Refresh();
        SetMD5Info(false);
        AssetDatabase.Refresh();
        if (isBuildSuccess)
            EditorUtility.DisplayDialog("增量打包", "增量打包成功", "确定");
        else
            EditorUtility.DisplayDialog("增量打包资源失败", "请检查报错信息", "确定");
        Application.logMessageReceived -= onLogMessage;
        AssetDatabase.Refresh();
    }

    [BoxGroup("Change/版本相关", true, true), Button("自动更新", ButtonSizes.Large),
     PropertySpace(SpaceBefore = 9, SpaceAfter = 9)]
    private void AutomaticIncrement()
    {
        isBuildSuccess = true;
        BuildTools.ClearConsole();
        Application.logMessageReceived += onLogMessage;
        SetMD5Info();
        AssetDatabase.Refresh();
        var path = GetContentStateDataPath();
        Debug.Log(path);
        var m_Settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressablesPlayerBuildResult result =
            ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, path);
        Debug.Log("BuildFinish path = " + m_Settings.RemoteCatalogBuildPath.GetValue(m_Settings));
        AssetDatabase.Refresh();
        CopyBuildData();
        AssetDatabase.Refresh();
        SetMD5Info(false);
        AssetDatabase.Refresh();
        if (isBuildSuccess)
            EditorUtility.DisplayDialog("增量打包", "增量打包成功", "确定");
        else
            EditorUtility.DisplayDialog("增量打包资源失败", "请检查报错信息", "确定");
        Application.logMessageReceived -= onLogMessage;
        AssetDatabase.Refresh();
    }

    [BoxGroup("Change/版本相关", true, true), ButtonGroup("Change/版本相关/Button"), Button("标记打包文件", ButtonSizes.Large)]
    //[HorizontalGroup("Change/ver"),Button("+",ButtonSizes.Large)]
    private void AddMark()
    {
        Mark();
        EditorUtility.DisplayDialog("自动标记", "自动标记成功", "确定");
    }

    [BoxGroup("Change/版本相关", true, true), ButtonGroup("Change/版本相关/Button"), Button("清理标记文件", ButtonSizes.Large)]
    private void RemMark()
    {
        BuildTools.DeleteFolder(Application.dataPath + "/AddressableAssets/Local/SpriteAtlas");
        BuildTools.DeleteFolder(Application.dataPath + "/AddressableAssets/Remoted/SpriteAtlas");
        AssetDatabase.Refresh();
        List<string> name =BuildTools.GetFolder();
        for (int i = 0; i < name.Count; i++)
        {
            if (!name[i].Contains("Local_Atlas") || !name[i].Contains("Remoted_Atlas"))
            {
                var group=setting.FindGroup(name[i]);
                setting.RemoveGroup(group);
            }
        }

        EditorUtility.DisplayDialog("清理图集成功", "图集清理", "确定");
     
    }


    [Title("设置")]
    [BoxGroup("Change/打包相关", true, true, 2), Button("设置为整包", ButtonSizes.Large)]
    private void SettingBig()
    {
        markStatus(2);
        EditorUtility.DisplayDialog("设置成功", "设置为整包", "确定");
    }

    [BoxGroup("Change/打包相关", true, true, 2), Button("设置为分包", ButtonSizes.Large), PropertySpace(SpaceAfter = 1)]
    private void SettingMiddle()
    {
        markStatus(1);
        EditorUtility.DisplayDialog("设置成功", "设置为分包", "确定");
    }

    [BoxGroup("Change/打包相关", true, true, 2), Button("设置为小包", ButtonSizes.Large), PropertySpace(SpaceAfter = 1)]
    private void SettingSmall()
    {
        SetMD5Info(false);
        markStatus(0);
        EditorUtility.DisplayDialog("设置成功", "设置为小包", "确定");
    }

    [Title("打包资源")]
    [BoxGroup("Change/打包相关", true, true, 2), Button("打包整包资源", ButtonSizes.Large), PropertySpace(SpaceAfter = 1)]
    private void BuildBig()
    {
        buildByStatus(2, false);
    }

    [BoxGroup("Change/打包相关", true, true, 2), Button("打包分包资源", ButtonSizes.Large), PropertySpace(SpaceAfter = 1)]
    private void BuildMiddle()
    {
        buildByStatus(1, false);
    }

    [BoxGroup("Change/打包相关", true, true, 2), Button("打包小包资源", ButtonSizes.Large), PropertySpace(SpaceAfter = 1)]
    private void BuildSmall()
    {
        buildByStatus(0, false);
    }


    [BoxGroup("Build", true, true), ButtonGroup("Build/Button"), Button("整包", ButtonSizes.Large), GUIColor(0, 1, 0)]
    private void BulidBig()
    {
        buildByStatus(2);
    }

    [BoxGroup("Build", true, true), ButtonGroup("Build/Button"), Button("分包", ButtonSizes.Large), GUIColor(0, 1, 0)]
    private void BulidMiddle()
    {
        buildByStatus(1);
    }

    [BoxGroup("Build", true, true), ButtonGroup("Build/Button"), Button("小包", ButtonSizes.Large), GUIColor(0, 1, 0)]
    private void BulidSmall()
    {
        buildByStatus(0);
    }

    [BoxGroup("Build", true, true), ButtonGroup("Build/Button"), Button("直接出包", ButtonSizes.Large), GUIColor(0, 0, 1)]
    private void BulidGlobal()
    {
        isBuildSuccess = true;
        BuildTools.ClearConsole();
        Application.logMessageReceived += onLogMessage;
        AssetDatabase.Refresh();
        build();
        AssetDatabase.Refresh();
        if (isBuildSuccess)
        {
            if (EditorUtility.DisplayDialog("一键打包完成", "一键打包完成", "确定"))
            {
                EditorUtility.RevealInFinder(BuildTools.OutPath);
                BuildTools.OutPath = string.Empty;
            }
        }
        else
        {
            if (EditorUtility.DisplayDialog("打包失败", "请检测报错信息", "确定"))
            {
                EditorUtility.RevealInFinder(BuildTools.OutPath);
                BuildTools.OutPath = string.Empty;
            }
        }

        Application.logMessageReceived -= onLogMessage;
    }
    #endregion
    private void onLogMessage(string condition, string StackTrace, LogType type)
    {
        if (type == LogType.Error)
        {
            if (condition != "EndLayoutGroup: BeginLayoutGroup must be called first.")
                isBuildSuccess = false;
        }
    }


}