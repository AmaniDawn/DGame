using System;
using System.Collections.Generic;
using Fantasy.Entitas;
using UnityEngine;
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
        /// <summary>
        /// 内置根节点缓存表。
        /// </summary>
        public Dictionary<BattleViewRootType, Transform> BuiltInRoots { get; } = new();

        /// <summary>
        /// 自定义根节点缓存表。
        /// </summary>
        public Dictionary<string, Transform> CustomRoots { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// 战斗表现总根节点对象。
        /// </summary>
        public GameObject RootObject { get; set; }

        /// <summary>
        /// 战斗表现总根节点变换。
        /// </summary>
        public Transform RootTransform { get; set; }

        /// <summary>
        /// 当前组件是否已完成初始化。
        /// </summary>
        public bool IsInitialized => RootObject != null;
    }
}