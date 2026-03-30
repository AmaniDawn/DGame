#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static VFavorites.Libs.VUtils;
using static VFavorites.Libs.VGUI;



namespace VFavorites
{
    public class VFavoritesMenu
    {

        public static bool pageScrollEnabled { get => EditorPrefsCached.GetBool("vFavorites-pageScrollEnabled", true); set => EditorPrefsCached.SetBool("vFavorites-pageScrollEnabled", value); }
        public static bool numberKeysEnabled { get => EditorPrefsCached.GetBool("vFavorites-numberKeysEnabled", true); set => EditorPrefsCached.SetBool("vFavorites-numberKeysEnabled", value); }
        public static bool arrowKeysEnabled { get => EditorPrefsCached.GetBool("vFavorites-arrowKeysEnabled", true); set => EditorPrefsCached.SetBool("vFavorites-arrowKeysEnabled", value); }

        public static bool fadeAnimationsEnabled { get => EditorPrefsCached.GetBool("vFavorites-fadeAnimationsEnabled", true); set => EditorPrefsCached.SetBool("vFavorites-fadeAnimationsEnabled", value); }
        public static bool pageScrollAnimationEnabled { get => EditorPrefsCached.GetBool("vFavorites-pageScrollAnimationEnabled", true); set => EditorPrefsCached.SetBool("vFavorites-pageScrollAnimationEnabled", value); }

        public static int activeOnKeyCombination { get => EditorPrefsCached.GetInt("vFavorites-activeOnKeyCombination", 0); set => EditorPrefsCached.SetInt("vFavorites-activeOnKeyCombination", value); }
        public static bool activeOnAltEnabled { get => activeOnKeyCombination == 0; set => activeOnKeyCombination = 0; }
        public static bool activeOnAltShiftEnabled { get => activeOnKeyCombination == 1; set => activeOnKeyCombination = 1; }
        public static bool activeOnCtrlAltEnabled { get => activeOnKeyCombination == 2; set => activeOnKeyCombination = 2; }

        public static bool pluginDisabled { get => EditorPrefsCached.GetBool("vFavorites-pluginDisabled", false); set => EditorPrefsCached.SetBool("vFavorites-pluginDisabled", value); }




        const string dir = "Tools/vFavorites/";

        const string pageScroll = dir + "Scroll to change page";
        const string numberKeys = dir + "1-9 keys to change page";
        const string arrowKeys = dir + "Arrow keys to change page or selection ";

        const string fadeAnimations = dir + "Fade animations";
        const string pageScrollAnimation = dir + "Page scroll animation";

        const string activeOnAlt = dir + "Holding Alt";
        const string activeOnAltShift = dir + "Holding Alt and Shift";
#if UNITY_EDITOR_OSX
        const string activeOnCtrlAlt = dir + "Holding Cmd and Alt";
#else
        const string activeOnCtrlAlt = dir + "Holding Ctrl and Alt";

#endif

        const string disablePlugin = dir + "Disable vFavorites";






        [MenuItem(dir + "Shortcuts", false, 1)] static void dadsas() { }
        [MenuItem(dir + "Shortcuts", true, 1)] static bool dadsas123() => false;

        [MenuItem(pageScroll, false, 2)] static void dadsadasadsdadsas() => pageScrollEnabled = !pageScrollEnabled;
        [MenuItem(pageScroll, true, 2)] static bool dadsadasdadsdasadsas() { Menu.SetChecked(pageScroll, pageScrollEnabled); return !pluginDisabled; }

        [MenuItem(numberKeys, false, 4)] static void dadsadadsas() => numberKeysEnabled = !numberKeysEnabled;
        [MenuItem(numberKeys, true, 4)] static bool dadsaddasadsas() { Menu.SetChecked(numberKeys, numberKeysEnabled); return !pluginDisabled; }

        [MenuItem(arrowKeys, false, 5)] static void dadsadaddassas() => arrowKeysEnabled = !arrowKeysEnabled;
        [MenuItem(arrowKeys, true, 5)] static bool dadadssaddasadsas() { Menu.SetChecked(arrowKeys, arrowKeysEnabled); return !pluginDisabled; }





        [MenuItem(dir + "Animations", false, 101)] static void dadsadsas() { }
        [MenuItem(dir + "Animations", true, 101)] static bool dadadssas123() => false;

