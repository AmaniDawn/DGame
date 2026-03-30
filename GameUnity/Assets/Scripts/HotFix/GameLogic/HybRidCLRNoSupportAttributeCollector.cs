using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DGame;

namespace GameLogic
{
    public static class RuntimeInitializeOnLoadMethodCollector
    {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
        private static readonly string s_runtimeInitializeOnLoadMethodCollectorClassName = "AOT.RuntimeInitializeOnLoadMethodCollector_Gen";
        private static readonly string s_executeMethodsName = "ExecuteMethods";
        private static readonly string s_executeAllMethodsName = "ExecuteAllMethods";
        private static MethodInfo s_methodInfo;
        private static Type s_collectorType;
#endif

        public static void ExecuteMethods(RuntimeInitializeLoadType loadType)
        {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
            if (!Settings.UpdateSettings.Enable)
            {
                return;
            }

            if (s_methodInfo != null)
            {
                s_methodInfo?.Invoke(null, new object[] { loadType });
                return;
            }

            if (s_collectorType == null)
            {
                s_collectorType = Type.GetType(s_runtimeInitializeOnLoadMethodCollectorClassName);
            }

            if (s_collectorType != null)
            {
                s_methodInfo = s_collectorType.GetMethod(s_executeMethodsName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                s_methodInfo?.Invoke(null, new object[] { loadType });
            }
#endif
        }

        public static void ExecuteMethods()
        {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
            if (!Settings.UpdateSettings.Enable)
            {
                return;
            }
            var collectorType = Type.GetType(s_runtimeInitializeOnLoadMethodCollectorClassName);
            if (collectorType != null)
            {
                var methodInfo = collectorType.GetMethod(s_executeAllMethodsName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                methodInfo?.Invoke(null, null);
            }
#endif
        }
    }

    public static class RequireComponentCollector
    {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
        private static readonly string s_requireComponentCollectorClassName = "AOT.RequireComponentCollector_Gen";
        private static readonly string s_executeMethodsName = "StartAddComponents";
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void ExecuteMethods()
        {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
            if (!Settings.UpdateSettings.Enable)
            {
                return;
            }
            var collectorType = Type.GetType(s_requireComponentCollectorClassName);

            if (collectorType != null)
            {
                var methodInfo = collectorType.GetMethod(s_executeMethodsName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                methodInfo?.Invoke(null, null);
            }
#endif
        }
    }
}