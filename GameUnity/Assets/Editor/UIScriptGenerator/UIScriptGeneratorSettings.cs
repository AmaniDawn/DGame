using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DGame
{
    [CreateAssetMenu(menuName = "DGame/UIScriptGeneratorSettings", fileName = "UIScriptGeneratorSettings")]
    public class UIScriptGeneratorSettings : EditorScriptableObjectSingleton<UIScriptGeneratorSettings>
    {
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private bool useBindComponent;
        [SerializeField] private string genCodePath = "Assets/Scripts/HotFix/GameLogic/UI/Gen";
        [SerializeField] private string impCodePath = "Assets/Scripts/HotFix/GameLogic/UI";
        [SerializeField] private string nameSpace = "GameLogic";
        [SerializeField] private string widgetName = "item";
        [SerializeField] private UIFieldCodeStyle codeStyle = UIFieldCodeStyle.MPrefix;
        [SerializeField] private List<UIGenType> uiGenTypes = new List<UIGenType>()
        {
            new UIGenType("UIWindow", false),
            new UIGenType("UIWidget", false),
        };
        public List<UIGenType> UIGenTypes => uiGenTypes;
        public GameObject UIRoot => uiRoot;
        public bool UseBindComponent => useBindComponent;
        public string GenCodePath => genCodePath;
        public string ImpCodePath => impCodePath;
        public string NameSpace => nameSpace;
        public string WidgetName => widgetName;
        public UIFieldCodeStyle CodeStyle => codeStyle;

        [SerializeField] private List<UIScriptGenerateRuler> scriptGenerateRulers = new List<UIScriptGenerateRuler>()
        {
            new UIScriptGenerateRuler("m_go", UIComponentName.GameObject),
            new UIScriptGenerateRuler("m_item", UIComponentName.GameObject),
            new UIScriptGenerateRuler("m_tf", UIComponentName.Transform),
            new UIScriptGenerateRuler("m_rect", UIComponentName.RectTransform),
            new UIScriptGenerateRuler("m_text", UIComponentName.Text),
            new UIScriptGenerateRuler("m_richText", UIComponentName.RichTextItem),
            new UIScriptGenerateRuler("m_btn", UIComponentName.Button),
            new UIScriptGenerateRuler("m_img", UIComponentName.Image),
            new UIScriptGenerateRuler("m_rimg", UIComponentName.RawImage),
            new UIScriptGenerateRuler("m_scrollBar", UIComponentName.Scrollbar),
            new UIScriptGenerateRuler("m_scroll", UIComponentName.ScrollRect),
            new UIScriptGenerateRuler("m_input", UIComponentName.InputField),
            new UIScriptGenerateRuler("m_grid", UIComponentName.GridLayoutGroup),
            new UIScriptGenerateRuler("m_hlay", UIComponentName.HorizontalLayoutGroup),
            new UIScriptGenerateRuler("m_vlay", UIComponentName.VerticalLayoutGroup),
            new UIScriptGenerateRuler("m_slider", UIComponentName.Slider),
            new UIScriptGenerateRuler("m_group", UIComponentName.ToggleGroup),
            new UIScriptGenerateRuler("m_curve", UIComponentName.AnimationCurve),
            new UIScriptGenerateRuler("m_canvasGroup", UIComponentName.CanvasGroup),
            new UIScriptGenerateRuler("m_canvas",UIComponentName.Canvas),
            new UIScriptGenerateRuler("m_toggle",UIComponentName.Toggle),
            new UIScriptGenerateRuler("m_dropDown",UIComponentName.Dropdown),
#if TextMeshPro
            new UIScriptGenerateRuler("m_tmp",UIComponentName.TextMeshProUGUI),
            new UIScriptGenerateRuler("m_tInput",UIComponentName.TMP_InputField),
#endif
        };

        public List<UIScriptGenerateRuler> ScriptGenerateRulers => scriptGenerateRulers;

        [MenuItem("DGame Tools/UISettings/Create UIScriptGeneratorSettings")]
        private static void CreateAutoBindGlobalSettings()
        {
            if (Instance != null)
            {
                string path = AssetDatabase.GetAssetPath(Instance);
                EditorUtility.DisplayDialog("警告", $"已存在UIScriptGeneratorSettings，路径: {path}", "确认");
                return;
            }

            UIScriptGeneratorSettings settings = ScriptableObject.CreateInstance<UIScriptGeneratorSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Editor/UIScriptGenerator/UIScriptGeneratorSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("DGame Tools/UISettings/CreateUIRoot")]
        public static void CreateUIRoot()
        {
            if (Instance.uiRoot == null)
            {
                EditorUtility.DisplayDialog("错误", "请先在 UIScriptGeneratorSettings 中设置 uiRoot 预制体", "确认");
                DGameUIGeneratorSettingsProvider.OpenUIGeneratorSettings();
                Selection.activeObject = Instance;
                return;
            }
            GameObject uiRoot = GameObject.Find(Instance.uiRoot.name);
            if (uiRoot != null)
            {
                Debug.Log("场景中已存在UIRoot物体");
                return;
            }

            uiRoot = PrefabUtility.InstantiatePrefab(Instance.uiRoot) as GameObject;
            uiRoot?.transform?.SetParent(null);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());
            Debug.Log("已成功创建UIRoot物体");
        }

        public static List<UIScriptGenerateRuler> GetScriptGenerateRulers() => Instance?.ScriptGenerateRulers;

        public static string GetUINameSpace() => Instance?.NameSpace;

        public static UIFieldCodeStyle GetUIFieldCodeStyle() => Instance != null ? Instance.CodeStyle : UIFieldCodeStyle.MPrefix;

        public static string GetGenCodePath() => Instance?.GenCodePath;
        public static string GetImpCodePath() => Instance?.ImpCodePath;

        public static string GetUIWidgetName() => Instance?.widgetName;

        public static string GetUIComponentWithoutPrefixName(UIComponentName uiComponentName)
        {
            if (Instance.ScriptGenerateRulers == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < Instance.ScriptGenerateRulers.Count; i++)
            {
                var rule = Instance.ScriptGenerateRulers[i];

                if (rule.componentName == uiComponentName)
                {
                    return rule.uiElementRegex.Substring(rule.uiElementRegex.IndexOf("_", StringComparison.Ordinal) + 1);
                }
            }
            return string.Empty;
        }

        public static string GetPrefixNameByCodeStyle(UIFieldCodeStyle style)
        {
            return style switch
            {
                UIFieldCodeStyle.UnderscorePrefix => "_",
                UIFieldCodeStyle.MPrefix => "m_",
                _ => "m_"
            };
        }

        public static UIGenType GetUIGenType(string uiGenTypeName)
        {
            if (string.IsNullOrEmpty(uiGenTypeName))
            {
                return null;
            }
            var tempList = Instance.UIGenTypes;
            for (int i = 0; i < tempList.Count; i++)
            {
                var uiGenType = tempList[i];

                if (string.Equals(uiGenTypeName, uiGenType.uiTypeName, StringComparison.Ordinal))
                {
                    return uiGenType;
                }
            }
            return null;
        }
    }
}