        [MenuItem(fadeAnimations, false, 102)] static void dadsdasadadsas() => fadeAnimationsEnabled = !fadeAnimationsEnabled;
        [MenuItem(fadeAnimations, true, 102)] static bool dadsadadsadsdasadsas() { Menu.SetChecked(fadeAnimations, fadeAnimationsEnabled); return !pluginDisabled; }

        [MenuItem(pageScrollAnimation, false, 103)] static void dadsdasdasadadsas() => pageScrollAnimationEnabled = !pageScrollAnimationEnabled;
        [MenuItem(pageScrollAnimation, true, 103)] static bool dadsadaddassadsdasadsas() { Menu.SetChecked(pageScrollAnimation, pageScrollAnimationEnabled); return !pluginDisabled; }




        [MenuItem(dir + "Open when", false, 1001)] static void dadsaddssas() { }
        [MenuItem(dir + "Open when", true, 1001)] static bool dadadsssas123() => false;

        [MenuItem(activeOnAlt, false, 1002)] static void dadsdasasdadsas() => activeOnAltEnabled = !activeOnAltEnabled;
        [MenuItem(activeOnAlt, true, 1002)] static bool dadsadadssdadsdasadsas() { Menu.SetChecked(activeOnAlt, activeOnAltEnabled); return !pluginDisabled; }

        [MenuItem(activeOnAltShift, false, 1003)] static void dadsdasasdadsadsas() => activeOnAltShiftEnabled = !activeOnAltShiftEnabled;
        [MenuItem(activeOnAltShift, true, 1003)] static bool dadsadadssdasdadsdasadsas() { Menu.SetChecked(activeOnAltShift, activeOnAltShiftEnabled); return !pluginDisabled; }

        [MenuItem(activeOnCtrlAlt, false, 1004)] static void dadsdasadasadssdadsas() => activeOnCtrlAltEnabled = !activeOnCtrlAltEnabled;
        [MenuItem(activeOnCtrlAlt, true, 1004)] static bool dadsadadsadssdadsdasadsas() { Menu.SetChecked(activeOnCtrlAlt, activeOnCtrlAltEnabled); return !pluginDisabled; }





        [MenuItem(dir + "More", false, 10001)] static void daasadsddsas() { }
        [MenuItem(dir + "More", true, 10001)] static bool dadsadsdasas123() => false;

        // [MenuItem(dir + "Open manual", false, 10002)]
        // static void dadadssadsas() => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(GetScriptPath("VFavorites").GetParentPath().CombinePath("Manual.pdf")));

        [MenuItem(dir + "Join our Discord", false, 10002)]
        static void dadasdsas() => Application.OpenURL("https://discord.gg/pUektnZeJT");





        [MenuItem(dir + "Deals ending soon/Get vHierarchy 2 at 60% off", false, 10003)]
        static void dadadssadasdsas() => Application.OpenURL("https://assetstore.unity.com/packages/slug/253397?aid=1100lGLBn&pubref=deal60menuvfav");

        [MenuItem(dir + "Deals ending soon/Get vFolders 2 at 60% off", false, 10004)]
        static void dadadssasddsas() => Application.OpenURL("https://assetstore.unity.com/packages/slug/255470?aid=1100lGLBn&pubref=deal60menuvfav");

        [MenuItem(dir + "Deals ending soon/Get vInspector 2 at 60% off", false, 10005)]
        static void dadadadsssadsas() => Application.OpenURL("https://assetstore.unity.com/packages/slug/252297?aid=1100lGLBn&pubref=deal60menuvfav");

        [MenuItem(dir + "Deals ending soon/Get vTabs 2 at 60% off", false, 10006)]
        static void dadadadsssadsadsas() => Application.OpenURL("https://assetstore.unity.com/packages/slug/253396?aid=1100lGLBn&pubref=deal60menuvfav");








        [MenuItem(disablePlugin, false, 100001)] static void dadsadsdasadasdasdsadadsas() { pluginDisabled = !pluginDisabled; UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); }
        [MenuItem(disablePlugin, true, 100001)] static bool dadsaddssdaasadsadadsdasadsas() { Menu.SetChecked(disablePlugin, pluginDisabled); return true; }

    }
}
#endif