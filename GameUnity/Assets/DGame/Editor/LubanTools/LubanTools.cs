using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public static class LubanTools
    {
        [MenuItem("DGame Tools/Luban/转表", priority = -100)]
        private static void ConvertTableData()
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            string path = Application.dataPath + "/../GameConfig/gen_bin_client_lazyload.sh";
#elif UNITY_EDITOR_WIN
            string path = Application.dataPath + "/../GameConfig/gen_bin_client_lazyload.bat";
#endif
            Debug.Log($"执行转表：{path}");
            ShellHelper.RunByPath(path);
            AssetDatabase.Refresh();
        }
    }
}