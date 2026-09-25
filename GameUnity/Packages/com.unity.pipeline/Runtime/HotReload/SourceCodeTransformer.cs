using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Pipeline.Compilation;
using UnityEngine;

namespace Unity.Pipeline.HotReload
{
    /// <summary>
    /// Transforms [HotReload] method bodies (edited in place on a MonoBehaviour) into static
    /// hot reload override methods that the registry can dispatch to.
    ///
    /// The transform is driven by a Roslyn <see cref="SemanticModel"/>: every identifier that binds
    /// to an instance member of the declaring type (or a base, e.g. <c>transform</c> on Component) is
    /// rewritten to <c>instance.&lt;member&gt;</c>; locals, parameters, static members and Unity
    /// built-ins are left untouched. The output mirrors the helper workflow's override shape:
    ///
    ///     [HotReloadOverrideMethod("Type.Method")]
    ///     public static &lt;ret&gt; Method(Type instance, &lt;params&gt;) { ...rewritten body... }
    /// </summary>
    public static class SourceCodeTransformer
    {
        /// <summary>
        /// Transform the [HotReload] methods named in <paramref name="methodBodies"/> into a
        /// static override class. <paramref name="originalTypeDefinition"/> (the full source of the
        /// file) is required so the semantic model can resolve member bindings in context.
        /// </summary>
        /// <param name="emitLineDirectives">
        /// When true, each rewritten body is bracketed by <c>#line</c> directives that map it back to
        /// the original source file so an attached debugger binds breakpoints in the file the user
        /// edited. Requires <paramref name="originalFilePath"/>. The body is emitted with its original
        /// whitespace (no <c>NormalizeWhitespace</c>) so line positions survive the transform.
        /// </param>
        /// <param name="originalFilePath">Absolute path of the source file, recorded in the #line directives.</param>
        public static string TransformMethodBodies(
            Dictionary<string, string> methodBodies,
            string originalTypeName,
            Dictionary<string, MethodSignatureInfo> originalMethodSignatures,
            string originalTypeDefinition = null,
            bool emitLineDirectives = false,
            string originalFilePath = null)
        {
            if (string.IsNullOrEmpty(originalTypeDefinition))
            {
                throw new InvalidOperationException(
                    "In-place transformation requires the full original source to build a semantic model.");
            }

            try
            {
                var tree = CSharpSyntaxTree.ParseText(originalTypeDefinition);
                var root = tree.GetRoot();

                var compilation = CSharpCompilation.Create(
                    "HotReloadInPlaceTransform",
                    new[] { tree },
                    RoslynCompilationService.GetMetadataReferences(),
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var model = compilation.GetSemanticModel(tree);

                var classDecl = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.ValueText == originalTypeName);
                if (classDecl == null)
                {
                    throw new InvalidOperationException($"Could not find class '{originalTypeName}' in source.");
                }

                var classSymbol = model.GetDeclaredSymbol(classDecl);
                var rewriter = new InstanceMemberQualifier(model, classSymbol);

                var sb = new StringBuilder();
                foreach (var u in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
                    sb.AppendLine(u.ToString());
                sb.AppendLine("using Unity.Pipeline.HotReload;");

                var namespaceName = GetNamespace(classDecl);
                if (!string.IsNullOrEmpty(namespaceName))
                    sb.AppendLine($"using {namespaceName};");
                sb.AppendLine();

                sb.AppendLine($"public static class {originalTypeName}HotReloadOverrides");
                sb.AppendLine("{");

                foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    var methodName = method.Identifier.ValueText;
                    if (!methodBodies.ContainsKey(methodName) || method.Body == null)
                        continue;

                    var rewrittenBody = (BlockSyntax)rewriter.Visit(method.Body);
                    var returnType = method.ReturnType.ToString();

                    var parameters = new List<string> { $"{originalTypeName} instance" };
                    parameters.AddRange(method.ParameterList.Parameters.Select(
                        p => $"{p.Type} {p.Identifier.ValueText}"));

                    sb.AppendLine($"    [HotReloadOverrideMethod(\"{originalTypeName}.{methodName}\")]");
                    sb.AppendLine($"    public static {returnType} {methodName}({string.Join(", ", parameters)})");

                    if (emitLineDirectives && !string.IsNullOrEmpty(originalFilePath))
                    {
                        // Map the body back to the original file. The rewriter only makes intra-line
                        // edits, so a single #line at the body's opening brace maps every line of the
                        // block; emit the body with original trivia (not NormalizeWhitespace) to keep
                        // line counts intact. #line hidden masks the generated scaffolding that follows.
                        var bodyLine = method.Body.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        var escapedPath = originalFilePath.Replace("\\", "\\\\");
                        sb.AppendLine($"#line {bodyLine} \"{escapedPath}\"");
                        sb.AppendLine(rewrittenBody.ToString());
                        sb.AppendLine("#line hidden");
                    }
                    else
                    {
                        sb.AppendLine("    " + rewrittenBody.NormalizeWhitespace().ToString());
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("}");

                var result = sb.ToString();
                Debug.Log($"HotReload: Transformed {methodBodies.Count} in-place method(s) for {originalTypeName}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error transforming in-place methods for {originalTypeName}: {ex.Message}");
                throw new InvalidOperationException(
                    $"Failed to transform in-place methods for {originalTypeName}: {ex.Message}", ex);
            }
        }

        private static string GetNamespace(SyntaxNode node)
        {
            for (var current = node.Parent; current != null; current = current.Parent)
            {
                if (current is NamespaceDeclarationSyntax ns)
                    return ns.Name.ToString();
            }
            return null;
        }

        /// <summary>
        /// Rewrites implicit instance-member access (this.x or bare x) to instance.x using the
        /// semantic model. Locals, parameters, static members and built-ins are left untouched.
        /// </summary>
        private class InstanceMemberQualifier : CSharpSyntaxRewriter
        {
            private readonly SemanticModel m_Model;
            private readonly INamedTypeSymbol m_Type;

            public InstanceMemberQualifier(SemanticModel model, INamedTypeSymbol type)
            {
                m_Model = model;
                m_Type = type;
            }

            public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node)
            {
                return SyntaxFactory.IdentifierName("instance").WithTriviaFrom(node);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (ShouldSkip(node))
                    return base.VisitIdentifierName(node);

                if (IsImplicitInstanceMember(node))
                    return Qualify(node);

                return base.VisitIdentifierName(node);
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                // e.g. GetComponent<Renderer>() — visit type arguments first.
                var visited = (GenericNameSyntax)base.VisitGenericName(node);

                if (ShouldSkip(node))
                    return visited;

                if (IsImplicitInstanceMember(node))
                    return Qualify(visited);

                return visited;
            }

            private static bool ShouldSkip(SimpleNameSyntax node)
            {
                // Right-hand side of a member access (foo.Bar -> Bar), or part of a qualified name.
                if (node.Parent is MemberAccessExpressionSyntax ma && ma.Name == node)
                    return true;
                if (node.Parent is QualifiedNameSyntax)
                    return true;
                if (node.Parent is MemberBindingExpressionSyntax)
                    return true;
                return false;
            }

            private bool IsImplicitInstanceMember(SimpleNameSyntax node)
            {
                var symbol = m_Model.GetSymbolInfo(node).Symbol;
                if (symbol == null || symbol.IsStatic)
                    return false;

                switch (symbol.Kind)
                {
                    case SymbolKind.Field:
                    case SymbolKind.Property:
                    case SymbolKind.Method:
                    case SymbolKind.Event:
                        return InTypeHierarchy(symbol.ContainingType);
                    default:
                        return false;
                }
            }

            private bool InTypeHierarchy(INamedTypeSymbol containingType)
            {
                if (containingType == null)
                    return false;
                for (var t = m_Type; t != null; t = t.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(t, containingType))
                        return true;
                }
                return false;
            }

            private static SyntaxNode Qualify(SimpleNameSyntax node)
            {
                return SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("instance"),
                        node.WithoutTrivia())
                    .WithTriviaFrom(node);
            }
        }
    }

    /// <summary>
    /// Information about a method's signature for transformation purposes.
    /// </summary>
    public class MethodSignatureInfo
    {
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
        public bool ReturnsValue => !string.IsNullOrEmpty(ReturnType) && ReturnType != "void";
    }

    /// <summary>
    /// Information about a method parameter.
    /// </summary>
    public class ParameterInfo
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool HasDefaultValue { get; set; }
        public string DefaultValue { get; set; }
    }
}
