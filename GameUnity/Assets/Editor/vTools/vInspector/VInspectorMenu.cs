#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using static VInspector.Libs.VUtils;
using static VInspector.Libs.VGUI;
// using static VTools.VDebug;

namespace VInspector
{
    class VInspectorMenu
    {
        public static bool navigationBarEnabled { get => EditorPrefsCached.GetBool("vInspector-navigationBarEnabled", false); set => EditorPrefsCached.SetBool("vInspector-navigationBarEnabled", value); }
        public static bool copyPasteButtonsEnabled { get => EditorPrefsCached.GetBool("vInspector-copyPasteButtonsEnabled", false); set => EditorPrefsCached.SetBool("vInspector-copyPasteButtonsEnabled", value); }
        public static bool playmodeSaveButtonEnabled { get => EditorPrefsCached.GetBool("vInspector-saveInPlaymodeButtonEnabled", false); set => EditorPrefsCached.SetBool("vInspector-saveInPlaymodeButtonEnabled", value); }
        public static bool componentWindowsEnabled { get => EditorPrefsCached.GetBool("vInspector-componentWindowsEnabled", false); set => EditorPrefsCached.SetBool("vInspector-componentWindowsEnabled", value); }
        public static bool componentAnimationsEnabled { get => EditorPrefsCached.GetBool("vInspector-componentAnimationsEnabled", false); set => EditorPrefsCached.SetBool("vInspector-componentAnimationsEnabled", value); }
        public static bool minimalModeEnabled { get => EditorPrefsCached.GetBool("vInspector-minimalModeEnabled", false); set => EditorPrefsCached.SetBool("vInspector-minimalModeEnabled", value); }
        public static bool resettableVariablesEnabled { get => EditorPrefsCached.GetBool("vInspector-resettableVariablesEnabled", false); set => EditorPrefsCached.SetBool("vInspector-resettableVariablesEnabled", value); }
        public static bool hideScriptFieldEnabled { get => EditorPrefsCached.GetBool("vInspector-hideScriptFieldEnabled", false); set => EditorPrefsCached.SetBool("vInspector-hideScriptFieldEnabled", value); }
        public static bool hideHelpButtonEnabled { get => EditorPrefsCached.GetBool("vInspector-hideHelpButtonEnabled", false); set => EditorPrefsCached.SetBool("vInspector-hideHelpButtonEnabled", value); }
        public static bool hidePresetsButtonEnabled { get => EditorPrefsCached.GetBool("vInspector-hidePresetsButtonEnabled", false); set => EditorPrefsCached.SetBool("vInspector-hidePresetsButtonEnabled", value); }

        public static bool toggleActiveEnabled { get => EditorPrefsCached.GetBool("vInspector-toggleActiveEnabled", true); set => EditorPrefsCached.SetBool("vInspector-toggleActiveEnabled", value); }
        public static bool deleteEnabled { get => EditorPrefsCached.GetBool("vInspector-deleteEnabled", true); set => EditorPrefsCached.SetBool("vInspector-deleteEnabled", value); }
        public static bool toggleExpandedEnabled { get => EditorPrefsCached.GetBool("vInspector-toggleExpandedEnabled", true); set => EditorPrefsCached.SetBool("vInspector-toggleExpandedEnabled", value); }
        public static bool collapseEverythingElseEnabled { get => EditorPrefsCached.GetBool("vInspector-collapseEverythingElseEnabled", true); set => EditorPrefsCached.SetBool("vInspector-collapseEverythingElseEnabled", value); }
        public static bool collapseEverythingEnabled { get => EditorPrefsCached.GetBool("vInspector-collapseEverythingEnabled", true); set => EditorPrefsCached.SetBool("vInspector-collapseEverythingEnabled", value); }

        public static bool attributesDisabled { get => EditorUtils.IsSymbolDefinedInAsmdef(nameof(VInspector), "VINSPECTOR_ATTRIBUTES_DISABLED"); set => EditorUtils.SetSymbolDefinedInAsmdef(nameof(VInspector), "VINSPECTOR_ATTRIBUTES_DISABLED", value); }
        public static bool pluginDisabled { get => ProjectPrefs.GetBool("vInspector-pluginDisabled", false); set => ProjectPrefs.SetBool("vInspector-pluginDisabled", value); }




