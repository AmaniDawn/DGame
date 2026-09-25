using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.Threading;
using UnityEngine;

namespace Unity.Pipeline.HotReload
{
    /// <summary>
    /// Central registry for managing hot reload method and component overrides.
    /// Handles runtime method resolution, override registration, and fallback logic.
    /// Thread-safe for runtime hot reload operations.
    /// </summary>
    public static class HotReloadRegistry
    {
        /// <summary>
        /// Dispatcher used to marshal main-thread-required hot-reload overrides to the main thread.
        /// Injected by RuntimePipelineManager (= its server's dispatcher). Null = run on the
        /// current thread (no marshaling).
        /// </summary>
        public static Dispatcher Dispatcher { get; set; }

        /// <summary>
        /// Absolute roots a running Player is allowed to hot reload source files from (Assets folder
        /// + loaded package locations). Baked into RuntimePipelineManager at build time (a running
        /// Player cannot resolve the project layout) and published here when the runtime server
        /// starts, so reload commands can validate incoming file paths. Null until published.
        /// </summary>
        public static IReadOnlyList<string> AllowedReloadRoots { get; set; }

        // Thread-safe collections for runtime hot reload switching
        private static readonly ConcurrentDictionary<string, MethodOverride> m_MethodOverrides = new();
        private static readonly ConcurrentDictionary<string, List<MethodInfo>> m_ReloadableMethods = new();
        private static readonly ConcurrentDictionary<string, Type> m_LoadedHotReloadTypes = new();

        /// <summary>
        /// Register a method as hot reloadable. Called during discovery phase.
        /// </summary>
        public static void RegisterReloadableMethod(MethodInfo method, HotReloadWithOverridesAttribute attribute)
        {
            var methodId = GetMethodId(method, attribute.Id);

            if (!m_ReloadableMethods.ContainsKey(methodId))
            {
                m_ReloadableMethods[methodId] = new List<MethodInfo>();
            }

            m_ReloadableMethods[methodId].Add(method);
        }

        /// <summary>
        /// Register a hot reload method override from compiled hot reload assembly.
        /// Returns true if the override was registered, false if it was skipped.
        /// </summary>
        public static bool RegisterMethodOverride(MethodInfo overrideMethod, HotReloadOverrideMethodAttribute attribute, Type sourceType)
        {
            return RegisterMethodOverride(overrideMethod, attribute, sourceType, out _);
        }

        /// <summary>
        /// Register a hot reload method override from compiled hot reload assembly.
        /// Returns true if the override was registered, false if it was skipped (target not
        /// reloadable or signature mismatch). The out parameter carries a user-facing reason
        /// when registration is skipped.
        /// </summary>
        public static bool RegisterMethodOverride(MethodInfo overrideMethod, HotReloadOverrideMethodAttribute attribute, Type sourceType, out string skipReason)
        {
            skipReason = null;
            var targetMethodId = attribute.TargetMethodId;

            // Validate that target method exists and is reloadable
            if (!m_ReloadableMethods.ContainsKey(targetMethodId))
            {
                Debug.LogWarning($"HotReload: Target method '{targetMethodId}' not found or not marked [HotReloadWithOverrides]");
                skipReason = $"target '{targetMethodId}' is not registered as [HotReloadWithOverrides]. " +
                    "Ensure the component is in the scene and in play mode, and that it calls " +
                    "HotReloadRegistry.RegisterReloadableType(...) in Awake.";
                return false;
            }

            var originalMethods = m_ReloadableMethods[targetMethodId];
            if (!originalMethods.Any())
            {
                Debug.LogWarning($"HotReload: No original methods registered for '{targetMethodId}'");
                skipReason = $"no original methods registered for '{targetMethodId}'.";
                return false;
            }

            // Validate signature compatibility (basic check - instance parameter + matching return type)
            var originalMethod = originalMethods.First();
            if (!ValidateSignatureCompatibility(originalMethod, overrideMethod))
            {
                Debug.LogError($"HotReload: Signature mismatch for '{targetMethodId}'. Override method must have instance parameter as first argument.");
                skipReason = $"signature mismatch for '{targetMethodId}'. The override must be " +
                    $"'public static {originalMethod.ReturnType.Name} {overrideMethod.Name}" +
                    $"({originalMethod.DeclaringType?.Name} instance, ...)'. " +
                    "A common cause is the override file redeclaring the target type.";
                return false;
            }

            var methodOverride = new MethodOverride
            {
                TargetMethodId = targetMethodId,
                OverrideMethod = overrideMethod,
                SourceType = sourceType,
                RequireMainThread = GetMainThreadRequirement(originalMethod),
                Description = attribute.Description
            };

            m_MethodOverrides[targetMethodId] = methodOverride;
            return true;
        }

