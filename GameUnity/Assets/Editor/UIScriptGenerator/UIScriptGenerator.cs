using System.Text;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public partial class UIScriptGenerator
    {
        private static string[] VARIABLE_NAME_REGEX;
        private const string GAP = "/";
        private static TextEditor m_textEditor = new TextEditor();

        [MenuItem("GameObject/ScriptGenerator/UIProperty", priority = 41)]
        public static void MemberProperty()
        {
            Generate(false);
        }

        [MenuItem("GameObject/ScriptGenerator/UIProperty", true, priority = 41)]
        public static bool ValidateMemberProperty()
        {
            return !UIScriptGeneratorSettings.Instance.UseBindComponent;
        }


        [MenuItem("GameObject/ScriptGenerator/UIProperty - UniTask", priority = 43)]
        public static void MemberPropertyUniTask()
        {
            Generate(false, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIProperty - UniTask", true, priority = 41)]
        public static bool ValidateMemberPropertyUniTask()
        {
            return !UIScriptGeneratorSettings.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListener", priority = 42)]
        public static void MemberPropertyAndListener()
        {
            Generate(true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListener", true, priority = 41)]
        public static bool ValidateMemberPropertyAndListener()
        {
            return !UIScriptGeneratorSettings.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListener - UniTask", priority = 44)]
        public static void MemberPropertyAndListenerUniTask()
        {
            Generate(true, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListener - UniTask", true, priority = 41)]
        public static bool ValidateMemberPropertyAndListenerUniTask()
        {
            return !UIScriptGeneratorSettings.Instance.UseBindComponent;
        }

        private static void Generate(bool includeListener, bool isUniTask = false)
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                return;
            }

            CheckVariableNames();

            StringBuilder strVar = new StringBuilder();
            StringBuilder strBind = new StringBuilder();
            StringBuilder strOnCreate = new StringBuilder();
            StringBuilder strCallback = new StringBuilder();
            Ergodic(root, root, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            StringBuilder strFile = new StringBuilder();

            if (includeListener)
            {
#if ENABLE_TEXTMESHPRO
                    strFile.AppendLine("using TMPro;");
#endif
                if (isUniTask)
                {
                    strFile.AppendLine("using Cysharp.Threading.Tasks;");
                }
                strFile.AppendLine("using UnityEngine;");
                strFile.AppendLine("using UnityEngine.UI;");
                strFile.AppendLine("using DGame;");
                strFile.AppendLine();
                strFile.AppendLine($"namespace {UIScriptGeneratorSettings.GetUINameSpace()}");
                strFile.AppendLine("{");
                {
                    var widgetPrefix = GetUIWidgetName();

                    if (root.name.StartsWith(widgetPrefix))
                    {
                        strFile.AppendLine($"\tclass {root.name.Replace(widgetPrefix, string.Empty)} : UIWidget");
                    }
                    else
                    {
                        // strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{root.name}\")]");
                        strFile.AppendLine($"\tclass {root.name} : UIWindow");
                    }
                    strFile.AppendLine("\t{");
                }
            }

            // 脚本工具生成的代码
            strFile.AppendLine($"\t\t#region 脚本工具生成的代码");
            strFile.AppendLine();
            strFile.AppendLine(strVar.ToString());
            strFile.AppendLine("\t\tprotected override void ScriptGenerator()");
            strFile.AppendLine("\t\t{");
            {
                strFile.Append(strBind.ToString());
                strFile.Append(strOnCreate.ToString());
            }
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.Append($"\t\t#endregion");
            strFile.AppendLine();

            if (includeListener)
            {
                strFile.AppendLine();
                strFile.AppendLine("\t\t#region 事件");
                strFile.AppendLine();
                strFile.Append(strCallback.ToString());
                strFile.AppendLine($"\t\t#endregion");
                strFile.AppendLine("\t}");
                strFile.AppendLine("}");
            }

            m_textEditor.Delete();
            m_textEditor.text = strFile.ToString();
            m_textEditor.SelectAll();
            m_textEditor.Copy();
            Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
        }

        public static void Ergodic(Transform root, Transform transform, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteScript(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);

                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }
                Ergodic(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            }
        }

        private static void WriteScript(Transform root, Transform child, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            string varName = child.name;
            // 查找相关的规则定义
            var rule = UIScriptGeneratorSettings.GetScriptGenerateRulers()
                .Find(r => varName.StartsWith(r.uiElementRegex));

            if (rule == null)
            {
                return;
            }
            var componentName = rule.componentName.ToString();
            if (string.IsNullOrEmpty(componentName))
            {
                return;
            }
            varName = GetVariableName(varName);
            if (string.IsNullOrEmpty(varName))
            {
                return;
            }
            string varPath = GetRelativePath(child, root);
            strVar.AppendLine($"\t\tprivate {componentName} {varName};");
            switch (rule.componentName)
            {
                case UIComponentName.GameObject:
                    strBind.AppendLine($"\t\t\t{varName} = FindChild(\"{varPath}\").gameObject;");
                    break;
                case UIComponentName.Transform:
                    strBind.AppendLine($"\t\t\t{varName} = FindChild(\"{varPath}\");");
                    break;
                case UIComponentName.AnimationCurve:
                    // strBind.Append($"\t\t\t{varName} = FindChildComponent<AnimCurveObject>(\"{varPath}\").m_animCurve;\n");
                    strBind.AppendLine($"\t\t\t{varName} = FindChildComponent<AnimationCurve>(\"{varPath}\");");
                    break;
                default:
                    if (rule.isUIWidget)
                    {
                        strBind.AppendLine($"\t\t\t{varName} = CreateWidget<{componentName}>(\"{varPath}\");");
                        return;
                    }
                    strBind.AppendLine($"\t\t\t{varName} = FindChildComponent<{componentName}>(\"{varPath}\");");
                    break;
            }

            switch (rule.componentName)
            {
                case UIComponentName.Button:
                    var btnFuncName = GetButtonFuncName(varName);
                    if (isUniTask)
                    {
                        strOnCreate.AppendLine($"\t\t\t{varName}.onClick.AddListener(UniTask.UnityAction({btnFuncName}));");
                        strCallback.AppendLine($"\t\tprivate async UniTaskVoid {btnFuncName}()");
                        strCallback.AppendLine("\t\t{");
                        strCallback.AppendLine("\t\t\tawait UniTask.Yield();");
                        strCallback.AppendLine("\t\t}");
                    }
                    else
                    {
                        strOnCreate.AppendLine($"\t\t\t{varName}.onClick.AddListener({btnFuncName});");
                        strCallback.AppendLine($"\t\tprivate void {btnFuncName}()");
                        strCallback.AppendLine("\t\t{");
                        strCallback.AppendLine("\t\t}");
                    }
                    strCallback.AppendLine();
                    break;
                case UIComponentName.Toggle:
                    var toggleFuncName = GetToggleFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({toggleFuncName});");
                    strCallback.AppendLine($"\t\tprivate void {toggleFuncName}(bool isOn)");
                    strCallback.AppendLine("\t\t{");
                    strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;
                case UIComponentName.Slider:
                    var sliderFuncName = GetSliderFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({sliderFuncName});");
                    strCallback.AppendLine($"\t\tprivate void {sliderFuncName}(float value)");
                    strCallback.AppendLine("\t\t{");
                    strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;
            }
        }

        private static object GetButtonFuncName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }
            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    return $"OnClick{varName.Replace(prefix + UIScriptGeneratorSettings.GetUIComponentWithoutPrefixName(UIComponentName.Button), string.Empty)}Btn";
                }
            }
            return varName;
        }

        private static object GetToggleFuncName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }
            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    return
                        $"OnToggle{varName.Replace(prefix + UIScriptGeneratorSettings.GetUIComponentWithoutPrefixName(UIComponentName.Toggle), string.Empty)}Change";
                }
            }
            return varName;
        }

        private static object GetSliderFuncName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }
            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    return
                        $"OnSlider{varName.Replace(prefix + UIScriptGeneratorSettings.GetUIComponentWithoutPrefixName(UIComponentName.Slider), string.Empty)}Change";
                }
            }
            return varName;
        }

        private static string GetRelativePath(Transform child, Transform root)
        {
            StringBuilder path = new StringBuilder();
            path.Append(child.name);
            Transform current = child;
            while (current.parent != null && current.parent != root)
            {
                current = current.parent;
                path.Insert(0, GAP);
                path.Insert(0, current.name);
            }
            return path.ToString();
        }

        private static string GetVariableName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }

            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    varName = varName.Replace(prefix, string.Empty);
                    varName = GetComponentName(varName);
                    break;
                }
            }
            return varName;
        }

        private static string GetComponentName(string componentName)
        {
            return GetPrefixName() + componentName;
        }

        private static string GetPrefixName()
        {
            return UIScriptGeneratorSettings.GetPrefixNameByCodeStyle(UIScriptGeneratorSettings.Instance.CodeStyle);
        }

        private static string GetPrefixNameByCodeStyle(UIFieldCodeStyle style)
        {
            return UIScriptGeneratorSettings.GetPrefixNameByCodeStyle(style);
        }

        private static string GetUIWidgetName()
        {
            return GetComponentName(UIScriptGeneratorSettings.GetUIWidgetName());
        }
    }
}