using System;
using System.Collections.Generic;
using Fantasy.Entitas.Interface;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 战斗视图根节点组件销毁系统。
    /// </summary>
    public sealed class BattleViewRootComponentDestroySystem : DestroySystem<BattleViewRootComponent>
    {
        /// <summary>
        /// 销毁战斗视图根节点组件。
        /// </summary>
        /// <param name="self">战斗视图根节点组件实例。</param>
        protected override void Destroy(BattleViewRootComponent self)
        {
            self?.Clear();
        }
    }

    /// <summary>
    /// 战斗视图根节点组件扩展方法。
    /// </summary>
    public static class BattleViewRootComponentSystem
    {
        private const string DEFAULT_ROOT_NAME_PREFIX = "BattleViewRoot";
        private static readonly List<string> s_invalidCustomRootKeys = new();

        /// <summary>
        /// 获取战斗级根节点。
        /// <remarks>适合挂地图、地面标记、场景级运行时对象等。</remarks>
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <returns>战斗级根节点。</returns>
        public static Transform BattleRoot(this BattleViewRootComponent self) => self.GetRoot(BattleViewRootType.Battle);

        /// <summary>
        /// 获取单位根节点。
        /// <remarks>适合挂载全部 <see cref="RenderUnit"/> 的根对象。</remarks>
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <returns>单位根节点。</returns>
        public static Transform UnitRoot(this BattleViewRootComponent self) => self.GetRoot(BattleViewRootType.Unit);

        /// <summary>
        /// 获取特效根节点。
        /// <remarks>适合挂载战斗中的普通特效对象。</remarks>
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <returns>特效根节点。</returns>
        public static Transform EffectRoot(this BattleViewRootComponent self) => self.GetRoot(BattleViewRootType.Effect);

        /// <summary>
        /// 获取掉落物根节点。
        /// <remarks>适合挂载战斗掉落物、拾取物等表现对象。</remarks>
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <returns>掉落物根节点。</returns>
        public static Transform DropRoot(this BattleViewRootComponent self) => self.GetRoot(BattleViewRootType.Drop);

        /// <summary>
        /// 获取调试根节点。
        /// <remarks>适合挂载调试标签、Gizmo 辅助对象和开发期诊断表现。</remarks>
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <returns>调试根节点。</returns>
        public static Transform DebugRoot(this BattleViewRootComponent self) => self.GetRoot(BattleViewRootType.Debug);

        /// <summary>
        /// 初始化当前战斗的视图根节点。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="rootName">总根节点名称；不传时自动根据当前战斗上下文生成。</param>
        public static void Init(this BattleViewRootComponent self, string rootName = null)
        {
            if (self == null || self.IsInitialized)
            {
                return;
            }

            rootName = string.IsNullOrWhiteSpace(rootName) ? self.BuildDefaultRootName() : rootName;
            self.RootObject = new GameObject(rootName);
            self.RootTransform = self.RootObject.transform;
            self.RootTransform.ResetLocalPosScaleRot();

            for (int i = (int)(BattleViewRootType.None + 1); i < (int)BattleViewRootType.Max; i++)
            {
                self.EnsureBuiltInRoot((BattleViewRootType)i);
            }
        }

        /// <summary>
        /// 获取指定内置根节点；若节点被销毁会自动重建。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="rootType">根节点类型。</param>
        /// <returns>根节点变换；非法类型时返回 null。</returns>
        public static Transform GetRoot(this BattleViewRootComponent self, BattleViewRootType rootType)
        {
            if (self == null || rootType == BattleViewRootType.None)
            {
                return null;
            }

            if (!self.IsInitialized)
            {
                self.Init();
            }

            return self.EnsureBuiltInRoot(rootType);
        }

        /// <summary>
        /// 尝试获取指定内置根节点。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="rootType">根节点类型。</param>
        /// <param name="root">输出根节点。</param>
        /// <returns>获取成功返回 true。</returns>
        public static bool TryGetRoot(this BattleViewRootComponent self, BattleViewRootType rootType, out Transform root)
        {
            root = self.GetRoot(rootType);
            return root != null;
        }

        /// <summary>
        /// 获取或创建一个自定义根节点，并默认挂到战斗级根节点下。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <returns>自定义根节点变换；名称非法时返回 null。</returns>
        public static Transform GetOrCreateCustomRoot(this BattleViewRootComponent self, string rootName)
            => self.GetOrCreateCustomRoot(rootName, self.BattleRoot());

        /// <summary>
        /// 获取或创建一个自定义根节点。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="parent">父节点；为空时默认挂到总根节点下。</param>
        /// <param name="resetLocalTransform">创建或重挂后是否重置本地变换。</param>
        /// <returns>自定义根节点变换；名称非法时返回 null。</returns>
        public static Transform GetOrCreateCustomRoot(this BattleViewRootComponent self, string rootName, Transform parent,
            bool resetLocalTransform = true)
        {
            if (self == null || string.IsNullOrWhiteSpace(rootName))
            {
                return null;
            }

            if (!self.IsInitialized)
            {
                self.Init();
            }

            self.CleanupInvalidCustomRoots();
            parent ??= self.RootTransform;

            if (self.CustomRoots.TryGetValue(rootName, out var root) && root != null)
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
            self.CustomRoots[rootName] = root;
            return root;
        }

        /// <summary>
        /// 尝试获取一个已存在的自定义根节点。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="root">输出根节点。</param>
        /// <returns>存在时返回 true。</returns>
        public static bool TryGetCustomRoot(this BattleViewRootComponent self, string rootName, out Transform root)
        {
            root = null;
            if (self == null || string.IsNullOrWhiteSpace(rootName))
            {
                return false;
            }

            self.CleanupInvalidCustomRoots();
            return self.CustomRoots.TryGetValue(rootName, out root) && root != null;
        }

        /// <summary>
        /// 移除一个自定义根节点。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="destroyGameObject">是否同时销毁对应游戏对象。</param>
        /// <returns>移除成功返回 true。</returns>
        public static bool RemoveCustomRoot(this BattleViewRootComponent self, string rootName, bool destroyGameObject = true)
        {
            if (self == null || string.IsNullOrWhiteSpace(rootName))
            {
                return false;
            }

            self.CleanupInvalidCustomRoots();
            if (!self.CustomRoots.Remove(rootName, out var root))
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
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="target">目标变换。</param>
        /// <param name="rootType">内置根节点类型。</param>
        /// <param name="worldPositionStays">是否保持世界坐标。</param>
        public static void ReparentToRoot(this BattleViewRootComponent self, Transform target, BattleViewRootType rootType,
            bool worldPositionStays = false)
        {
            if (self == null || target == null)
            {
                return;
            }

            var parent = self.GetRoot(rootType);
            if (parent == null)
            {
                return;
            }

            target.SetParent(parent, worldPositionStays);
        }

        /// <summary>
        /// 将目标变换重挂到指定自定义根节点下；若不存在则自动创建。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        /// <param name="target">目标变换。</param>
        /// <param name="rootName">自定义根节点名称。</param>
        /// <param name="worldPositionStays">是否保持世界坐标。</param>
        public static void ReparentToCustomRoot(this BattleViewRootComponent self, Transform target, string rootName,
            bool worldPositionStays = false)
        {
            if (self == null || target == null)
            {
                return;
            }

            var parent = self.GetOrCreateCustomRoot(rootName);
            if (parent == null)
            {
                return;
            }

            target.SetParent(parent, worldPositionStays);
        }

        /// <summary>
        /// 清理当前战斗的全部视图根节点。
        /// </summary>
        /// <param name="self">战斗视图根节点组件。</param>
        public static void Clear(this BattleViewRootComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.BuiltInRoots.Clear();
            self.CustomRoots.Clear();

            if (self.RootObject != null)
            {
                Object.Destroy(self.RootObject);
            }

            self.RootObject = null;
            self.RootTransform = null;
        }

        private static string BuildDefaultRootName(this BattleViewRootComponent self)
        {
            var battleId = self.Parent?.Id ?? self.Id;
            return $"{DEFAULT_ROOT_NAME_PREFIX}_{battleId}";
        }

        private static Transform EnsureBuiltInRoot(this BattleViewRootComponent self, BattleViewRootType rootType)
        {
            if (self.RootTransform == null)
            {
                return null;
            }

            if (self.BuiltInRoots.TryGetValue(rootType, out var root) && root != null)
            {
                return root;
            }

            var go = new GameObject(GetBuiltInRootName(rootType));
            root = go.transform;
            root.SetParent(self.RootTransform, false);
            root.ResetLocalPosScaleRot();
            self.BuiltInRoots[rootType] = root;
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

        private static void CleanupInvalidCustomRoots(this BattleViewRootComponent self)
        {
            if (self.CustomRoots.Count == 0)
            {
                return;
            }

            s_invalidCustomRootKeys.Clear();
            foreach (var pair in self.CustomRoots)
            {
                if (pair.Value == null)
                {
                    s_invalidCustomRootKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < s_invalidCustomRootKeys.Count; i++)
            {
                self.CustomRoots.Remove(s_invalidCustomRootKeys[i]);
            }
            s_invalidCustomRootKeys.Clear();
        }
    }
}