        /// <summary>
        /// Attempt to invoke hot reload override if available, otherwise invoke original method.
        /// Returns true if hot reload override was invoked, false if original method should be called.
        /// </summary>
        public static bool TryInvokeHotReload<T>(string methodId, T instance, object[] parameters = null)
        {
            return TryInvokeHotReload(methodId, (object)instance, parameters);
        }

        /// <summary>
        /// Non-generic dispatch entry point. This is the method woven into [HotReload]
        /// methods at compile time (a non-generic signature keeps the injected IL simple).
        /// Returns true if a hot reload override was invoked, false if the original body should run.
        /// </summary>
        public static bool TryInvokeHotReload(string methodId, object instance, object[] parameters = null)
        {
            if (!m_MethodOverrides.TryGetValue(methodId, out var methodOverride))
            {
                return false; // No hot reload override available
            }

            try
            {
                // Prepare parameters with instance as first parameter
                var invokeParams = new object[parameters?.Length + 1 ?? 1];
                invokeParams[0] = instance;
                if (parameters != null && parameters.Length > 0)
                {
                    Array.Copy(parameters, 0, invokeParams, 1, parameters.Length);
                }

                // Invoke on appropriate thread. The dispatcher is injected by the runtime manager
                // (HotReloadRegistry is reached from hot-reloaded runtime code, not a command).
                // If no dispatcher was injected, run on the current thread.
                if (methodOverride.RequireMainThread && Dispatcher != null && !Dispatcher.IsMainThread())
                {
                    Dispatcher.Invoke(() =>
                    {
                        methodOverride.OverrideMethod.Invoke(null, invokeParams);
                    });
                }
                else
                {
                    methodOverride.OverrideMethod.Invoke(null, invokeParams);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotReload: Error invoking override for '{methodId}': {ex.Message}");
                Debug.LogError($"HotReload: Stack trace: {ex.StackTrace}");
                return false; // Fall back to original method
            }
        }

        /// <summary>
        /// Register all methods in a type that have the HotReloadWithOverrides attribute.
        /// Uses reflection to discover and register methods marked with [HotReloadWithOverrides].
        /// </summary>
        public static void RegisterReloadableType(System.Type type)
        {
            if (type == null)
            {
                Debug.LogWarning("HotReload: Cannot register null type");
                return;
            }

            int registeredCount = 0;

            // Get all instance methods (public and non-public)
            var instanceMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in instanceMethods)
            {
                var hotReloadAttr = method.GetCustomAttribute<HotReloadWithOverridesAttribute>();
                if (hotReloadAttr != null)
                {
                    RegisterReloadableMethod(method, hotReloadAttr);
                    registeredCount++;
                }
            }

            // Get all static methods (public and non-public)
            var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var method in staticMethods)
            {
                var hotReloadAttr = method.GetCustomAttribute<HotReloadWithOverridesAttribute>();
                if (hotReloadAttr != null)
                {
                    RegisterReloadableMethod(method, hotReloadAttr);
                    registeredCount++;
                }
            }

            if (registeredCount > 0)
            {
                Debug.Log($"HotReload: Registered {registeredCount} reloadable methods from type {type.Name}");
            }
            else
            {
                Debug.Log($"HotReload: No methods with [HotReloadWithOverrides] attribute found in type {type.Name}");
            }
        }

        /// <summary>
        /// Register a type from loaded hot reload assembly for discovery scanning.
        /// </summary>
        public static void RegisterHotReloadType(Type type, string assemblyId)
        {
            var typeKey = $"{assemblyId}:{type.FullName}";
            m_LoadedHotReloadTypes[typeKey] = type;
        }

