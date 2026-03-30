using System;
using System.Text;
using DGame;
using UnityEngine;

namespace Launcher
{
    [Serializable]
    public class TextDefine
    {
        // 本地资源版本号存储Key
        public string Game_Version_Key = "GAME_VERSION";

        // InitPackageProcedure
        public string Init_Package_Error_Tips =
            "请检查StreamingAssets/package/DefaultPackage/PackageManifest_DefaultPackage.version是否存在";
        public string Init_Package_Failed_Tips = "资源初始化失败！";
        public string Init_Package_Failed_Try_Again_Tips = "资源初始化失败！点击确认重试 \n \n <color=#FF0000>原因: {0}</color>";
        public string Init_Package_Retry_Tips = "重新初始化资源中...";

        // InitResourceProcedure
        public string Init_Resource_Tips = "初始化资源中...";
        public string Init_Resource_Update_Manifest_Tips = "更新清单文件...";
        public string Init_Resource_Error_Try_Again_Tips = "获取远程版本失败！点击确认重试\n <color=#FF0000>原因: {0}</color>";
        public string Init_Resource_Error_Try_Again_WebGL_Tips = "初始化资源失败！点击确认重试\n <color=#FF0000>原因: {0}</color>";
        public string Init_Resource_Network_Error_Try_Again_WebGL_Tips = "当前网络不可用，请检查本地网络设置后点击确认进行重试";
        public string Init_Resource_No_Find_Local_Record_Tips = "没有找到本地版本记录，需要更新资源！";
        public string Init_Resource_Have_Notice_No_Force_Update_Tips = "检测到可选资源更新，推荐完成更新提升游戏体验";
        public string Init_Resource_Have_Notice_No_Force_Update_Suggestion_Tips = "更新失败，检测到可选资源更新，推荐完成更新提升游戏体验！ \n \n 确定再试一次，取消进入游戏";

        // PreloadProcedure
        public string Preload_Loading_Tips = "正在载入...{0}%";
        public string Preload_Loading_Success_Tips = "载入完成";

        // CreateDownloaderProcedure
        public string Create_Downloader_Begin_Tips = "创建补丁下载器...";
        public string Create_Downloader_Total_Download_files_Tips = "总共找到了 {0} 个补丁文件, 总大小为 {1}MB";

        // DownloadFileProcedure
        public string Download_File_Begin_Tips = "开始下载更新文件...";
        public string Download_File_Error_Tips = "下载补丁文件失败: {0}";
        public string Download_File_Update_Line1_Tips = "正在更新，已更新 {0}/{1} ({2:F2}%)";
        public string Download_File_Update_Line2_Tips = "已更新大小 {0}MB/{1}MB";
        public string Download_File_Update_Line3_Tips = "当前网速 {0}/s，剩余时间 {1}";

        // DownloadOverProcedure
        public string Download_Over_Tips = "下载完成...";

        // ClearCacheProcedure
        public string Clear_Cache_Begin_Tips = "清理未使用的缓存文件...";
        public string Clear_Cache_Over_Tips = "清理完成 即将进入游戏...";
    }

    public class UpdateUIDefine : TextDefine
    {
        private static UpdateUIDefine m_instance;
        public static UpdateUIDefine Instance => m_instance != null ? m_instance : m_instance = new UpdateUIDefine();

        public void InitConfigData(TextAsset config)
        {
            if (config == null)
            {
                DLogger.Warning("没有找到相关的热更文本配置文件，将使用默认的");
                return;
            }

            TextDefine textDefine = JsonUtility.FromJson<TextDefine>(config.text);

            if (textDefine == null)
            {
                DLogger.Warning("加载的配置文件无法转换成热更文本配置数据，将使用默认的");
                return;
            }

            // 本地资源版本号存储Key
            Game_Version_Key = textDefine.Game_Version_Key;

            // InitPackageProcedure
            Init_Package_Error_Tips = textDefine.Init_Package_Error_Tips;
            Init_Package_Failed_Tips = textDefine.Init_Package_Failed_Tips;
            Init_Package_Failed_Try_Again_Tips = textDefine.Init_Package_Failed_Try_Again_Tips;
            Init_Package_Retry_Tips = textDefine.Init_Package_Retry_Tips;

            // InitResourceProcedure
            Init_Resource_Tips = textDefine.Init_Resource_Tips;
            Init_Resource_Update_Manifest_Tips = textDefine.Init_Resource_Update_Manifest_Tips;
            Init_Resource_Error_Try_Again_Tips = textDefine.Init_Resource_Error_Try_Again_Tips;
            Init_Resource_Error_Try_Again_WebGL_Tips = textDefine.Init_Resource_Error_Try_Again_WebGL_Tips;
            Init_Resource_Network_Error_Try_Again_WebGL_Tips =
                textDefine.Init_Resource_Network_Error_Try_Again_WebGL_Tips;
            Init_Resource_No_Find_Local_Record_Tips = textDefine.Init_Resource_No_Find_Local_Record_Tips;
            Init_Resource_Have_Notice_No_Force_Update_Tips = textDefine.Init_Resource_Have_Notice_No_Force_Update_Tips;
            Init_Resource_Have_Notice_No_Force_Update_Suggestion_Tips =
                textDefine.Init_Resource_Have_Notice_No_Force_Update_Suggestion_Tips;

            // PreloadProcedure
            Preload_Loading_Tips = textDefine.Preload_Loading_Tips;
            Preload_Loading_Success_Tips = textDefine.Preload_Loading_Success_Tips;

            // CreateDownloaderProcedure
            Create_Downloader_Begin_Tips = textDefine.Create_Downloader_Begin_Tips;
            Create_Downloader_Total_Download_files_Tips = textDefine.Create_Downloader_Total_Download_files_Tips;

            // DownloadFileProcedure
            Download_File_Begin_Tips = textDefine.Download_File_Begin_Tips;
            Download_File_Error_Tips = textDefine.Download_File_Error_Tips;
            Download_File_Update_Line1_Tips = textDefine.Download_File_Update_Line1_Tips;
            Download_File_Update_Line2_Tips = textDefine.Download_File_Update_Line2_Tips;
            Download_File_Update_Line3_Tips = textDefine.Download_File_Update_Line3_Tips;

            // DownloadOverProcedure
            Download_Over_Tips = textDefine.Download_Over_Tips;

            // ClearCacheProcedure
            Clear_Cache_Begin_Tips = textDefine.Clear_Cache_Begin_Tips;
            Clear_Cache_Over_Tips = textDefine.Clear_Cache_Over_Tips;
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("DGame Tools/Settings/Save Update UI Define Config")]
        public static void Save()
        {
            string dirPath = Application.dataPath + "/Resources/Config/";
            string filePath = dirPath + "UpdateUIDefine.json";

            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }

            if (System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"配置文件已经存在: {filePath}");
                return;
            }
            var jsonStr = JsonUtility.ToJson(Instance, true);
            System.IO.File.WriteAllText(filePath, jsonStr, Encoding.UTF8);
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"配置文件已生成: {filePath}");
        }

#endif
    }
}