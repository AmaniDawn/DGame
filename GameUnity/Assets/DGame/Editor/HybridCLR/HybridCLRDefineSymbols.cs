using HybridCLR;
using HybridCLR.Installer;
using UnityEditor;

namespace DGame
{
    public static class HybridCLRDefineSymbols
    {
        private const string ENABLE_HYBRIDCLR_SCRIPTING_DEFINE_SYMBOLS = "ENABLE_HYBRIDCLR";
        private const string ENABLE_OBFUZ_SCRIPTING_DEFINE_SYMBOLS = "ENABLE_OBFUZ";

        [MenuItem("DGame Tools/HybridCLR/启用HybridCLR", priority = 220)]
        public static void EnableHybridCLR()
        {
            var controller = new InstallerController();

            if (!controller.HasInstalledHybridCLR() || controller.InstalledLibil2cppVersion != controller.PackageVersion)
            {
                controller.InstallDefaultHybridCLR();
            }

            if (!HybridCLR.SettingsUtil.Enable)
            {
                HybridCLR.SettingsUtil.Enable = true;
                UpdateSettingsInspector.ForceUpdateAssemblies();
            }

            ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(ENABLE_HYBRIDCLR_SCRIPTING_DEFINE_SYMBOLS);
        }

        [MenuItem("DGame Tools/HybridCLR/禁用HybridCLR", priority = 221)]
        public static void DisableHybridCLR()
        {
#if ENABLE_HYBRIDCLR

            HybridCLR.SettingsUtil.Enable = false;
            EditorUtility.SetDirty(SettingsUtil.HybridCLRSettings);
            AssetDatabase.SaveAssets();

#endif

            ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(ENABLE_HYBRIDCLR_SCRIPTING_DEFINE_SYMBOLS);
        }

// #if ENABLE_OBFUZ
        [MenuItem("DGame Tools/HybridCLR/启用Obfuz")]
        public static void EnableObfuz()
        {
            ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(ENABLE_OBFUZ_SCRIPTING_DEFINE_SYMBOLS);
        }
// #endif

// #if ENABLE_OBFUZ
        [MenuItem("DGame Tools/HybridCLR/禁用Obfuz")]
        public static void DisableObfuz()
        {
            ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(ENABLE_OBFUZ_SCRIPTING_DEFINE_SYMBOLS);
        }
// #endif
    }
}