        /// <summary>
        /// Clear all hot reload overrides. Used by cleanup_hotreload command.
        /// </summary>
        public static void ClearAllOverrides()
        {
            var overrideCount = m_MethodOverrides.Count;
            var typeCount = m_LoadedHotReloadTypes.Count;

            m_MethodOverrides.Clear();
            m_LoadedHotReloadTypes.Clear();
        }

        /// <summary>
        /// Clear all registry state including reloadable methods.
        /// FOR TESTING ONLY - this should not be called in production code.
        /// </summary>
        public static void ClearAllForTesting()
        {
            var overrideCount = m_MethodOverrides.Count;
            var typeCount = m_LoadedHotReloadTypes.Count;
            var reloadableCount = m_ReloadableMethods.Sum(kvp => kvp.Value.Count);

            m_MethodOverrides.Clear();
            m_LoadedHotReloadTypes.Clear();
            m_ReloadableMethods.Clear();
        }

        /// <summary>
        /// Get statistics about current hot reload state.
        /// </summary>
        public static HotReloadStats GetStats()
        {
            return new HotReloadStats
            {
                ReloadableMethodCount = m_ReloadableMethods.Sum(kvp => kvp.Value.Count),
                ActiveOverrideCount = m_MethodOverrides.Count,
                LoadedTypeCount = m_LoadedHotReloadTypes.Count,
                ReloadableMethodIds = m_ReloadableMethods.Keys.ToList(),
                ActiveOverrideIds = m_MethodOverrides.Keys.ToList()
            };
        }

        /// <summary>
        /// Generate method ID from MethodInfo and optional custom ID.
        /// Format: TypeName.MethodName or custom ID if provided.
        /// </summary>
        private static string GetMethodId(MethodInfo method, string customId = null)
        {
            if (!string.IsNullOrEmpty(customId))
            {
                return customId;
            }

            return $"{method.DeclaringType?.Name}.{method.Name}";
        }

        /// <summary>
        /// Validate that hot reload method signature is compatible with original method.
        /// Hot reload method must have instance parameter as first argument.
        /// </summary>
        private static bool ValidateSignatureCompatibility(MethodInfo originalMethod, MethodInfo overrideMethod)
        {
            var originalParams = originalMethod.GetParameters();
            var overrideParams = overrideMethod.GetParameters();

            // Hot reload method must have at least one parameter (the instance)
            if (overrideParams.Length == 0)
            {
                return false;
            }

            // First parameter must be compatible with declaring type of original method
            var firstParam = overrideParams[0];
            if (!originalMethod.DeclaringType.IsAssignableFrom(firstParam.ParameterType))
            {
                return false;
            }

            // Remaining parameters must match original method parameters
            if (overrideParams.Length - 1 != originalParams.Length)
            {
                return false;
            }

            for (int i = 0; i < originalParams.Length; i++)
            {
                if (overrideParams[i + 1].ParameterType != originalParams[i].ParameterType)
                {
                    return false;
                }
            }

            // Return types must match
            return originalMethod.ReturnType == overrideMethod.ReturnType;
        }

        /// <summary>
        /// Determine if original method requires main thread execution.
        /// </summary>
        private static bool GetMainThreadRequirement(MethodInfo originalMethod)
        {
            // Check for HotReloadWithOverridesAttribute main thread requirement
            var hotReloadAttr = originalMethod.GetCustomAttribute<HotReloadWithOverridesAttribute>();
            if (hotReloadAttr != null)
            {
                return hotReloadAttr.RequireMainThread;
            }

            // Default to main thread requirement for safety
            return true;
        }

        /// <summary>
        /// Information about a registered method override.
        /// </summary>
        private class MethodOverride
        {
            public string TargetMethodId { get; set; }
            public MethodInfo OverrideMethod { get; set; }
            public Type SourceType { get; set; }
            public bool RequireMainThread { get; set; }
            public string Description { get; set; }
        }
    }

    /// <summary>
    /// Statistics about current hot reload registry state.
    /// </summary>
    public class HotReloadStats
    {
        public int ReloadableMethodCount { get; set; }
        public int ActiveOverrideCount { get; set; }
        public int LoadedTypeCount { get; set; }
        public List<string> ReloadableMethodIds { get; set; } = new();
        public List<string> ActiveOverrideIds { get; set; } = new();
    }
}