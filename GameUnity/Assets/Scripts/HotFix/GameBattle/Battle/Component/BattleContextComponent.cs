using Fantasy;
using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 战斗上下文组件。
    /// <remarks>作为战斗场景的核心组件，管理战斗相关的所有逻辑。</remarks>
    /// </summary>
    public sealed class BattleContextComponent : Entity
    {
        /// <summary>
        /// 获取帧同步管理组件。
        /// </summary>
        public FrameSyncComponent FrameSync { get; private set; }

        /// <summary>
        /// 获取逻辑单位工厂组件。
        /// </summary>
        public LogicUnitFactoryComponent LogicUnitFactoryComponent { get; private set; }

        /// <summary>
        /// 获取逻辑单位注册表组件。
        /// </summary>
        public LogicUnitRegistryComponent LogicUnitRegistry { get; private set; }

        /// <summary>
        /// 获取逻辑单位生命周期组件。
        /// </summary>
        public LogicUnitLifecycleComponent LogicUnitLifecycle { get; private set; }

        /// <summary>
        /// 获取战斗级事件中心组件。
        /// </summary>
        public BattleEventHubComponent BattleEvents { get; private set; }

        /// <summary>
        /// 获取战斗单例管理组件。
        /// </summary>
        public SingletonManagerComponent SingletonManager { get; private set; }

        /// <summary>
        /// 获取或设置渲染单位工厂。
        /// </summary>
        internal IRenderUnitFactory RenderUnitFactory { get; set; }

        /// <summary>
        /// 创建战斗上下文组件并挂载到指定子场景。
        /// </summary>
        /// <param name="subScene">战斗子场景。</param>
        /// <returns>创建的战斗上下文组件。</returns>
        internal static BattleContextComponent Create(SubScene subScene)
        {
            var battle = subScene.AddComponent<BattleContextComponent>();
            battle.SingletonManager = battle.AddComponent<SingletonManagerComponent>();
            battle.FrameSync = battle.AddComponent<FrameSyncComponent>();
            battle.LogicUnitFactoryComponent = battle.AddComponent<LogicUnitFactoryComponent>();
            battle.LogicUnitRegistry = battle.AddComponent<LogicUnitRegistryComponent>();
            battle.LogicUnitLifecycle = battle.AddComponent<LogicUnitLifecycleComponent>();
            battle.BattleEvents = battle.AddComponent<BattleEventHubComponent>();
            return battle;
        }

        /// <summary>
        /// 销毁战斗上下文组件。
        /// <remarks>释放所有相关资源</remarks>
        /// </summary>
        public void Destroy()
        {
            LogicUnitLifecycle?.DestroyAll(LogicUnitDestroyReason.BattleCleanup);
            LogicUnitRegistry?.Clear();
            FrameSync = null;
            RenderUnitFactory = null;
            LogicUnitFactoryComponent = null;
            LogicUnitRegistry = null;
            LogicUnitLifecycle = null;
            BattleEvents = null;
            SingletonManager = null;
        }
    }
}
