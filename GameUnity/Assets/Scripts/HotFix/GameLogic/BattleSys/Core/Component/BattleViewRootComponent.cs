using System;
using System.Collections.Generic;
using Fantasy.Entitas;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 战斗视图根节点类型。
    /// </summary>
    public enum BattleViewRootType
    {
        None = 0,      // 无效根节点类型
        Battle = 1,    // 战斗级根节点，用于地图和场景级运行时对象
        Unit = 2,      // 单位根节点，用于挂载全部渲染单位
        Effect = 3,    // 特效根节点，用于挂载战斗表现特效
        Drop = 4,      // 掉落物根节点，用于挂载掉落物和拾取物
        Debug = 5,     // 调试根节点，用于挂载调试标签和诊断对象
        Max = 6        // 根节点类型最大值
    }

    /// <summary>
    /// 战斗视图根节点组件。
    /// <remarks>
    /// 负责为当前战斗实例创建统一的运行时表现层级，避免单位、特效、掉落物等对象直接散落到场景根节点。
    /// 该组件归属于 <see cref="BattleContextComponent"/>，生命周期与单场战斗严格一致。
    /// </remarks>
    /// </summary>
    public sealed class BattleViewRootComponent : Entity
    {
        private const string DEFAULT_ROOT_NAME_PREFIX = "BattleViewRoot"; // 战斗视图总根节点默认名称前缀
        private static readonly List<string> s_invalidCustomRootKeys = new(); // 自定义根节点清理时复用的无效键缓存列表
        private readonly Dictionary<BattleViewRootType, Transform> m_builtInRoots = new(); // 内置根节点缓存表
        private readonly Dictionary<string, Transform> m_customRoots = new(StringComparer.Ordinal); // 自定义根节点缓存表

        /// <summary>
        /// 战斗表现总根节点对象。
        /// </summary>
        public GameObject RootObject { get; private set; }

        /// <summary>
        /// 战斗表现总根节点变换。
        /// </summary>
        public Transform RootTransform { get; private set; }

        /// <summary>
        /// 当前组件是否已完成初始化。
        /// </summary>
        public bool IsInitialized => RootObject != null;

        /// <summary>
        /// 战斗级根节点。
        /// <remarks>适合挂地图、地面标记、场景级运行时对象等。</remarks>
        /// </summary>
        public Transform BattleRoot => GetRoot(BattleViewRootType.Battle);

        /// <summary>
        /// 单位根节点。
        /// <remarks>适合挂载全部 <see cref="RenderUnit"/> 的根对象。</remarks>
        /// </summary>
        public Transform UnitRoot => GetRoot(BattleViewRootType.Unit);

        /// <summary>
        /// 特效根节点。
        /// <remarks>适合挂载战斗中的普通特效对象。</remarks>
        /// </summary>
        public Transform EffectRoot => GetRoot(BattleViewRootType.Effect);

        /// <summary>
        /// 掉落物根节点。
        /// <remarks>适合挂载战斗掉落物、拾取物等表现对象。</remarks>
        /// </summary>
        public Transform DropRoot => GetRoot(BattleViewRootType.Drop);

        /// <summary>
        /// 调试根节点。
        /// <remarks>适合挂载调试标签、Gizmo 辅助对象和开发期诊断表现。</remarks>
        /// </summary>
        public Transform DebugRoot => GetRoot(BattleViewRootType.Debug);

        /// <summary>
        /// 初始化当前战斗的视图根节点。
        /// </summary>
        /// <param name="rootName">总根节点名称；不传时自动根据当前战斗上下文生成。</param>
        public void Init(string rootName = null)
        {
            if (IsInitialized)
            {
                return;
            }

            rootName = string.IsNullOrWhiteSpace(rootName) ? BuildDefaultRootName() : rootName;
            RootObject = new GameObject(rootName);
            RootTransform = RootObject.transform;
            RootTransform.ResetLocalPosScaleRot();

            for (int i = (int)(BattleViewRootType.None + 1); i < (int)BattleViewRootType.Max; i++)
            {
                EnsureBuiltInRoot((BattleViewRootType)i);
            }
        }

        /// <summary>
        /// 获取指定内置根节点；若节点被销毁会自动重建。
        /// </summary>
        /// <param name="rootType">根节点类型。</param>
        /// <returns>根节点变换；非法类型时返回 null。</returns>
        public Transform GetRoot(BattleViewRootType rootType)
        {
            if (rootType == BattleViewRootType.None)
            {
                return null;
            }

            if (!IsInitialized)
            {
                Init();
            }

            return EnsureBuiltInRoot(rootType);
        }

        /// <summary>
        /// 尝试获取指定内置根节点。
        /// </summary>
        /// <param name="rootType">根节点类型。</param>
        /// <param name="root">输出根节点。</param>
        /// <returns>获取成功返回 true。</returns>
        public bool TryGetRoot(BattleViewRootType rootType, out Transform root)
        {
            root = GetRoot(rootType);
            return root != null;
        }

        /// <summary>
        /// 获取或创建一个自定义根节点，并默认挂到战斗级根节点下。
        /// </summary>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <returns>自定义根节点变换；名称非法时返回 null。</returns>
        public Transform GetOrCreateCustomRoot(string rootName)
            => GetOrCreateCustomRoot(rootName, BattleRoot);

        /// <summary>
        /// 获取或创建一个自定义根节点。
        /// </summary>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="parent">父节点；为空时默认挂到总根节点下。</param>
        /// <param name="resetLocalTransform">创建或重挂后是否重置本地变换。</param>
        /// <returns>自定义根节点变换；名称非法时返回 null。</returns>
        public Transform GetOrCreateCustomRoot(string rootName, Transform parent, bool resetLocalTransform = true)
        {
            if (string.IsNullOrWhiteSpace(rootName))
            {
                return null;
            }

            if (!IsInitialized)
            {
                Init();
            }

            CleanupInvalidCustomRoots();
            parent ??= RootTransform;

            if (m_customRoots.TryGetValue(rootName, out var root) && root != null)
            {
                if (root.parent != parent)
                {
                    root.SetParent(parent, false);
                    if (resetLocalTransform)
                    {
                        root.ResetLocalPosScaleRot();
                    }
                }
                return root;
            }

            var go = new GameObject(rootName);
            root = go.transform;
            root.SetParent(parent, false);
            if (resetLocalTransform)
            {
                root.ResetLocalPosScaleRot();
            }
            m_customRoots[rootName] = root;
            return root;
        }

        /// <summary>
        /// 尝试获取一个已存在的自定义根节点。
        /// </summary>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="root">输出根节点。</param>
        /// <returns>存在时返回 true。</returns>
        public bool TryGetCustomRoot(string rootName, out Transform root)
        {
            root = null;
            if (string.IsNullOrWhiteSpace(rootName))
            {
                return false;
            }

            CleanupInvalidCustomRoots();
            return m_customRoots.TryGetValue(rootName, out root) && root != null;
        }

        /// <summary>
        /// 移除一个自定义根节点。
        /// </summary>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="destroyGameObject">是否同时销毁对应游戏对象。</param>
        /// <returns>移除成功返回 true。</returns>
        public bool RemoveCustomRoot(string rootName, bool destroyGameObject = true)
        {
            if (string.IsNullOrWhiteSpace(rootName))
            {
                return false;
            }

            CleanupInvalidCustomRoots();
            if (!m_customRoots.Remove(rootName, out var root))
            {
                return false;
            }

            if (destroyGameObject && root != null)
            {
                Object.Destroy(root.gameObject);
            }
            return true;
        }

        /// <summary>
        /// 将目标变换重挂到指定内置根节点下。
        /// </summary>
        /// <param name="target">目标变换。</param>
        /// <param name="rootType">内置根节点类型。</param>
        /// <param name="worldPositionStays">是否保持世界坐标。</param>
        public void ReparentToRoot(Transform target, BattleViewRootType rootType, bool worldPositionStays = false)
        {
            if (target == null)
            {
                return;
            }

            var parent = GetRoot(rootType);
            if (parent == null)
            {
                return;
            }

            target.SetParent(parent, worldPositionStays);
        }

        /// <summary>
        /// 将目标变换重挂到指定自定义根节点下；若不存在则自动创建。
        /// </summary>
        /// <param name="target">目标变换。</param>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="worldPositionStays">是否保持世界坐标。</param>
        public void ReparentToCustomRoot(Transform target, string rootName, bool worldPositionStays = false)
        {
            if (target == null)
            {
                return;
            }

            var parent = GetOrCreateCustomRoot(rootName);
            if (parent == null)
            {
                return;
            }

            target.SetParent(parent, worldPositionStays);
        }

        /// <summary>
        /// 清理当前战斗的全部视图根节点。
        /// </summary>
        public void Clear()
        {
            m_builtInRoots.Clear();
            m_customRoots.Clear();

            if (RootObject != null)
            {
                Object.Destroy(RootObject);
            }

            RootObject = null;
            RootTransform = null;
        }

        private string BuildDefaultRootName()
        {
            var battleId = Parent?.Id ?? Id;
            return $"{DEFAULT_ROOT_NAME_PREFIX}_{battleId}";
        }

        private Transform EnsureBuiltInRoot(BattleViewRootType rootType)
        {
            if (RootTransform == null)
            {
                return null;
            }

            if (m_builtInRoots.TryGetValue(rootType, out var root) && root != null)
            {
                return root;
            }

            var go = new GameObject(GetBuiltInRootName(rootType));
            root = go.transform;
            root.SetParent(RootTransform, false);
            root.ResetLocalPosScaleRot();
            m_builtInRoots[rootType] = root;
            return root;
        }

        private static string GetBuiltInRootName(BattleViewRootType rootType)
            => rootType switch
            {
                BattleViewRootType.Battle => "BattleRoot",
                BattleViewRootType.Unit => "UnitRoot",
                BattleViewRootType.Effect => "EffectRoot",
                BattleViewRootType.Drop => "DropRoot",
                BattleViewRootType.Debug => "DebugRoot",
                _ => "UnknownRoot",
            };

        private void CleanupInvalidCustomRoots()
        {
            if (m_customRoots.Count == 0)
            {
                return;
            }

            s_invalidCustomRootKeys.Clear();
            foreach (var pair in m_customRoots)
            {
                if (pair.Value == null)
                {
                    s_invalidCustomRootKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < s_invalidCustomRootKeys.Count; i++)
            {
                m_customRoots.Remove(s_invalidCustomRootKeys[i]);
            }
            s_invalidCustomRootKeys.Clear();
        }
    }
}