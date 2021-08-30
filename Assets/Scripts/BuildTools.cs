using UnityEngine;
using System.IO;
using System;
using System.Text;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using System.Reflection;
public static class BuildTools
{
    /// <remarks>目标平台</remarks>
    public static string Platform
    {
        get
        {
            string platform = string.Empty;
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android: platform = "Android"; break;
                case BuildTarget.iOS: platform = "Ios"; break;
                case BuildTarget.StandaloneWindows64: platform = "StandaloneWindows64"; break;
                case BuildTarget.StandaloneWindows: platform = "Windows"; break;
                default: platform = "AssetBundles"; break;
            }
            return platform;
        }
    }

    public static string AssetPath
    {
        get
        {
            return Application.dataPath + "/AddressableAssets";
        }
    }


    private static string _outPath = string.Empty;

    /// <remarks>输出路径</remarks>
    public static string OutPath
    {
        get
        {
            if (string.IsNullOrEmpty(_outPath))
            {
                _outPath = getOutPath();
            }
            return _outPath;
        }
        set
        {
            _outPath = string.Empty;
        }
    }

    public static string AssetBundleChangePath
    {
        get
        {
            int index = Application.dataPath.LastIndexOf("/");
            return Application.dataPath.Replace("Assets", "BuildAssetsUpdate/" + BuildTools.Platform);
        }
    }

    private static string getOutPath()
    {
        int index = Application.dataPath.LastIndexOf("/");
        var time = System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm");
        var setting = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
        string environment = setting.profileSettings.GetProfileName(setting.activeProfileId);
        if (BuildTools.Platform == "Android")
        {
            return Application.dataPath.Replace("Assets", "BuildAssets/Packages/" + BuildTools.Platform + "/" + environment + "/" + Application.version + "/Android_" + environment + "_" + time + ".apk");
        }
        else if (BuildTools.Platform == "Ios")
        {
            return Application.dataPath.Replace("Assets", "BuildAssets/Packages/" + BuildTools.Platform + "/" + environment);
        }
        return Application.dataPath.Replace("Assets", "BuildAssets/Packages/" + BuildTools.Platform + "/" + environment + "/" + Application.version + "/" + environment + "_" + time);
    }

    /// <remarks>计算MD5</remarks>
    public static string CalculateMD5(string file)
    {
        string result = "";
        FileStream fs = new FileStream(file, FileMode.Open);
        try
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(fs);
            fs.Close();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            result = sb.ToString();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("md5file() fail, error:" + e.Message);
        }
        finally
        {
            fs.Close();
        }
        return result;
    }

    /// <remarks>文件复制</remarks>
    public static void CopyFile(string filePath, string targetPath)
    {
        FileInfo file = new FileInfo(filePath);
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        if (file != null)
        {
            string tempPath = Path.Combine(targetPath, file.Name);
            file.CopyTo(tempPath, true);//如果文件存在则覆盖
        }
    }
    /// <summary>
    /// 复制文件夹所有文件
    /// </summary>
    /// <param name="sourcePath">源目录</param>
    /// <param name="destPath">目的目录</param>
    public static void CopyFolder(string sourcePath, string destPath)
    {
        if (Directory.Exists(sourcePath))
        {
            if (!Directory.Exists(destPath))
            {
                //目标目录不存在则创建
                try
                {
                    Directory.CreateDirectory(destPath);
                }
                catch (Exception ex)
                {
                    throw new Exception("创建目标目录失败：" + ex.Message);
                }
            }
            //获得源文件下所有文件
            List<string> files = new List<string>(Directory.GetFiles(sourcePath));
            files.ForEach(c =>
            {
                string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                File.Copy(c, destFile, true);//覆盖模式
            });
            //获得源文件下所有目录文件
            List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));
            folders.ForEach(c =>
            {
                string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                //采用递归的方法实现
                CopyFolder(c, destDir);
            });
 
        }
    }
    /// <remarks>文件夹复制</remarks>
    public static void CopyDirectory(string sourcePath, string targetPath, bool containSubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourcePath);
        if (!dir.Exists)
        {
            return;
        }
        DirectoryInfo[] dirs = dir.GetDirectories();
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(targetPath, file.Name);
            file.CopyTo(tempPath, true);//如果文件存在则覆盖
        }
        if (containSubDirs)
        {
            foreach (var subDir in dirs)
            {
                string tempPath = Path.Combine(targetPath, subDir.Name);
                CopyDirectory(subDir.FullName, tempPath, containSubDirs);
            }
        }

    }

    /// <remarks>删除指定的文件夹</remarks>
    public static void DeleteFolder(string path)
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        if (dir.Exists)
        {
            FileInfo[] files = dir.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                files[i].Delete();
                files[i] = null;
            }
            files = null;
            var dirs = dir.GetDirectories();
            for (int i = 0; i < dirs.Length; i++)
            {
                Directory.Delete(dirs[i].FullName, true);
            }
        }
    }
    /// <summary>
    /// 返回说有文件夹的组合名字
    /// </summary>
    public static List<string> GetFolder()
    {
        List<string> name=new List<string>();
        //获取指定路径下的内容
        string filePath = Application.dataPath+"/AddressableAssets";
        string LocalPath = filePath + "/Local";
        string RemotedPath = filePath + "/Remoted";
        DirectoryInfo Localfolder= new DirectoryInfo(LocalPath);
        DirectoryInfo Remotedfolder= new DirectoryInfo(RemotedPath);
        //遍历文件夹
        foreach (DirectoryInfo folder in Localfolder.GetDirectories())
        {
            string[] foldernames=folder.ToString().Split('\\');
            string foldername = foldernames[foldernames.Length - 1];
            name.Add("Local_"+foldername);
        }
        foreach (DirectoryInfo folder in Remotedfolder.GetDirectories())
        {
            string[] foldernames=folder.ToString().Split('\\');
            string foldername = foldernames[foldernames.Length - 1];
            name.Add("Remoted_"+foldername);
        }
        return name;
    }

    public static void ClearConsole()
    {
        Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
        System.Type logEntries = assembly.GetType("UnityEditor.LogEntries");
        var method = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        if (method != null)
        {
            method.Invoke(null, null);
            GUIUtility.keyboardControl = 0;
        }
        else
        {
            Debug.LogWarning("Can't find clear method");
        }
        AssetDatabase.Refresh();
    }
}
#endif