        public static void RepaintInspectors()
        {
            Resources.FindObjectsOfTypeAll(typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow"))
                     .Cast<EditorWindow>()
                     .ForEach(r => r.Repaint());

            Resources.FindObjectsOfTypeAll(typeof(Editor).Assembly.GetType("UnityEditor.PropertyEditor"))
                     .Where(r => r.GetType().BaseType == typeof(EditorWindow))
                     .Cast<EditorWindow>()
                     .ForEach(r => r.Repaint());
        }



        const string dir = "Tools/vInspector/";
#if UNITY_EDITOR_OSX
        const string cmd = "Cmd";
#else
        const string cmd = "Ctrl";
#endif

        const string navigationBar = dir + "Navigation bar";
        const string copyPasteButtons = dir + "Copy \u2215 Paste components";
        const string saveInPlaymodeButton = dir + "Save in play mode";
        const string componentWindows = dir + "Create component windows with Alt-Drag";
        const string componentAnimations = dir + "Component expand \u2215 collapse animations";
        const string minimalMode = dir + "Minimal mode";
        const string resettableVariables = dir + "Resettable variables";
        const string hideScriptField = dir + "Hide script field";
        const string hideHelpButton = dir + "Hide help button";
        const string hidePresetsButton = dir + "Hide presets button";

        const string toggleActive = dir + "A to toggle component active";
        const string delete = dir + "X to delete component";
        const string toggleExpanded = dir + "E to expand \u2215 collapse component";
        const string collapseEverythingElse = dir + "Shift-E to isolate component";
        const string collapseEverything = dir + "Ctrl-Shift-E to expand \u2215 collapse all components";

        const string disableAttributes = dir + "Disable attributes";
        const string disablePlugin = dir + "Disable vInspector";







        [MenuItem(dir + "Features", false, 1)] static void dadsas() { }
        [MenuItem(dir + "Features", true, 1)] static bool dadsas123() => false;

        [MenuItem(navigationBar, false, 2)] static void dadsadsadasdsadadsas() { navigationBarEnabled = !navigationBarEnabled; RepaintInspectors(); }
        [MenuItem(navigationBar, true, 2)] static bool dadsaddsasadadsdasadsas() { Menu.SetChecked(navigationBar, navigationBarEnabled); return !pluginDisabled; }

        [MenuItem(copyPasteButtons, false, 3)] static void dadsaasddsadaasdsdsadadsas() { copyPasteButtonsEnabled = !copyPasteButtonsEnabled; VInspector.UpdateHeaderButtons(null); RepaintInspectors(); }
        [MenuItem(copyPasteButtons, true, 3)] static bool dadsaddasdsasaasddadsdasadsas() { Menu.SetChecked(copyPasteButtons, copyPasteButtonsEnabled); return !pluginDisabled; }

        [MenuItem(saveInPlaymodeButton, false, 4)] static void dadsadsadaasasdsdsadadsas() { playmodeSaveButtonEnabled = !playmodeSaveButtonEnabled; VInspector.UpdateHeaderButtons(null); RepaintInspectors(); }
        [MenuItem(saveInPlaymodeButton, true, 4)] static bool dadsaddsasaadsasddadsdasadsas() { Menu.SetChecked(saveInPlaymodeButton, playmodeSaveButtonEnabled); return !pluginDisabled; }

        [MenuItem(componentWindows, false, 5)] static void dadsadsadaasdsdsadadsas() { componentWindowsEnabled = !componentWindowsEnabled; RepaintInspectors(); }
        [MenuItem(componentWindows, true, 5)] static bool dadsaddsasaasddadsdasadsas() { Menu.SetChecked(componentWindows, componentWindowsEnabled); return !pluginDisabled; }

        [MenuItem(componentAnimations, false, 6)] static void dadsadsadsadaasdsdsadadsas() { componentAnimationsEnabled = !componentAnimationsEnabled; RepaintInspectors(); }
        [MenuItem(componentAnimations, true, 6)] static bool dadsadddsasasaasddadsdasadsas() { Menu.SetChecked(componentAnimations, componentAnimationsEnabled); return !pluginDisabled; }

        [MenuItem(minimalMode, false, 7)] static void dadsadsadsadsadasdsadadsas() { minimalModeEnabled = !minimalModeEnabled; RepaintInspectors(); }
        [MenuItem(minimalMode, true, 7)] static bool dadsadasdasddsasadadsdasadsas() { Menu.SetChecked(minimalMode, minimalModeEnabled); return !pluginDisabled; }

        [MenuItem(resettableVariables, false, 8)] static void dadsadsadsadasdsadadsas() { resettableVariablesEnabled = !resettableVariablesEnabled; RepaintInspectors(); }
        [MenuItem(resettableVariables, true, 8)] static bool dadsadasddsasadadsdasadsas() { Menu.SetChecked(resettableVariables, resettableVariablesEnabled); return !pluginDisabled; }

        [MenuItem(hideScriptField, false, 9)] static void dadsadsdsaadsadsadasdsadadsas() { hideScriptFieldEnabled = !hideScriptFieldEnabled; RepaintInspectors(); }
        [MenuItem(hideScriptField, true, 9)] static bool dadsadasadsdasddsasadadsdasadsas() { Menu.SetChecked(hideScriptField, hideScriptFieldEnabled); return !pluginDisabled; }

        [MenuItem(hideHelpButton, false, 10)] static void dadsadsadsdsaadsadsadasdsadadsas() { hideHelpButtonEnabled = !hideHelpButtonEnabled; VInspector.UpdateHeaderButtons(null); RepaintInspectors(); }
        [MenuItem(hideHelpButton, true, 10)] static bool dadsaadsdasadsdasddsasadadsdasadsas() { Menu.SetChecked(hideHelpButton, hideHelpButtonEnabled); return !pluginDisabled; }

        [MenuItem(hidePresetsButton, false, 11)] static void dadsadsdsaadssdadsadasdsadadsas() { hidePresetsButtonEnabled = !hidePresetsButtonEnabled; VInspector.UpdateHeaderButtons(null); RepaintInspectors(); }
        [MenuItem(hidePresetsButton, true, 11)] static bool dadsadasadsddsasddsasadadsdasadsas() { Menu.SetChecked(hidePresetsButton, hidePresetsButtonEnabled); return !pluginDisabled; }




        [MenuItem(dir + "Shortcuts", false, 1001)] static void dadsadsas() { }
        [MenuItem(dir + "Shortcuts", true, 1001)] static bool dadsadsas123() => false;

        [MenuItem(toggleActive, false, 1002)] static void dadsadadsas() => toggleActiveEnabled = !toggleActiveEnabled;
        [MenuItem(toggleActive, true, 1002)] static bool dadsaddasadsas() { Menu.SetChecked(toggleActive, toggleActiveEnabled); return !pluginDisabled; }

        [MenuItem(delete, false, 1003)] static void dadsadsadasdadsas() => deleteEnabled = !deleteEnabled;
        [MenuItem(delete, true, 1003)] static bool dadsaddsasaddasadsas() { Menu.SetChecked(delete, deleteEnabled); return !pluginDisabled; }

        [MenuItem(toggleExpanded, false, 1004)] static void dadsaddsasadasdsadadsas() => toggleExpandedEnabled = !toggleExpandedEnabled;
        [MenuItem(toggleExpanded, true, 1004)] static bool dadsaddsdsasadadsdasadsas() { Menu.SetChecked(toggleExpanded, toggleExpandedEnabled); return !pluginDisabled; }

        [MenuItem(collapseEverythingElse, false, 1005)] static void dadsadsasdadasdsadadsas() => collapseEverythingElseEnabled = !collapseEverythingElseEnabled;
        [MenuItem(collapseEverythingElse, true, 1005)] static bool dadsaddsdasasadadsdasadsas() { Menu.SetChecked(collapseEverythingElse, collapseEverythingElseEnabled); return !pluginDisabled; }

        [MenuItem(collapseEverything, false, 1006)] static void dadsadsdasadasdsadadsas() => collapseEverythingEnabled = !collapseEverythingEnabled;
        [MenuItem(collapseEverything, true, 1006)] static bool dadsaddssdaasadadsdasadsas() { Menu.SetChecked(collapseEverything, collapseEverythingEnabled); return !pluginDisabled; }




        [MenuItem(dir + "More", false, 10001)] static void daasadsddsas() { }
        [MenuItem(dir + "More", true, 10001)] static bool dadsadsdasas123() => false;

        [MenuItem(dir + "Open manual", false, 10002)]
        static void dadadssadsas() => Application.OpenURL("https://kubacho-lab.gitbook.io/vinspector2");

        [MenuItem(dir + "Join our Discord", false, 10003)]
        static void dadasdsas() => Application.OpenURL("https://discord.gg/pUektnZeJT");




        [MenuItem(disablePlugin, false, 100001)] static void dadsadsdsdasadasdasdsadadsas() { pluginDisabled = !pluginDisabled; attributesDisabled = pluginDisabled; }
        [MenuItem(disablePlugin, true, 100001)] static bool dadsaddssdsdaasadsadadsdasadsas() { Menu.SetChecked(disablePlugin, pluginDisabled); return true; }



    }
}
#endif