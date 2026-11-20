using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.Compilation;

namespace Unity.Pipeline.Telemetry
{
    /// <summary>
    /// What one eval records about itself for the command-executed analytics event. Filled while
    /// the eval runs and read once its execution ends.
    /// </summary>
    sealed class EvalAnalyticsDetails
    {
        /// <summary>
        /// Whether evals record their details. The Editor sets it to follow its analytics; off
        /// otherwise, so a Player records nothing.
        /// </summary>
        internal static Func<bool> IsCollecting { get; set; } = () => false;

        /// <summary>The Unity members the eval resolved to, as <c>Namespace.Type.Member</c>, distinct and sorted.</summary>
        public IReadOnlyList<string> UnityApisUsed { get; set; } = Array.Empty<string>();

        /// <summary>The ids of the final compile's errors, such as <c>CS0103</c>, distinct and sorted.</summary>
        public IReadOnlyList<string> CompileErrorCodes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The full name of the exception the eval threw when it is a system or Unity type,
        /// <see cref="UserDefinedExceptionType"/> for any other type; null when nothing was thrown.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// Why the eval failed, as its error code (such as <c>COMPILATION_ERROR</c>); null when it
        /// succeeded. The eval returns only an exit code, so it records the finer category here.
        /// </summary>
        public string FailureKind { get; set; }

        /// <summary>Reported in place of an exception type that is neither a system nor a Unity type.</summary>
        internal const string UserDefinedExceptionType = "UserDefinedException";

        /// <summary>
        /// Records the Unity members and the error ids of the eval's final compile. Never throws:
        /// what fails to record is left empty, and the eval goes on.
        /// </summary>
        internal void RecordCompilation(CompilationResult compilation)
        {
            try
            {
                CompileErrorCodes = compilation.Diagnostics
                    .Where(d => d.Severity == "error")
                    .Select(d => d.Id)
                    .Distinct()
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray();
                UnityApisUsed = compilation.ListUnityApis?.Invoke() ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                // Reporting must never break the eval.
                UnityEngine.Debug.LogWarning($"Pipeline analytics failed to record an eval's compile: {ex.Message}");
            }
        }

        /// <summary>Records the type of the exception the eval threw. Never throws.</summary>
        internal void RecordException(Exception exception, Assembly evalAssembly)
        {
            try
            {
                ExceptionType = ExceptionTypeName(exception.GetType(), evalAssembly);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Pipeline analytics failed to record an eval's exception: {ex.Message}");
            }
        }

        /// <summary>
        /// How <paramref name="type"/> is reported: its full name for a system or Unity type, which
        /// says nothing about the project, and <see cref="UserDefinedExceptionType"/> otherwise.
        /// </summary>
        /// <param name="type">The thrown exception's type.</param>
        /// <param name="evalAssembly">The eval's own assembly, whose types are always user defined.</param>
        internal static string ExceptionTypeName(Type type, Assembly evalAssembly)
        {
            var assembly = type.Assembly;
            var name = assembly.GetName().Name;
            var known = name == "mscorlib" || name == "netstandard" || name == "System"
                || name.StartsWith("System.", StringComparison.Ordinal) || IsUnityAssembly(name);
            return known && assembly != evalAssembly ? type.FullName : UserDefinedExceptionType;
        }

        /// <summary>
        /// Whether an assembly is part of Unity: named <c>UnityEngine*</c>, <c>UnityEditor*</c> or
        /// <c>Unity.*</c>, and shipped with the Editor or in a package from Unity's registry. A project
        /// assembly can take any name, so the name alone is not enough. Always false outside the Editor.
        /// </summary>
        internal static bool IsUnityAssembly(string name)
        {
            if (!HasUnityName(name))
                return false;
#if UNITY_EDITOR
            s_UnityAssemblies ??= FindUnityAssemblies();
            return s_UnityAssemblies.Contains(name);
#else
            return false;
#endif
        }

        static bool HasUnityName(string name)
        {
            return name != null
                && (name.StartsWith("UnityEngine", StringComparison.Ordinal)
                    || name.StartsWith("UnityEditor", StringComparison.Ordinal)
                    || name.StartsWith("Unity.", StringComparison.Ordinal));
        }

#if UNITY_EDITOR
        // A domain reload, which any package change causes, resets it. Internal so tests can reset it too.
        internal static HashSet<string> s_UnityAssemblies;

        static HashSet<string> FindUnityAssemblies()
        {
            var editorFolder = System.IO.Path.GetFullPath(UnityEditor.EditorApplication.applicationContentsPath)
                .TrimEnd('/', '\\') + System.IO.Path.DirectorySeparatorChar;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in PipelineUtils.GetLoadedAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;
                var name = assembly.GetName().Name;
                // An assembly loaded from bytes, such as an internals eval's, which takes a Unity test
                // assembly's name, has no file, so it is neither shipped with the Editor nor in a package.
                var path = PipelineUtils.GetLoadedAssemblyPath(assembly);
                if (HasUnityName(name) && !string.IsNullOrEmpty(path) && (IsUnder(path, editorFolder) || IsFromUnityPackage(assembly)))
                    names.Add(name);
            }

            return names;
        }

        static bool IsUnder(string path, string folder)
        {
            return System.IO.Path.GetFullPath(path).StartsWith(folder, StringComparison.OrdinalIgnoreCase);
        }

        static bool IsFromUnityPackage(Assembly assembly)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
            return package != null
                && (package.source == UnityEditor.PackageManager.PackageSource.BuiltIn
                    || package.source == UnityEditor.PackageManager.PackageSource.Registry && package.registry != null && package.registry.isDefault);
        }
#endif
    }
}
