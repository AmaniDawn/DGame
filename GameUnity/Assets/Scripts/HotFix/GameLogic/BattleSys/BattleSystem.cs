using GameBattle;

namespace GameLogic
{
    public sealed class BattleSystem : Singleton<BattleSystem>
    {
        public BattleContextComponent CurBattleContext { get; private set; }

        public CameraMgrComponent CameraMgr { get; private set; }

        public void Init(BattleContextComponent battleContext)
        {
            CurBattleContext = battleContext;
            battleContext.SetRenderUnitFactory(battleContext.AddComponent<RenderUnitFactoryComponent>());
            CameraMgr = battleContext.AddComponent<CameraMgrComponent>();
        }
        
        public void Clear()
        {
            CurBattleContext = null;
            CameraMgr = null;
        }
    }
}