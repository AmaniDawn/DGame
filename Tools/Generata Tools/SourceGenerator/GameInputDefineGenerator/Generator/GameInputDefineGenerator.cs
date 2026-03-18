using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        var actionNames = CollectButtonActionNames(context.Compilation);
        if (actionNames.Count == 0)
        {
            return;
        }

        context.AddSource(Definition.TargetFileName, GenerateEnumSource(actionNames));
    }

    private static List<string> CollectButtonActionNames(Compilation compilation)
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

            var jsonText = jsonLiteral.Token.ValueText;
            return ParseButtonActionNames(jsonText);
        }

        return new List<string>();
    }

    private static bool IsFromJsonInvocation(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        return memberAccess.Name.Identifier.ValueText == "FromJson";
    }

    private static List<string> ParseButtonActionNames(string jsonText)
    {
        var result = new List<string>();
        var memberNameSet = new HashSet<string>(StringComparer.Ordinal);

        using var document = JsonDocument.Parse(jsonText);
        if (!document.RootElement.TryGetProperty("maps", out var mapsElement) || mapsElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var mapElement in mapsElement.EnumerateArray())
        {
            if (!mapElement.TryGetProperty("name", out var mapNameElement) ||
                !Definition.TargetMapNames.Contains(mapNameElement.GetString() ?? string.Empty))
            {
                continue;
            }

            if (!mapElement.TryGetProperty("actions", out var actionsElement) || actionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var actionElement in actionsElement.EnumerateArray())
            {
                if (!actionElement.TryGetProperty("type", out var typeElement) ||
                    !string.Equals(typeElement.GetString(), "Button", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!actionElement.TryGetProperty("name", out var nameElement))
                {
                    continue;
                }

                var actionName = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(actionName))
                {
                    continue;
                }

                var memberName = SanitizeEnumMemberName(actionName!);
                if (memberName.Length == 0 || !memberNameSet.Add(memberName))
                {
                    continue;
                }

                result.Add(memberName);
            }
        }

        return result;
    }

    private static string SanitizeEnumMemberName(string sourceName)
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

    private static string GenerateEnumSource(IReadOnlyList<string> actionNames)
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
        builder.AppendLine($"namespace {Definition.TargetNamespace}");
        builder.AppendLine("{");
        builder.AppendLine($"\t/// <summary>");
        builder.AppendLine($"\t/// 输入动作枚举");
        builder.AppendLine($"\t/// </summary>");
        builder.AppendLine($"\tpublic enum {Definition.TargetEnumName}");
        builder.AppendLine("\t{");

        for (var i = 0; i < actionNames.Count; i++)
        {
            builder.AppendLine($"\t\t{actionNames[i]} = {i},");
        }

        builder.AppendLine("\t}");
        builder.AppendLine("}");
        return builder.ToString();
    }
}



