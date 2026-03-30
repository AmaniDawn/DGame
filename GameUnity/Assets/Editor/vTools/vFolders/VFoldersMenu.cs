#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static VFolders.Libs.VUtils;
using static VFolders.Libs.VGUI;
// using static VTools.VDebug;


namespace VFolders
{
    class VFoldersMenu
    {

        public static bool navigationBarEnabled { get => EditorPrefsCached.GetBool("vFolders-navigationBarEnabled", false); set => EditorPrefsCached.SetBool("vFolders-navigationBarEnabled", value); }
        public static bool twoLineNamesEnabled { get => EditorPrefsCached.GetBool("vFolders-twoLineNamesEnabled", false); set => EditorPrefsCached.SetBool("vFolders-twoLineNamesEnabled", value); }

        public static bool autoIconsEnabled { get => EditorPrefsCached.GetBool("vFolders-autoIconsEnabled", false); set => EditorPrefsCached.SetBool("vFolders-autoIconsEnabled", value); }
        public static bool hierarchyLinesEnabled { get => EditorPrefsCached.GetBool("vFolders-hierarchyLinesEnabled", false); set => EditorPrefsCached.SetBool("vFolders-hierarchyLinesEnabled", value); }
        public static bool zebraStripingEnabled { get => EditorPrefsCached.GetBool("vFolders-zebraStripingEnabled", false); set => EditorPrefsCached.SetBool("vFolders-zebraStripingEnabled", value); }
        public static bool contentMinimapEnabled { get => EditorPrefsCached.GetBool("vFolders-contentMinimapEnabled", false); set => EditorPrefsCached.SetBool("vFolders-contentMinimapEnabled", value); }
        public static bool backgroundColorsEnabled { get => EditorPrefsCached.GetBool("vFolders-backgroundColorsEnabled", false); set => EditorPrefsCached.SetBool("vFolders-backgroundColorsEnabled", value); }
        public static bool minimalModeEnabled { get => EditorPrefsCached.GetBool("vFolders-minimalModeEnabled", false); set => EditorPrefsCached.SetBool("vFolders-minimalModeEnabled", value); }
        public static bool foldersFirstEnabled { get => EditorPrefsCached.GetBool("vFolders-foldersFirstEnabled", false); set => EditorPrefsCached.SetBool("vFolders-foldersFirstEnabled", value); }
        public static bool toggleExpandedEnabled { get => EditorPrefsCached.GetBool("vFolders-toggleExpandedEnabled", true); set => EditorPrefsCached.SetBool("vFolders-toggleExpandedEnabled", value); }
        public static bool collapseEverythingElseEnabled { get => EditorPrefsCached.GetBool("vFolders-collapseEverythingElseEnabled", true); set => EditorPrefsCached.SetBool("vFolders-collapseEverythingElseEnabled", value); }
        public static bool collapseEverythingEnabled { get => EditorPrefsCached.GetBool("vFolders-collapseEverythingEnabled", true); set => EditorPrefsCached.SetBool("vFolders-collapseEverythingEnabled", value); }

        public static bool pluginDisabled { get => EditorPrefsCached.GetBool("vFolders-pluginDisabled", false); set => EditorPrefsCached.SetBool("vFolders-pluginDisabled", value); }




        const string dir = "Tools/vFolders/";

        const string navigationBar = dir + "Navigation bar";
        const string autoIcons = dir + "Automatic icons";
        const string twoLineNames = dir + "Two-line names";
        const string hierarchyLines = dir + "Hierarchy lines";
        const string backgroundColors = dir + "Background colors";
        const string minimalMode = dir + "Minimal mode";
        const string zebraStriping = dir + "Zebra striping";
        const string contentMinimap = dir + "Content minimap";
        const string foldersFirst = dir + "Sort folders first";

        const string toggleExpanded = dir + "E to expand \u2215 collapse folder";
        const string collapseEverythingElse = dir + "Shift-E to isolate folder";
        const string collapseEverything = dir + "Ctrl-Shift-E to collapse all folders";

        const string disablePlugin = dir + "Disable vFolders";






        [MenuItem(dir + "Features", false, 1)] static void daasddsas() { }
        [MenuItem(dir + "Features", true, 1)] static bool dadsdasas123() => false;

        [MenuItem(navigationBar, false, 2)] static void dadsaadsdsadasdsadadsas() { navigationBarEnabled = !navigationBarEnabled; EditorApplication.RepaintProjectWindow(); }
        [MenuItem(navigationBar, true, 2)] static bool dadsaddasdsasadadsdasadsas() { Menu.SetChecked(navigationBar, navigationBarEnabled); return !pluginDisabled; }

        [MenuItem(twoLineNames, false, 3)] static void dadsadaddssadass() { twoLineNamesEnabled = !twoLineNamesEnabled; EditorApplication.RepaintProjectWindow(); }
        [MenuItem(twoLineNames, true, 3)] static bool dadsaddasdsaasddsas() { Menu.SetChecked(twoLineNames, twoLineNamesEnabled); return !pluginDisabled; }

