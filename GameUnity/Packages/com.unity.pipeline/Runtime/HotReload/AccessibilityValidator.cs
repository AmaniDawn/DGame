using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unity.Pipeline.Compilation;
using UnityEngine;

namespace Unity.Pipeline.HotReload
{
    /// <summary>
    /// Validates that [HotReload] method bodies only access PUBLIC instance members of the
    /// declaring type. In-place overrides are compiled into a separate assembly, so they can only
    /// reach public members of the original type; private/internal/protected access would fail to
    /// compile. This check runs up front (via a Roslyn semantic model) to produce a clear message
    /// instead of a raw compiler error.
    /// </summary>
    public static class AccessibilityValidator
    {
        public static AccessibilityValidationResult ValidatePublicAccess(
            string sourceCode,
            Dictionary<string, string> methodBodies,
            string originalTypeName)
        {
            var result = new AccessibilityValidationResult
            {
                IsValid = true,
                Violations = new List<AccessibilityViolation>()
            };

            try
            {
                var tree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = tree.GetRoot();

                var compilation = CSharpCompilation.Create(
                    "HotReloadInPlaceValidation",
                    new[] { tree },
                    RoslynCompilationService.GetMetadataReferences(),
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var model = compilation.GetSemanticModel(tree);

                var classDecl = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.ValueText == originalTypeName);
                if (classDecl == null)
                {
                    result.IsValid = false;
                    result.ValidationError = $"Could not find class '{originalTypeName}' in source.";
                    return result;
                }

                var classSymbol = model.GetDeclaredSymbol(classDecl);

                foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    var methodName = method.Identifier.ValueText;
                    if (!methodBodies.ContainsKey(methodName) || method.Body == null)
                        continue;

                    var seen = new HashSet<string>();
                    foreach (var name in method.Body.DescendantNodes().OfType<SimpleNameSyntax>())
                    {
                        if (IsMemberName(name))
                            continue;

                        var symbol = model.GetSymbolInfo(name).Symbol;
                        if (!IsInstanceMemberOf(symbol, classSymbol))
                            continue;

                        if (symbol.DeclaredAccessibility != Accessibility.Public && seen.Add(symbol.Name))
                        {
                            result.Violations.Add(new AccessibilityViolation
                            {
                                MemberName = symbol.Name,
                                MethodName = methodName,
                                AccessLevel = symbol.DeclaredAccessibility,
                                ViolationType = AccessibilityViolationType.PrivateAccess,
                                ErrorMessage = $"Cannot access non-public member '{symbol.Name}' " +
                                    $"({symbol.DeclaredAccessibility}) in [HotReload] method '{methodName}'",
                                Suggestion = $"Make '{symbol.Name}' public in {originalTypeName}, or use a " +
                                    "public property/method. In-place overrides compile in a separate assembly " +
                                    "and can only access public members."
                            });
                        }
                    }
                }

                result.IsValid = result.Violations.Count == 0;
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Accessibility validation error: {ex.Message}");
                return new AccessibilityValidationResult
                {
                    IsValid = false,
                    ValidationError = ex.Message,
                    Violations = new List<AccessibilityViolation>()
                };
            }
        }

        /// <summary>True if the name is the right-hand member name of an access (foo.Bar -> Bar).</summary>
        private static bool IsMemberName(SimpleNameSyntax node)
        {
            if (node.Parent is MemberAccessExpressionSyntax ma && ma.Name == node)
                return true;
            if (node.Parent is QualifiedNameSyntax)
                return true;
            if (node.Parent is MemberBindingExpressionSyntax)
                return true;
            return false;
        }

        /// <summary>True if the symbol is an instance field/property/method/event of the type or a base.</summary>
        private static bool IsInstanceMemberOf(ISymbol symbol, INamedTypeSymbol type)
        {
            if (symbol == null || symbol.IsStatic)
                return false;

            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Method:
                case SymbolKind.Event:
                    break;
                default:
                    return false;
            }

            for (var t = type; t != null; t = t.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(t, symbol.ContainingType))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Result of accessibility validation for hot reload methods.
    /// </summary>
    public class AccessibilityValidationResult
    {
        public bool IsValid { get; set; }
        public List<AccessibilityViolation> Violations { get; set; } = new List<AccessibilityViolation>();
        public string ValidationError { get; set; }

        public string GetFormattedErrorMessage()
        {
            if (!string.IsNullOrEmpty(ValidationError))
                return $"HotReload Validation Error: {ValidationError}";

            if (Violations.Count == 0)
                return "All member access is valid for hot reload.";

            var errorMessage = $"HotReload Validation Failed: {Violations.Count} accessibility violation(s) found\n\n";
            for (int i = 0; i < Violations.Count; i++)
            {
                var violation = Violations[i];
                errorMessage += $"{i + 1}. Method '{violation.MethodName}': {violation.ErrorMessage}\n";
                errorMessage += $"   → Suggestion: {violation.Suggestion}\n";
                if (i < Violations.Count - 1)
                    errorMessage += "\n";
            }

            errorMessage += "\nFix these accessibility issues and run reload_file again.";
            return errorMessage;
        }
    }

    /// <summary>
    /// Information about a specific accessibility violation in hot reload code.
    /// </summary>
    public class AccessibilityViolation
    {
        public string MemberName { get; set; }
        public string MethodName { get; set; }
        public Accessibility AccessLevel { get; set; }
        public AccessibilityViolationType ViolationType { get; set; }
        public string ErrorMessage { get; set; }
        public string Suggestion { get; set; }
    }

    /// <summary>
    /// Types of accessibility violations that can occur in hot reload methods.
    /// </summary>
    public enum AccessibilityViolationType
    {
        PrivateAccess,
        InternalAccess,
        ProtectedAccess,
        ParseError
    }
}
