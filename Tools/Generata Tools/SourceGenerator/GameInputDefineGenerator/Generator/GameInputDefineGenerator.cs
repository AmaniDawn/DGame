using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GameInputDefineGenerator.Generator;

[Generator]
public class GameInputDefineGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var inputActions = CollectButtonActions(context.Compilation);
        if (inputActions.Count == 0)
        {
            return;
        }

        context.AddSource(Definition.InputDefineFileName, GenerateInputDefineSource(inputActions));
    }

    private static List<InputActionInfo> CollectButtonActions(Compilation compilation)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            var classNode = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == Definition.TargetClassName);

            if (classNode == null)
            {
                continue;
            }

            var namespaceNode = classNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (namespaceNode == null || namespaceNode.Name.ToString() != Definition.TargetNamespace)
            {
                continue;
            }

            var constructorNode = classNode.Members
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == Definition.TargetClassName);

            if (constructorNode == null)
            {
                continue;
            }

            var invocationNode = constructorNode.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(IsFromJsonInvocation);

            if (invocationNode == null || invocationNode.ArgumentList.Arguments.Count == 0)
            {
                continue;
            }

            var jsonLiteral = invocationNode.ArgumentList.Arguments[0].Expression as LiteralExpressionSyntax;
            if (jsonLiteral == null)
            {
                continue;
            }

            return ParseButtonActions(jsonLiteral.Token.ValueText);
        }

        return new List<InputActionInfo>();
    }

    private static bool IsFromJsonInvocation(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        return memberAccess.Name.Identifier.ValueText == "FromJson";
    }

    private static List<InputActionInfo> ParseButtonActions(string jsonText)
    {
        var result = new List<InputActionInfo>();
        var enumMemberNameSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var mapName in Definition.TargetMapNames)
        {
            var actionsContent = ExtractActionsContent(jsonText, mapName);
            if (string.IsNullOrEmpty(actionsContent))
            {
                continue;
            }

            var actionBlocks = ExtractObjectBlocks(actionsContent);
            foreach (var actionBlock in actionBlocks)
            {
                var typeValue = ExtractJsonStringValue(actionBlock, "type");
                if (!string.Equals(typeValue, "Button", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var actionName = ExtractJsonStringValue(actionBlock, "name");
                if (string.IsNullOrWhiteSpace(actionName))
                {
                    continue;
                }

                var enumMemberName = SanitizeIdentifier(actionName);
                if (enumMemberName.Length == 0 || !enumMemberNameSet.Add(enumMemberName))
                {
                    continue;
                }

                result.Add(new InputActionInfo(mapName, actionName, enumMemberName));
            }
        }

        return result;
    }

    private static string ExtractActionsContent(string jsonText, string targetMapName)
    {
        var mapPattern = $"\"name\": \"{targetMapName}\"";
        var mapNameIndex = jsonText.IndexOf(mapPattern, StringComparison.Ordinal);
        if (mapNameIndex < 0)
        {
            return string.Empty;
        }

        var actionsPattern = "\"actions\": [";
        var actionsStartIndex = jsonText.IndexOf(actionsPattern, mapNameIndex, StringComparison.Ordinal);
        if (actionsStartIndex < 0)
        {
            return string.Empty;
        }

        var arrayStartIndex = jsonText.IndexOf('[', actionsStartIndex);
        if (arrayStartIndex < 0)
        {
            return string.Empty;
        }

        var arrayEndIndex = FindMatchingBracket(jsonText, arrayStartIndex, '[', ']');
        if (arrayEndIndex < 0 || arrayEndIndex <= arrayStartIndex)
        {
            return string.Empty;
        }

        return jsonText.Substring(arrayStartIndex + 1, arrayEndIndex - arrayStartIndex - 1);
    }

    private static List<string> ExtractObjectBlocks(string content)
    {
        var result = new List<string>();
        var index = 0;

        while (index < content.Length)
        {
            var objectStartIndex = content.IndexOf('{', index);
            if (objectStartIndex < 0)
            {
                break;
            }

            var objectEndIndex = FindMatchingBracket(content, objectStartIndex, '{', '}');
            if (objectEndIndex < 0)
            {
                break;
            }

            result.Add(content.Substring(objectStartIndex, objectEndIndex - objectStartIndex + 1));
            index = objectEndIndex + 1;
        }

        return result;
    }

    private static int FindMatchingBracket(string content, int startIndex, char openChar, char closeChar)
    {
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = startIndex; i < content.Length; i++)
        {
            var ch = content[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (ch == openChar)
            {
                depth++;
            }
            else if (ch == closeChar)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string ExtractJsonStringValue(string content, string key)
    {
        var keyPattern = $"\"{key}\":";
        var keyIndex = content.IndexOf(keyPattern, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return string.Empty;
        }

        var firstQuoteIndex = content.IndexOf('"', keyIndex + keyPattern.Length);
        if (firstQuoteIndex < 0)
        {
            return string.Empty;
        }

        var valueBuilder = new StringBuilder();
        var escape = false;

        for (var i = firstQuoteIndex + 1; i < content.Length; i++)
        {
            var ch = content[i];

            if (escape)
            {
                valueBuilder.Append(ch);
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '"')
            {
                return valueBuilder.ToString();
            }

            valueBuilder.Append(ch);
        }

        return string.Empty;
    }

    private static string SanitizeIdentifier(string sourceName)
    {
        var builder = new StringBuilder(sourceName.Length);
        var makeNextUpper = true;

        foreach (var ch in sourceName)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                if (builder.Length == 0 && char.IsDigit(ch))
                {
                    builder.Append('_');
                }

                builder.Append(makeNextUpper ? char.ToUpperInvariant(ch) : ch);
                makeNextUpper = false;
            }
            else
            {
                makeNextUpper = true;
            }
        }

        return builder.ToString();
    }

    private static string GenerateInputDefineSource(IReadOnlyList<InputActionInfo> inputActions)
    {
        var builder = new StringBuilder();
        builder.AppendLine("//----------------------------------------------------------");
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// \tThis code was generated by the source generator.");
        builder.AppendLine("// \tChanges to this file may cause incorrect behavior.");
        builder.AppendLine("// \twill be lost if the code is regenerated.");
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("//----------------------------------------------------------");
        builder.AppendLine();
        builder.AppendLine("using UnityEngine.InputSystem.Interactions;");
        builder.AppendLine();
        builder.AppendLine($"namespace {Definition.TargetNamespace}");
        builder.AppendLine("{");
        builder.AppendLine($"\tpublic enum {Definition.TargetEnumName}");
        builder.AppendLine("\t{");
        
        builder.AppendLine($"\t\tNone = 0,");
        for (var i = 0; i < inputActions.Count; i++)
        {
            builder.AppendLine($"\t\t{inputActions[i].EnumMemberName} = {i + 1},");
        }

        builder.AppendLine("\t}");
        builder.AppendLine();
        builder.AppendLine($"\tpublic partial class {Definition.TargetInputDefineClassName}");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\tpublic static void RegisterInputActions()");
        builder.AppendLine("\t\t{");

        foreach (var inputAction in inputActions)
        {
            builder.AppendLine($"\t\t\tm_inputActions.{inputAction.MapName}.{inputAction.ActionName}.started += (ctx) =>");
            builder.AppendLine("\t\t\t{");
            builder.AppendLine($"\t\t\t\tGameModule.Input.ReceiveInputAction(InputEventType.{inputAction.EnumMemberName}, InputState.Started, ctx.time);");
            builder.AppendLine("\t\t\t};");
            builder.AppendLine($"\t\t\tm_inputActions.{inputAction.MapName}.{inputAction.ActionName}.performed += (ctx) =>");
            builder.AppendLine("\t\t\t{");
            builder.AppendLine("\t\t\t\tif (ctx.interaction is HoldInteraction)");
            builder.AppendLine("\t\t\t\t{");
            builder.AppendLine($"\t\t\t\t\tGameModule.Input.ReceiveInputAction(InputEventType.{inputAction.EnumMemberName}, InputState.Performed, ctx.time);");
            builder.AppendLine("\t\t\t\t}");
            builder.AppendLine("\t\t\t};");
            builder.AppendLine($"\t\t\tm_inputActions.{inputAction.MapName}.{inputAction.ActionName}.canceled += (ctx) =>");
            builder.AppendLine("\t\t\t{");
            builder.AppendLine($"\t\t\t\tGameModule.Input.ReceiveInputAction(InputEventType.{inputAction.EnumMemberName}, InputState.Canceled, ctx.time);");
            builder.AppendLine("\t\t\t};");
            builder.AppendLine();
        }

        builder.AppendLine("\t\t}");
        builder.AppendLine("\t}");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private struct InputActionInfo
    {
        public InputActionInfo(string mapName, string actionName, string enumMemberName)
        {
            MapName = mapName;
            ActionName = actionName;
            EnumMemberName = enumMemberName;
        }

        public string MapName { get; }

        public string ActionName { get; }

        public string EnumMemberName { get; }
    }
}

