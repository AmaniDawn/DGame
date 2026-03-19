using System;
using System.Collections.Generic;

namespace DGame
{
    /// <summary>
    /// 游戏模块管理系统
    /// </summary>
    public static class ModuleSystem
    {
        /// <summary>
        /// 默认模块数量
        /// </summary>
        private const int DEFAULT_MODULE_COUNT = 16;
        private static readonly Dictionary<Type, Module> m_moduleMaps = new Dictionary<Type, Module>(DEFAULT_MODULE_COUNT);
        private static readonly LinkedList<Module> m_modules = new LinkedList<Module>();
        private static readonly LinkedList<Module> m_updateModules = new LinkedList<Module>();
        private static readonly List<IUpdateModule> m_updateExecuteList = new List<IUpdateModule>(DEFAULT_MODULE_COUNT);
        private static bool m_isExecuteListDirty;

        #region Update 模块轮询

        /// <summary>
        /// 模块轮询
        /// </summary>
        /// <param name="elapsedTime">逻辑时间间隔 秒为单位</param>
        /// <param name="realElapsedTime">真实时间间隔 秒为单位</param>
        public static void Update(float elapsedTime, float realElapsedTime)
        {
            if (m_isExecuteListDirty)
            {
                m_isExecuteListDirty = false;
                BuildUpdateExecuteList();
            }

            for (int i = 0; i < m_updateExecuteList.Count; i++)
            {
                m_updateExecuteList[i].Update(elapsedTime, realElapsedTime);
            }
        }

        private static void BuildUpdateExecuteList()
        {
            m_updateExecuteList.Clear();

            foreach (var updateModule in m_updateModules)
            {
                m_updateExecuteList.Add(updateModule as IUpdateModule);
            }
        }

        #endregion

        #region GetModule

        public static T GetModule<T>() where T : class
        {
            Type type = typeof(T);

            if (!type.IsInterface)
            {
                throw new DGameException(Utility.StringUtil.Format("获取的模块必须是一个接口类型的：", type.FullName));
            }

            if (m_moduleMaps.TryGetValue(type, out var module))
            {
                return module as T;
            }

            // 获取模块名 命名空间.模块 因为传进来的必须是一个接口类型 所以裁剪掉开头的I字母
            string moduleName = $"{type.Namespace}.{type.Name.Substring(1)}, {type.Assembly.GetName().Name}";
            // string moduleName = Utility.StringUtil.Format("{0}.{1}", type.Namespace, type.Name.Substring(1));
            Type moduleType = Type.GetType(moduleName);
            if (moduleType == null)
            {
                throw new DGameException(Utility.StringUtil.Format("找不到该类型的游戏模块：", moduleName));
            }
            return GetModule(moduleType) as T;
        }

        public static Module GetModule(Type type)
        {
            return m_moduleMaps.TryGetValue(type, out Module module) ? module : CreateModule(type);
        }

        #endregion

        #region CreateModule

        /// <summary>
        /// 创建游戏模块
        /// </summary>
        /// <param name="moduleType"></param>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        private static Module CreateModule(Type moduleType)
        {
            Module module = Activator.CreateInstance(moduleType) as Module;

            if (module == null)
            {
                throw new DGameException(Utility.StringUtil.Format("创建游戏模块失败：", moduleType.FullName));
            }

            m_moduleMaps[moduleType] = module;
            RegisterUpdateModule(module);
            return module;
        }

        /// <summary>
        /// 注册自定义模块
        /// </summary>
        /// <param name="module"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        public static T RegisterModule<T>(Module module) where T : class
        {
            Type type = typeof(T);

            if (!type.IsInterface)
            {
                throw new DGameException(Utility.StringUtil.Format("注册的游戏模块必须是接口类型的", type.FullName));
            }

            m_moduleMaps[type] = module;
            RegisterUpdateModule(module);
            return module as T;
        }

        /// <summary>
        /// 注册可轮询模块
        /// </summary>
        /// <param name="module"></param>
        private static void RegisterUpdateModule(Module module)
        {
            LinkedListNode<Module> current = m_modules.First;

            while (current != null)
            {
                if (module.Priority > current.Value.Priority)
                {
                    break;
                }
                current = current.Next;
            }

            if (current != null)
            {
                m_modules.AddBefore(current, module);
            }
            else
            {
                m_modules.AddLast(module);
            }

            Type interfaceType = typeof(IUpdateModule);
            bool implementsIUpdateModule = interfaceType.IsInstanceOfType(module);

            if (implementsIUpdateModule)
            {
                LinkedListNode<Module> currentUpdate = m_updateModules.First;
                while (currentUpdate != null)
                {
                    if (module.Priority > currentUpdate.Value.Priority)
                    {
                        break;
                    }
                    currentUpdate = currentUpdate.Next;
                }

                if (currentUpdate != null)
                {
                    m_updateModules.AddBefore(currentUpdate, module);
                }
                else
                {
                    m_updateModules.AddLast(module);
                }
                m_isExecuteListDirty = true;
            }
            module.OnCreate();
        }

        #endregion

        /// <summary>
        /// 销毁并清理所有的模块
        /// </summary>
        public static void Destroy()
        {
            // 按优先级从低往高执行销毁处理（从后往前）
            LinkedListNode<Module> current = m_modules.Last;
            while (current != null)
            {
                current.Value.OnDestroy();
                current = current.Previous;
            }

            m_modules.Clear();
            m_moduleMaps.Clear();
            m_updateModules.Clear();
            m_updateExecuteList.Clear();
            MemoryPool.ClearAll();
            Utility.Marshal.FreeCachedHGlobal();
        }
    }
}