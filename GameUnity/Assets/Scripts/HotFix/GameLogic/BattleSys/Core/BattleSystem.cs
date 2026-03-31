using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 战斗系统入口。
    /// </summary>
    public sealed class BattleSystem : Singleton<BattleSystem>
    {
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
            battleContext.SetRenderUnitFactory(battleContext.AddComponent<RenderUnitFactoryComponent>());
            CameraMgr = battleContext.AddComponent<CameraMgrComponent>();
        }
        
        /// <summary>
        /// 清理战斗系统状态。
        /// </summary>
        public void Clear()
        {
            CurBattleContext = null;
            CameraMgr = null;
        }
    }
}