using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.HotReload;
using UnityEngine;
#if UNITY_6000_3_OR_NEWER
using UnityEngine.Assemblies;
#endif

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Registry for discovering and managing CLI commands via pluggable discovery mechanism.
    /// Scans for methods marked with [CliCommand] attribute across all assemblies.
    /// Based on unity-tools ToolRegistry patterns adapted for Pipeline requirements.
    /// </summary>
    public static class CommandRegistry
    {
        private static IReadOnlyList<CommandInfo> m_CachedCommands;
        private static ICommandDiscovery m_Discovery;

        /// <summary>
        /// Set the command discovery mechanism.
        /// Editor assembly provides TypeCache-based discovery, Runtime uses reflection fallback.
        /// </summary>
        public static void SetDiscovery(ICommandDiscovery discovery)
        {
            m_Discovery = discovery;
            ClearCache(); // Re-discover with new mechanism
        }

        /// <summary>
        /// Discover all CLI commands marked with [CliCommand] attribute.
        /// Uses injected discovery mechanism (TypeCache in Editor, reflection in Runtime).
        /// Results are cached until domain reload.
        /// </summary>
        public static IEnumerable<CommandInfo> DiscoverCommands()
        {
            if (m_CachedCommands == null)
            {
                m_CachedCommands = DiscoverCommandsInternal().ToList();

                // Also discover hot reload methods when commands are discovered
                DiscoverHotReloadMethods();
            }

            return m_CachedCommands;
        }

        /// <summary>
        /// Clear the command cache. Called automatically on domain reload.
        /// Can be called manually for testing or dynamic command registration.
        /// </summary>
        public static void ClearCache()
        {
            m_CachedCommands = null;
        }

        /// <summary>
        /// Internal implementation of command discovery using injected discovery mechanism.
        /// </summary>
        private static IEnumerable<CommandInfo> DiscoverCommandsInternal()
        {
            var commands = new List<CommandInfo>();

            try
            {
                // Use injected discovery mechanism if available, otherwise fallback to reflection
                IEnumerable<MethodInfo> methods;

                if (m_Discovery != null)
                {
                    methods = m_Discovery.GetMethodsWithAttribute<CliCommandAttribute>();
                }
                else
                {
                    // Fallback: Use reflection to scan loaded assemblies
                    methods = GetMethodsWithAttributeViaReflection<CliCommandAttribute>();
                }

                foreach (var method in methods)
                {
                    try
                    {
                        var commandInfo = CreateCommandInfo(method);
                        if (commandInfo != null)
                        {
                            commands.Add(commandInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to register command from method {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to discover commands: {ex.Message}");
            }

            return commands;
        }

        /// <summary>
        /// Fallback command discovery using reflection when TypeCache is not available.
        /// </summary>
        private static IEnumerable<MethodInfo> GetMethodsWithAttributeViaReflection<T>() where T : Attribute
        {
            var methods = new List<MethodInfo>();

            try
            {
                var assemblies = PipelineUtils.GetLoadedAssemblies();

                foreach (var assembly in assemblies)
                {
                    // Skip system assemblies for performance
                    if (IsSystemAssembly(assembly))
                        continue;

                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                            {
                                if (method.GetCustomAttribute<T>() != null)
                                {
                                    methods.Add(method);
                                }
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip assemblies that can't be loaded
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Reflection-based command discovery failed: {ex.Message}");
            }

            return methods;
        }

        /// <summary>
        /// Check if assembly is a system assembly that should be skipped during discovery.
        /// </summary>
        private static bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            return name.StartsWith("System.") ||
                   name.StartsWith("Microsoft.") ||
                   name.StartsWith("mscorlib") ||
                   name.StartsWith("netstandard") ||
                   name.Equals("UnityEngine") ||
                   name.Equals("UnityEditor");
        }

        /// <summary>
        /// Create CommandInfo from a method with CliCommand attribute.
        /// </summary>
        private static CommandInfo CreateCommandInfo(MethodInfo method)
        {
            var commandAttr = method.GetCustomAttribute<CliCommandAttribute>();
            if (commandAttr == null)
                return null;

            // Validate method is static (required for CLI commands).
            // Any static method may be registered regardless of accessibility
            // (public, internal, or private) — invocation goes through MethodInfo.Invoke.
            if (!method.IsStatic)
            {
                Debug.LogWarning($"Command method {method.DeclaringType?.Name}.{method.Name} must be static");
                return null;
            }

            // Discover parameters
            var parameters = DiscoverParameters(method).ToList();

            return new CommandInfo(
                commandAttr.Name,
                commandAttr.Description,
                commandAttr.MainThreadRequired,
                method,
                parameters,
                commandAttr.RuntimeOnly
            );
        }

        /// <summary>
        /// Discover parameter information from method parameters.
        /// </summary>
        private static IEnumerable<CommandParameterInfo> DiscoverParameters(MethodInfo method)
        {
            foreach (var param in method.GetParameters())
            {
                var argAttr = param.GetCustomAttribute<CliArgAttribute>();

                // Parameters without CliArg attribute get default metadata
                var name = argAttr?.Name ?? param.Name;
                var description = argAttr?.Description ?? $"Parameter: {param.Name}";
                var required = argAttr?.Required ?? !param.HasDefaultValue;
                var defaultValue = param.HasDefaultValue ? param.DefaultValue : argAttr?.DefaultValue;

                yield return new CommandParameterInfo(name, description, required, param.ParameterType, defaultValue);
            }
        }

        /// <summary>
        /// Discover and register methods marked with [HotReloadWithOverrides] attribute.
        /// Called during command discovery to populate the hot reload registry.
        /// </summary>
        private static void DiscoverHotReloadMethods()
        {
            try
            {
                // Use injected discovery mechanism if available, otherwise fallback to reflection
                IEnumerable<MethodInfo> methods;

                if (m_Discovery != null)
                {
                    methods = m_Discovery.GetMethodsWithAttribute<HotReloadWithOverridesAttribute>();
                }
                else
                {
                    // Fallback: Use reflection to scan loaded assemblies
                    methods = GetMethodsWithAttributeViaReflection<HotReloadWithOverridesAttribute>();
                }

                foreach (var method in methods)
                {
                    try
                    {
                        var hotReloadAttr = method.GetCustomAttribute<HotReloadWithOverridesAttribute>();
                        if (hotReloadAttr != null)
                        {
                            HotReloadRegistry.RegisterReloadableMethod(method, hotReloadAttr);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to register hot reload method {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to discover hot reload methods: {ex.Message}");
            }
        }
    }
}