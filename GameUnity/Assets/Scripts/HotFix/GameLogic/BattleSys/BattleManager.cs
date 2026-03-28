using Fantasy;
using Fantasy.Entitas;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 战斗管理器，作为战斗系统的上层入口，负责创建和管理战斗实例。
    /// 此为渲染层的战斗管理器，负责初始化渲染相关的工厂组件。
    /// </summary>
    public static class BattleManager
    {
        /// <summary>
        /// 创建一个新的战斗实例。
        /// </summary>
        /// <param name="scene">父场景，默认使用 GameClient.Instance.Scene。</param>
        /// <returns>创建的战斗上下文组件。</returns>
        public static BattleContextComponent CreateBattle(Scene scene = null)
        {
            scene ??= GameClient.Instance.Scene;
            var battleScene = Entity.Create<SubScene>(scene);
            var battleContext = GameBattle.BattleManager.CreateBattle(battleScene);
            battleContext.SetRenderUnitFactory(battleContext.AddComponent<RenderUnitFactoryComponent>());
            return battleContext;
        }

        /// <summary>
        /// 获取当前战斗上下文组件。
        /// </summary>
        /// <returns>当前战斗上下文组件，如果没有则返回 null。</returns>
        public static BattleContextComponent GetCurBattleContext() => GameBattle.BattleManager.CurBattleContextComponent;

        /// <summary>
        /// 获取当前战斗场景。
        /// </summary>
        /// <returns>当前战斗场景，如果没有则返回 null。</returns>
        public static SubScene GetCurBattleScene() => GameBattle.BattleManager.CurScene;

        /// <summary>
        /// 销毁当前战斗实例，释放所有相关资源。
        /// </summary>
        public static void DestroyBattle() => GameBattle.BattleManager.DestroyBattle();
    }
}