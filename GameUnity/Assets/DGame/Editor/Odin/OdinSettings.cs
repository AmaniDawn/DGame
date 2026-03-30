using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 编辑器增加一个Odin功能的开关 用于测试使用
    /// </summary>
    public static class OdinSettings
    {
        private static string ENABLE_ODIN_INSPECTOR = "ENABLE_ODIN_INSPECTOR";

        [MenuItem("DGame Tools/Odin Settings/开启Odin编辑器功能", false, 101)]
        private static void EnableOdin()
        {
            ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(ENABLE_ODIN_INSPECTOR);
        }

        [MenuItem("DGame Tools/Odin Settings/禁用Odin编辑器功能", false, 102)]
        private static void DisableOdin()
        {
            ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(ENABLE_ODIN_INSPECTOR);
        }
    }
}