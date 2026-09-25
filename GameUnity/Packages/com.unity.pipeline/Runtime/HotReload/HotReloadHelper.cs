using System;
using System.Reflection;
using UnityEngine;

namespace Unity.Pipeline.HotReload
{
    /// <summary>
    /// Helper utilities for integrating hot reload calls into methods marked with [HotReloadWithOverrides].
    /// Provides convenience methods for checking and invoking hot reload overrides.
    /// </summary>
    public static class HotReloadHelper
    {
        /// <summary>
        /// Execute method with hot reload override check.
        /// If hot reload override exists, invokes it; otherwise invokes original method.
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <param name="instance">Instance to invoke method on</param>
        /// <param name="methodName">Name of the method to invoke</param>
        /// <param name="originalMethod">Original method implementation</param>
        /// <param name="parameters">Method parameters</param>
        /// <returns>Result of method invocation</returns>
        public static object ExecuteWithHotReload<T>(T instance, string methodName, Func<object> originalMethod, params object[] parameters)
        {
            var methodId = GetMethodId<T>(methodName);

            // Try hot reload first
            if (HotReloadRegistry.TryInvokeHotReload(methodId, instance, parameters))
            {
                return null; // Hot reload methods currently don't return values (can be enhanced later)
            }

            // Fall back to original method
            return originalMethod?.Invoke();
        }

        /// <summary>
        /// Execute void method with hot reload override check.
        /// Convenience overload for void methods.
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <param name="instance">Instance to invoke method on</param>
        /// <param name="methodName">Name of the method to invoke</param>
        /// <param name="originalMethod">Original method implementation</param>
        /// <param name="parameters">Method parameters</param>
        public static void ExecuteWithHotReload<T>(T instance, string methodName, Action originalMethod, params object[] parameters)
        {
            var methodId = GetMethodId<T>(methodName);

            // Try hot reload first
            if (HotReloadRegistry.TryInvokeHotReload(methodId, instance, parameters))
            {
                return; // Hot reload override was invoked
            }

            // Fall back to original method
            originalMethod?.Invoke();
        }

        /// <summary>
        /// Execute method with custom method ID and hot reload override check.
        /// Use this when the method has a custom ID specified in [HotReloadWithOverrides(Id = "...")].
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <param name="instance">Instance to invoke method on</param>
        /// <param name="customMethodId">Custom method ID from HotReloadWithOverrides attribute</param>
        /// <param name="originalMethod">Original method implementation</param>
        /// <param name="parameters">Method parameters</param>
        /// <returns>Result of method invocation</returns>
        public static object ExecuteWithHotReloadCustomId<T>(T instance, string customMethodId, Func<object> originalMethod, params object[] parameters)
        {
            // Try hot reload first
            if (HotReloadRegistry.TryInvokeHotReload(customMethodId, instance, parameters))
            {
                return null; // Hot reload methods currently don't return values
            }

            // Fall back to original method
            return originalMethod?.Invoke();
        }

        /// <summary>
        /// Execute void method with custom method ID and hot reload override check.
        /// Use this when the method has a custom ID specified in [HotReloadWithOverrides(Id = "...")].
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <param name="instance">Instance to invoke method on</param>
        /// <param name="customMethodId">Custom method ID from HotReloadWithOverrides attribute</param>
        /// <param name="originalMethod">Original method implementation</param>
        /// <param name="parameters">Method parameters</param>
        public static void ExecuteWithHotReloadCustomId<T>(T instance, string customMethodId, Action originalMethod, params object[] parameters)
        {
            // Try hot reload first
            if (HotReloadRegistry.TryInvokeHotReload(customMethodId, instance, parameters))
            {
                return; // Hot reload override was invoked
            }

            // Fall back to original method
            originalMethod?.Invoke();
        }

        /// <summary>
        /// Check if a hot reload override is active for a specific method.
        /// Useful for debugging or conditional logic based on hot reload state.
        /// </summary>
        /// <typeparam name="T">Type of the instance</typeparam>
        /// <param name="methodName">Name of the method to check</param>
        /// <returns>True if hot reload override is active</returns>
        public static bool IsHotReloadActive<T>(string methodName)
        {
            var methodId = GetMethodId<T>(methodName);
            var stats = HotReloadRegistry.GetStats();
            return stats.ActiveOverrideIds.Contains(methodId);
        }

        /// <summary>
        /// Check if a hot reload override is active for a specific custom method ID.
        /// </summary>
        /// <param name="customMethodId">Custom method ID to check</param>
        /// <returns>True if hot reload override is active</returns>
        public static bool IsHotReloadActive(string customMethodId)
        {
            var stats = HotReloadRegistry.GetStats();
            return stats.ActiveOverrideIds.Contains(customMethodId);
        }

        /// <summary>
        /// Generate standard method ID from type and method name.
        /// Format: TypeName.MethodName
        /// </summary>
        /// <typeparam name="T">Type containing the method</typeparam>
        /// <param name="methodName">Name of the method</param>
        /// <returns>Standard method ID</returns>
        private static string GetMethodId<T>(string methodName)
        {
            return $"{typeof(T).Name}.{methodName}";
        }
    }
}