        [MenuItem(autoIcons, false, 4)] static void dadsadadsas() { autoIconsEnabled = !autoIconsEnabled; VFolders.folderInfoCache.Clear(); EditorApplication.RepaintProjectWindow(); }
        [MenuItem(autoIcons, true, 4)] static bool dadsaddasadsas() { Menu.SetChecked(autoIcons, autoIconsEnabled); return !pluginDisabled; }

        [MenuItem(hierarchyLines, false, 5)] static void dadsadadsadass() { hierarchyLinesEnabled = !hierarchyLinesEnabled; EditorApplication.RepaintProjectWindow(); }
        [MenuItem(hierarchyLines, true, 5)] static bool dadsaddasaasddsas() { Menu.SetChecked(hierarchyLines, hierarchyLinesEnabled); return !pluginDisabled; }

        [MenuItem(zebraStriping, false, 6)] static void dadsadaddsasadsadass() { zebraStripingEnabled = !zebraStripingEnabled; EditorApplication.RepaintProjectWindow(); }
        [MenuItem(zebraStriping, true, 6)] static bool dadsaddadassadsaasddsas() { Menu.SetChecked(zebraStriping, zebraStripingEnabled); return !pluginDisabled; }

        [MenuItem(contentMinimap, false, 7)] static void dadsadadasdsadass() { contentMinimapEnabled = !contentMinimapEnabled; EditorApplication.RepaintProjectWindow(); }
        [MenuItem(contentMinimap, true, 7)] static bool dadsadddasasaasddsas() { Menu.SetChecked(contentMinimap, contentMinimapEnabled); return !pluginDisabled; }

        [MenuItem(backgroundColors, false, 8)] static void dadsadadsadsadass() { backgroundColorsEnabled = !backgroundColorsEnabled; EditorApplication.RepaintProjectWindow(); }
        [MenuItem(backgroundColors, true, 8)] static bool dadsaddasadsaasddsas() { Menu.SetChecked(backgroundColors, backgroundColorsEnabled); return !pluginDisabled; }

        [MenuItem(minimalMode, false, 9)] static void dadsadadsaddsasadass() { minimalModeEnabled = !minimalModeEnabled; EditorApplication.RepaintProjectWindow(); }
        [MenuItem(minimalMode, true, 9)] static bool dadsaddasadsadsaasddsas() { Menu.SetChecked(minimalMode, minimalModeEnabled); return !pluginDisabled; }
#if UNITY_EDITOR_OSX
        [MenuItem(foldersFirst, false, 10)] static void dadsdsfaadsdadsas() { foldersFirstEnabled = !foldersFirstEnabled; EditorApplication.RepaintProjectWindow(); if (!foldersFirstEnabled) UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); }
        [MenuItem(foldersFirst, true, 10)] static bool dadsasdfdadsdasadsas() { Menu.SetChecked(foldersFirst, foldersFirstEnabled); return !pluginDisabled; }
#endif



        [MenuItem(dir + "Shortcuts", false, 101)] static void dadsas() { }
        [MenuItem(dir + "Shortcuts", true, 101)] static bool dadsas123() => false;

        [MenuItem(toggleExpanded, false, 102)] static void dadsadsadasdsadadsas() => toggleExpandedEnabled = !toggleExpandedEnabled;
        [MenuItem(toggleExpanded, true, 102)] static bool dadsaddsasadadsdasadsas() { Menu.SetChecked(toggleExpanded, toggleExpandedEnabled); return !pluginDisabled; }

        [MenuItem(collapseEverythingElse, false, 103)] static void dadsadsasdadasdsadadsas() => collapseEverythingElseEnabled = !collapseEverythingElseEnabled;
        [MenuItem(collapseEverythingElse, true, 103)] static bool dadsaddsdasasadadsdasadsas() { Menu.SetChecked(collapseEverythingElse, collapseEverythingElseEnabled); return !pluginDisabled; }

        [MenuItem(collapseEverything, false, 104)] static void dadsadsdasadasdsadadsas() => collapseEverythingEnabled = !collapseEverythingEnabled;
        [MenuItem(collapseEverything, true, 104)] static bool dadsaddssdaasadadsdasadsas() { Menu.SetChecked(collapseEverything, collapseEverythingEnabled); return !pluginDisabled; }




        [MenuItem(dir + "More", false, 1001)] static void daasadsddsas() { }
        [MenuItem(dir + "More", true, 1001)] static bool dadsadsdasas123() => false;

        [MenuItem(dir + "Open manual", false, 1002)]
        static void dadadssadsas() => Application.OpenURL("https://kubacho-lab.gitbook.io/vfolders-2");

        [MenuItem(dir + "Join our Discord", false, 1003)]
        static void dadasdsas() => Application.OpenURL("https://discord.gg/pUektnZeJT");






        [MenuItem(disablePlugin, false, 10001)] static void dadsadsdasadasdasdsadadsas() { pluginDisabled = !pluginDisabled; UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); }
        [MenuItem(disablePlugin, true, 10001)] static bool dadsaddssdaasadsadadsdasadsas() { Menu.SetChecked(disablePlugin, pluginDisabled); return true; }




    }
}
#endif