using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unity.Pipeline.HotReload
{
    /// <summary>
    /// Up-front, parse-only validation for the helper (separate override file) workflow used by
    /// the <c>reload_file_override</c> command. Catches the common setup mistakes before compilation so
    /// the user gets an actionable message instead of a confusing compiler error.
    ///
    /// Rules:
    ///  1. The file must contain at least one method annotated [HotReloadOverrideMethod(...)].
    ///  2. The file must not redeclare a type that an override targets (the override's first
    ///     parameter type). Redeclaring the target type binds the override to a duplicate type
    ///     and the registration silently mismatches.
    ///  3. Each [HotReloadOverrideMethod] method must be 'public static' and take the instance as its
    ///     first parameter.
    /// </summary>
    public static class OverrideFileValidator
    {
        public static OverrideFileValidationResult Validate(string sourceCode, string displayName)
        {
            var result = new OverrideFileValidationResult { DisplayName = displayName };

            var root = CSharpSyntaxTree.ParseText(sourceCode ?? string.Empty).GetRoot();

            // All type names declared in this file (class/struct), by simple name.
            var declaredTypeNames = new HashSet<string>(root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Select(t => t.Identifier.ValueText));

            // All methods annotated [HotReloadOverrideMethod(...)].
            var overrideMethods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(HasHotReloadOverrideMethodAttribute)
                .ToList();

            // Rule 1: must contain at least one override method.
            if (overrideMethods.Count == 0)
            {
                result.Errors.Add(
                    $"No [HotReloadOverrideMethod] overrides found in '{displayName}'. An override file must contain " +
                    "public static methods annotated [HotReloadOverrideMethod(\"Type.Method\")]. " +
                    "If you meant to edit a method body directly in its own source file, that is the in-place " +
                    "workflow (reload_file), not reload_file_override.");
                return result;
            }

            foreach (var method in overrideMethods)
            {
                var name = method.Identifier.ValueText;
                var modifiers = method.Modifiers.Select(m => m.ValueText).ToList();

                // Rule 3: must be public static.
                if (!modifiers.Contains("static") || !modifiers.Contains("public"))
                {
                    result.Errors.Add(
                        $"Override method '{name}' must be declared 'public static'.");
                }

                // Rule 3: must take the instance as its first parameter.
                var parameters = method.ParameterList.Parameters;
                if (parameters.Count == 0)
                {
                    result.Errors.Add(
                        $"Override method '{name}' must take the target instance as its first parameter, " +
                        $"e.g. 'public static void {name}(MyComponent instance, ...)'.");
                    continue;
                }

                // Rule 2: the file must not redeclare the target type (the first parameter type).
                var targetTypeName = GetSimpleTypeName(parameters[0].Type);
                if (targetTypeName != null && declaredTypeNames.Contains(targetTypeName))
                {
                    result.Errors.Add(
                        $"'{displayName}' declares '{targetTypeName}', which is the type that override " +
                        $"'{name}' targets. Put the override in a separate file (any folder) that does not " +
                        $"redeclare your gameplay type '{targetTypeName}'.");
                }
            }

            return result;
        }

        private static bool HasHotReloadOverrideMethodAttribute(MethodDeclarationSyntax method)
        {
            return method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("HotReloadOverrideMethod"));
        }

        /// <summary>
        /// Extract the simple (unqualified) name of a parameter type, e.g. "BossController" from
        /// "Namespace.BossController". Returns null for types we cannot reduce to a single name.
        /// </summary>
        private static string GetSimpleTypeName(TypeSyntax type)
        {
            switch (type)
            {
                case IdentifierNameSyntax id:
                    return id.Identifier.ValueText;
                case QualifiedNameSyntax qualified:
                    return qualified.Right.Identifier.ValueText;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Result of validating a helper-workflow override file.
    /// </summary>
    public class OverrideFileValidationResult
    {
        public string DisplayName { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public string GetFormattedErrorMessage()
        {
            if (IsValid)
            {
                return "Override file is valid.";
            }

            var message = $"reload_file_override validation failed for '{DisplayName}': {Errors.Count} issue(s)\n";
            for (int i = 0; i < Errors.Count; i++)
            {
                message += $"\n{i + 1}. {Errors[i]}";
            }
            return message;
        }
    }
}
