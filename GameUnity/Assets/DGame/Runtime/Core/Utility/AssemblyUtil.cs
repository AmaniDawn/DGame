using System;
using System.Collections.Generic;
using System.Data;

namespace DGame
{
    public static partial class Utility
    {
        /// <summary>
        /// 程序集相关工具
        /// </summary>
        public static class AssemblyUtil
        {
            private static readonly List<System.Reflection.Assembly> m_assemblies = null;
            private static readonly Dictionary<string, Type> m_cacheTypDict = new Dictionary<string, Type>(StringComparer.Ordinal);

            static AssemblyUtil()
            {
                m_assemblies = new List<System.Reflection.Assembly>();
                // 获取当前域的所有程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    m_assemblies.Add(assemblies[i]);
                }
            }

            #region GetAssembly

            /// <summary>
            /// 获取已加载的程序集。
            /// </summary>
            /// <returns>已加载的程序集。</returns>
            public static System.Reflection.Assembly[] GetAssembly()
            {
                return m_assemblies.ToArray();
            }

            /// <summary>
            /// 获取已加载的程序集。
            /// </summary>
            /// <returns>已加载的程序集。</returns>
            public static List<System.Reflection.Assembly> GetAssemblyList()
            {
                return m_assemblies;
            }

            #endregion

            #region GetTypes

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <returns>已加载的程序集中的所有类型</returns>
            public static Type[] GetTypes()
            {
                List<Type> types = new List<Type>();
                for (int i = 0; i < m_assemblies.Count; i++)
                {
                    types.AddRange(m_assemblies[i].GetTypes());
                }
                return types.ToArray();
            }

            /// <summary>
            /// 获取某个类型
            /// </summary>
            /// <returns>需要获取的类型</returns>
            public static List<Type> GetTypes(Type type)
            {
                List<Type> results = new List<Type>();
                for (int i = 0; i < m_assemblies.Count; i++)
                {
                    var types = m_assemblies[i].GetTypes();

                    for (int j = 0; j < types.Length; j++)
                    {
                        var t = types[j];

                        if (type.IsAssignableFrom(t) && !t.IsAbstract)
                        {
                            results.Add(t);
                        }
                    }
                }
                return results;
            }

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <returns>已加载的程序集中的所有类型</returns>
            public static void GetTypes(List<Type> results)
            {
                if (results == null)
                {
                    throw new DGameException("传入的类型列表是无效的");
                }
                results.Clear();
                for (int i = 0; i < m_assemblies.Count; i++)
                {
                    results.AddRange(m_assemblies[i].GetTypes());
                }
            }

            public static Type GetType(string typeName)
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    throw new DataException($"类型名称{typeName}无效");
                }
                Type type = null;

                if (m_cacheTypDict.TryGetValue(typeName, out type))
                {
                    return type;
                }

                type = Type.GetType(typeName);

                if (type != null)
                {
                    m_cacheTypDict.TryAdd(typeName, type);
                    return type;
                }

                string typeKey;
                for (int i = 0; i < m_assemblies.Count; i++)
                {
                    var assembly = m_assemblies[i];
                    typeKey = StringUtil.Format("{0}, {1}", typeName, assembly.FullName);

                    if (m_cacheTypDict.TryGetValue(typeKey, out type))
                    {
                        return type;
                    }
                    type = Type.GetType(typeKey);

                    if (type != null)
                    {
                        m_cacheTypDict.TryAdd(typeKey, type);
                        return type;
                    }
                }
                return null;
            }

            #endregion

        }
    }
}