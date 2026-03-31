using System;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 战斗系统入口。
    /// </summary>
    public sealed class BattleSystem : Singleton<BattleSystem>, IUpdate
    {
        #region Component

        /// <summary>
        /// 当前战斗内的渲染单位注册表。
        /// </summary>
        public RenderUnitRegistry RenderUnits { get; } = new();

        /// <summary>
        /// 获取当前战斗上下文。
        /// </summary>
        public BattleContextComponent CurBattleContext { get; private set; }

        /// <summary>
        /// 获取摄像机管理组件。
        /// </summary>
        public CameraMgrComponent CameraMgr { get; private set; }

        /// <summary>
        /// 获取当前战斗的视图根节点组件。
        /// </summary>
        public BattleViewRootComponent ViewRoots { get; private set; }

        #endregion

        /// <summary>
        /// 初始化战斗系统。
        /// </summary>
        /// <param name="battleContext">战斗上下文。</param>
        public void Init(BattleContextComponent battleContext)
        {
            CurBattleContext = battleContext;
            RenderUnits.Clear();
            battleContext.SetRenderUnitFactory(battleContext.AddComponent<RenderUnitFactoryComponent>());
            CameraMgr = battleContext.AddComponent<CameraMgrComponent>();
            ViewRoots = battleContext.AddComponent<BattleViewRootComponent>();
            ViewRoots.Init();
        }

        #region RenderUnit注册与反注册相关

        /// <summary>
        /// 注册一个活跃渲染单位，使其在每帧执行逻辑到表现的同步。
        /// </summary>
        /// <param name="renderUnit">渲染单位。</param>
        public void RegisterRenderUnit(RenderUnit renderUnit)
            => RenderUnits.Register(renderUnit);

        /// <summary>
        /// 反注册渲染单位。
        /// </summary>
        /// <param name="renderUnit">渲染单位。</param>
        public void UnregisterRenderUnit(RenderUnit renderUnit)
            => RenderUnits.Unregister(renderUnit);

        /// <summary>
        /// 按 Entity.Id 查询渲染单位。
        /// </summary>
        /// <param name="entityId">渲染单位实体 ID。</param>
        /// <param name="renderUnit">查询结果。</param>
        /// <returns>找到时返回 <see langword="true"/>。</returns>
        public bool TryGetRenderUnit(long entityId, out RenderUnit renderUnit)
            => RenderUnits.TryGet(entityId, out renderUnit);

        /// <summary>
        /// 遍历当前战斗中的全部渲染单位。
        /// </summary>
        /// <param name="visitor">遍历回调。</param>
        public void ForEachRenderUnit(Action<RenderUnit> visitor)
            => RenderUnits.ForEach(visitor);

        #endregion

        /// <summary>
        /// 每帧驱动所有活跃渲染单位执行同步与插值。
        /// </summary>
        public void OnUpdate()
        {
            if (CurBattleContext == null || RenderUnits.Count == 0)
            {
                return;
            }

            var snapshot = RenderUnits.BuildSnapshot();
            foreach (var renderUnit in snapshot)
            {
                if (renderUnit == null || renderUnit.IsDisposed || renderUnit.IsDestroyed)
                {
                    RenderUnits.Unregister(renderUnit);
                    continue;
                }

                renderUnit.SyncFromLogic();
            }
        }

        /// <summary>
        /// 清理战斗系统状态。
        /// </summary>
        public void Clear()
        {
            RenderUnits.Clear();
            CurBattleContext = null;
            CameraMgr = null;
            ViewRoots = null;
        }
    }
}