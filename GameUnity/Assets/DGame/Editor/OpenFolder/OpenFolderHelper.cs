using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public static class OpenFolderHelper
    {
        [MenuItem("DGame Tools/打开文件夹/打开Assets", false, 81)]
        private static void OpenAssetsPathFolder()
        {
            OpenFolder(Application.dataPath);
        }

        [MenuItem("DGame Tools/打开文件夹/打开PersistentDataPathFolder", false, 82)]
        private static void OpenPersistentDataPathFolder()
        {
            OpenFolder(Application.persistentDataPath);
        }

        [MenuItem("DGame Tools/打开文件夹/打开StreamingAssetsPathFolder", false, 83)]
        private static void OpenStreamingAssetsPathFolder()
        {
            OpenFolder(Application.streamingAssetsPath);
        }

        /// <summary>
        /// 打开 Temporary Cache Path 文件夹。
        /// </summary>
        [MenuItem("DGame Tools/打开文件夹/打开Temporary Cache Path", false, 84)]
        public static void OpenFolderTemporaryCachePath()
        {
            OpenFolder(Application.temporaryCachePath);
        }

        /// <summary>
        /// 打开 Console Log Path 文件夹。
        /// </summary>
        [MenuItem("DGame Tools/打开文件夹/打开Console Log Path", false, 85)]
        public static void OpenFolderConsoleLogPath()
        {
            OpenFolder(System.IO.Path.GetDirectoryName(Application.consoleLogPath));
        }

        public static void OpenFolder(string folder)
        {
            folder = $"\"{folder}\"";
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    Process.Start("Explorer.exe", folder.Replace('/', '\\'));
                    break;

                case RuntimePlatform.OSXEditor:
                    Process.Start("open", folder);
                    break;

                default:
                    throw new System.Exception($"Not support open folder on '{Application.platform}' platform.");
            }
        }
    }
}