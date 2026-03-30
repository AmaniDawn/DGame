#if UNITY_EDITOR

using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GameLogic
{
    [InitializeOnLoad]
    internal static class RedDotTreeCodeGenerator
    {
        private static int s_nextId;
        private static bool s_isGeneratingFromCompilation;

        static RedDotTreeCodeGenerator()
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        public static void GenerateCode(RedDotTreeConfig config, bool showDialog)
        {
            if (config == null)
            {
                return;
            }

            config.RefreshPaths();
            AssignIds(config);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("// =============================================================================");
            sb.AppendLine("// AUTO-GENERATED FILE - DO NOT EDIT MANUALLY");
            sb.AppendLine($"// Generated from: {AssetDatabase.GetAssetPath(config)}");
            sb.AppendLine("// =============================================================================");
            sb.AppendLine();

            sb.AppendLine($"namespace {config.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic static class {config.ClassName}");
            sb.AppendLine("\t{");

            foreach (var root in config.RootNodes)
            {
                GenerateNodeCode(sb, root, 2);
            }

            sb.AppendLine();
            GenerateRegisterAllMethod(sb, config);

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            string fullPath = Path.Combine(Application.dataPath, config.OutputPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool fileExists = File.Exists(fullPath);
            if (fileExists)
            {
                var attributes = File.GetAttributes(fullPath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(fullPath, attributes & ~FileAttributes.ReadOnly);
                }
            }

            AssetDatabase.SaveAssetIfDirty(config);
            string newContent = sb.ToString();
            string oldContent = fileExists ? File.ReadAllText(fullPath, Encoding.UTF8) : string.Empty;
            bool contentChanged = !string.Equals(oldContent, newContent, System.StringComparison.Ordinal);

            if (contentChanged)
            {
                File.WriteAllText(fullPath, newContent, Encoding.UTF8);
                AssetDatabase.Refresh();
            }

            if (File.Exists(fullPath))
            {
                File.SetAttributes(fullPath, File.GetAttributes(fullPath) | FileAttributes.ReadOnly);
            }

            if (contentChanged)
            {
                Debug.Log($"[RedDot] Code generated successfully: Assets/{config.OutputPath}");
            }
            if (showDialog)
            {
                string message = contentChanged
                    ? $"Code generated successfully!\n\nPath: Assets/{config.OutputPath}"
                    : $"Code is already up to date.\n\nPath: Assets/{config.OutputPath}";
                EditorUtility.DisplayDialog("Success", message, "OK");
            }
        }

        private static void OnCompilationFinished(object context)
        {
            if (s_isGeneratingFromCompilation)
            {
                return;
            }

            s_isGeneratingFromCompilation = true;
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:RedDotTreeConfig");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var config = AssetDatabase.LoadAssetAtPath<RedDotTreeConfig>(path);
                    GenerateCode(config, false);
                }
            }
            finally
            {
                s_isGeneratingFromCompilation = false;
            }
        }

        private static void AssignIds(RedDotTreeConfig config)
        {
            s_nextId = 1;
            foreach (var root in config.RootNodes)
            {
                AssignNodeId(root);
            }
        }

        private static void AssignNodeId(RedDotNodeConfig node)
        {
            node.generatedId = s_nextId++;
            foreach (var child in node.children)
            {
                AssignNodeId(child);
            }
        }

        private static void GenerateRegisterAllMethod(StringBuilder sb, RedDotTreeConfig config)
        {
            string indent = "        ";

            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// 注册所有红点节点到 RedDotModule（零 GC）");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public static void RegisterAll()");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}\tvar mgr = RedDotModule.Instance;");
            sb.AppendLine();

            foreach (var root in config.RootNodes)
            {
                GenerateRegisterCode(sb, root, indent + "    ", string.Empty);
            }

            sb.AppendLine($"{indent}}}");
        }

        private static void GenerateRegisterCode(StringBuilder sb, RedDotNodeConfig node, string indent, string parentAccessPath)
        {
            string nodeName = SanitizeName(node.name);
            string accessPath = string.IsNullOrEmpty(parentAccessPath) ? nodeName : $"{parentAccessPath}.{nodeName}";
            bool hasChildren = node.children.Count > 0;

            string idExpr = hasChildren ? $"{accessPath}.Id" : accessPath;
            string pathExpr = hasChildren ? $"{accessPath}.Path" : $"{accessPath}Path";
            string segmentsExpr = hasChildren ? $"{accessPath}.Segments" : $"{accessPath}Segments";

            if (!string.IsNullOrEmpty(node.description))
            {
                sb.AppendLine($"{indent}// {node.description}");
            }

            sb.AppendLine($"{indent}mgr.Register({idExpr}, {pathExpr}, {segmentsExpr}, RedDotType.{node.type}, RedDotAggregateStrategy.{node.strategy});");

            if (hasChildren)
            {
                sb.AppendLine();
                foreach (var child in node.children)
                {
                    GenerateRegisterCode(sb, child, indent, accessPath);
                }
            }
        }

        private static void GenerateNodeCode(StringBuilder sb, RedDotNodeConfig node, int indentLevel)
        {
            string indent = new string('\t', indentLevel);
            string nodeName = SanitizeName(node.name);
            string segmentsArray = GenerateSegmentsArray(node.generatedPath);

            if (node.children.Count > 0)
            {
                sb.AppendLine();

                if (!string.IsNullOrEmpty(node.description))
                {
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {node.description}");
                    sb.AppendLine($"{indent}/// </summary>");
                }

                sb.AppendLine($"{indent}public static class {nodeName}");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}\tpublic const int Id = {node.generatedId};");
                sb.AppendLine($"{indent}\tpublic const string Path = \"{node.generatedPath}\";");
                sb.AppendLine($"{indent}\tpublic static readonly string[] Segments = {segmentsArray};");

                foreach (var child in node.children)
                {
                    GenerateNodeCode(sb, child, indentLevel + 1);
                }

                sb.AppendLine($"{indent}}}");
            }
            else
            {
                if (!string.IsNullOrEmpty(node.description))
                {
                    sb.AppendLine();
                    sb.AppendLine($"{indent}/// <summary>");
                    sb.AppendLine($"{indent}/// {node.description}");
                    sb.AppendLine($"{indent}/// </summary>");
                }

                sb.AppendLine($"{indent}public const int {nodeName} = {node.generatedId};");
                sb.AppendLine($"{indent}public const string {nodeName}Path = \"{node.generatedPath}\";");
                sb.AppendLine($"{indent}public static readonly string[] {nodeName}Segments = {segmentsArray};");
            }
        }

        private static string GenerateSegmentsArray(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "{ }";
            }

            string[] segments = path.Split('/');
            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");

            for (int i = 0; i < segments.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append($"\"{segments[i]}\"");
            }

            sb.Append(" }");
            return sb.ToString();
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unknown";
            }

            StringBuilder sb = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                    capitalizeNext = false;
                }
                else if (c == '_' || c == ' ' || c == '-')
                {
                    capitalizeNext = true;
                }
            }

            string result = sb.ToString();
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            return result;
        }
    }
}

#endif
