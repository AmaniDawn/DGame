using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DGame
{
    internal static class TypeUtil
    {
        private static readonly string[] m_runtimeAssemblyNames = new string[]
        {
            "DGame.Runtime",
            "DGame.AOT",
            "Assembly-CSharp",
            "GameLauncher",
            "GameLogic"
        };

        private static readonly string[] m_runtimeOrEditorAssemblyNames =
        {
            "DGame.Runtime",
            "DGame.AOT",
            "Assembly-CSharp",
            "DGame.Editor",
            "Assembly-CSharp-Editor",
            "GameLauncher",
        };

        /// <summary>
        /// 在运行时程序集中获取指定基类的所有子类的名称。
        /// </summary>
        /// <param name="typeBase">基类类型。</param>
        /// <returns>指定基类的所有子类的名称。</returns>
        internal static string[] GetRuntimeTypeNames(System.Type typeBase)
        {
            return GetTypeNames(typeBase, m_runtimeAssemblyNames);
        }

        /// <summary>
        /// 在运行时或编辑器程序集中获取指定基类的所有子类的名称。
        /// </summary>
        /// <param name="typeBase">基类类型。</param>
        /// <returns>指定基类的所有子类的名称。</returns>
        internal static string[] GetRuntimeOrEditorTypeNames(System.Type typeBase)
        {
            return GetTypeNames(typeBase, m_runtimeOrEditorAssemblyNames);
        }

        private static string[] GetTypeNames(System.Type typeBase, string[] assemblyNames)
        {
            List<string> typeNames = new List<string>();
            foreach (string assemblyName in assemblyNames)
            {
                Assembly assembly = null;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch
                {
                    continue;
                }

                if (assembly == null)
                {
                    continue;
                }

                System.Type[] types = assembly.GetTypes();
                foreach (System.Type type in types)
                {
                    if (type.IsClass && !type.IsAbstract && typeBase.IsAssignableFrom(type))
                    {
                        typeNames.Add(type.FullName);
                    }
                }
            }

            typeNames.Sort();
            return typeNames.ToArray();
        }
    }
}