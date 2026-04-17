using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameLogic;
using DGame;
using GameProto;
using YooAsset;

#if ENABLE_OBFUZ
using Obfuz;
#endif

#pragma warning disable CS0436

/// <summary>
/// 热更DLL入口
/// </summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameStart
{
    private static List<Assembly> m_hotfixAssembly;

    public static void Entrance(object[] objects)
    {
        m_hotfixAssembly = (List<Assembly>)objects[0];
        GameEventLauncher.Init();
        // HybridCLR 不支持的特性
        // RuntimeInitializeOnLoadMethodCollector.ExecuteMethods();
        DGame.Utility.UnityUtil.AddDestroyListener(OnDestroy);
        DLogger.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
        DLogger.Warning("======= Entrance GameStart =======");
        DLogger.Warning("======= 开始游戏 =======");
        InitLanguageSettings();
        StartGame();
    }

    private static void InitLanguageSettings()
    {
        ILocalizationModule localizationModule = GameModule.LocalizationModule;
        DGameLocalizationHelper localizationHelper = new DGameLocalizationHelper();
        localizationModule.SetLocalizationHelper(localizationHelper);
        DLogger.Info("LocalizationHelper: {0}", typeof(LocalAreaType));

        if (GameModule.ResourceModule.PlayMode == EPlayMode.EditorSimulateMode)
        {
            // 设置编辑器资源模式使用的语言
            localizationModule.SetLanguage(LocalAreaType.CN);
            return;
        }

        LocalAreaType language = localizationModule.CurrentLanguage;
        if (DGame.Utility.PlayerPrefsUtil.HasSetting(Constant.Settings.LANGUAGE))
        {
            try
            {
                int languageIndex = DGame.Utility.PlayerPrefsUtil.GetInt(Constant.Settings.LANGUAGE);
                language = (LocalAreaType)languageIndex;
                // string languageString = DGame.Utility.PlayerPrefsUtil.GetString(Constant.Settings.LANGUAGE);
                // language = (DGame.Language)System.Enum.Parse(typeof(DGame.Language), languageString);
                // System.Enum.TryParse(languageString, out language);
                CheckLanguageIsSupport(ref language);
            }
            catch(System.Exception exception)
            {
                DLogger.Error("Init language error, reason {0}", exception.ToString());
            }
        }
        else
        {
            language = localizationModule.SystemLanguage;
            CheckLanguageIsSupport(ref language);
        }

        localizationModule.SetLanguage(language);
        DGame.Utility.PlayerPrefsUtil.SetInt(Constant.Settings.LANGUAGE, (int)language);
        // DGame.Utility.PlayerPrefsUtil.SetString(Constant.Settings.LANGUAGE, language.ToString());
        // DGame.Utility.PlayerPrefsUtil.Save();
        DLogger.Info("Init language settings complete, current language is '{0}'.", language.ToString());
    }

    private static bool CheckLanguageIsSupport(ref LocalAreaType language)
    {
        if (language >= LocalAreaType.MAX)
        {
            // 若是暂不支持的语言，则使用英语
            language = LocalAreaType.EN;
            return false;
        }

        return true;
    }

    private static void StartGame()
    {
        GameModule.UIModule.ShowWindow<MainWindow>();
    }

    private static void OnDestroy()
    {
        SingletonSystem.Destroy();
        UIImageEffect.ClearCache();
        GameModule.Destroy();
        DLogger.Warning("====== Destroy GameStart =======");
    }

    public static Assembly GetAssembly(string assemblyName)
    {
        assemblyName = assemblyName.Replace(".dll", "");
        IEnumerable<Assembly> allAssemblies =
            Settings.UpdateSettings.Enable ? m_hotfixAssembly : AppDomain.CurrentDomain.GetAssemblies();
        return allAssemblies.First(assembly => assembly.FullName.Contains(assemblyName));
    }
}