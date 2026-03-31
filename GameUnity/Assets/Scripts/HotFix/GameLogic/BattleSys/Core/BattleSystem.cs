using System.Collections.Generic;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 战斗系统入口。
    /// </summary>
    public sealed class BattleSystem : Singleton<BattleSystem>, IUpdate
    {
        private readonly List<RenderUnit> m_renderUnits = new();
        private readonly List<RenderUnit> m_renderUnitsBuffer = new();

        /// <summary>
        /// 获取当前战斗上下文。
        /// </summary>
        public BattleContextComponent CurBattleContext { get; private set; }

        /// <summary>
        /// 获取摄像机管理组件。
        /// </summary>
        public CameraMgrComponent CameraMgr { get; private set; }

        /// <summary>
        /// 初始化战斗系统。
        /// </summary>
        /// <param name="battleContext">战斗上下文。</param>
        public void Init(BattleContextComponent battleContext)
        {
            CurBattleContext = battleContext;
            m_renderUnits.Clear();
            m_renderUnitsBuffer.Clear();
            battleContext.SetRenderUnitFactory(battleContext.AddComponent<RenderUnitFactoryComponent>());
            CameraMgr = battleContext.AddComponent<CameraMgrComponent>();
        }

        /// <summary>
        /// 注册一个活跃渲染单位，使其在每帧执行逻辑到表现的同步。
        /// </summary>
        /// <param name="renderUnit">渲染单位。</param>
        public void RegisterRenderUnit(RenderUnit renderUnit)
        {
            if (renderUnit == null || m_renderUnits.Contains(renderUnit))
            {
                return;
            }

            m_renderUnits.Add(renderUnit);
        }

        /// <summary>
        /// 反注册渲染单位。
        /// </summary>
        /// <param name="renderUnit">渲染单位。</param>
        public void UnregisterRenderUnit(RenderUnit renderUnit)
        {
            if (renderUnit == null)
            {
                return;
            }

            m_renderUnits.Remove(renderUnit);
        }

        /// <summary>
        /// 每帧驱动所有活跃渲染单位执行同步与插值。
        /// </summary>
        public void OnUpdate()
        {
            if (CurBattleContext == null || m_renderUnits.Count == 0)
            {
                return;
            }

            m_renderUnitsBuffer.Clear();
            m_renderUnitsBuffer.AddRange(m_renderUnits);
            foreach (var renderUnit in m_renderUnitsBuffer)
            {
                if (renderUnit == null || renderUnit.IsDisposed || renderUnit.IsDestroyed)
                {
                    m_renderUnits.Remove(renderUnit);
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
            m_renderUnits.Clear();
            m_renderUnitsBuffer.Clear();
            CurBattleContext = null;
            CameraMgr = null;
        }
    }
}
