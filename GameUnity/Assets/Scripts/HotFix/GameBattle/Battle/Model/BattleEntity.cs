using Fantasy;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 战斗实体，作为战斗场景的核心组件，管理战斗相关的所有逻辑
    /// </summary>
    public sealed class BattleEntity : Entity
    {
        /// <summary>
        /// 帧同步管理组件
        /// </summary>
        public FrameSyncComponent FrameSync { get; private set; }
        
        public LogicUnitFactoryComponent LogicUnitFactoryComponent { get; private set; }
        
        internal IRenderUnitFactory RenderUnitFactory { get; set; }

        /// <summary>
        /// 创建战斗实体并挂载到指定子场景
        /// </summary>
        /// <param name="subScene">战斗子场景</param>
        /// <returns>创建的战斗实体</returns>
        internal static BattleEntity Create(SubScene subScene)
        {
            var battle = subScene.AddComponent<BattleEntity>();
            battle.FrameSync = battle.AddComponent<FrameSyncComponent>();
            battle.LogicUnitFactoryComponent = battle.AddComponent<LogicUnitFactoryComponent>();
            return battle;
        }

        public void Destroy()
        {
            FrameSync = null;
            RenderUnitFactory = null;
            LogicUnitFactoryComponent = null;
        }
    }
}