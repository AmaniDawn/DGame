using Fantasy;
using Fantasy.Entitas;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 战斗管理器，作为战斗系统的上层入口，负责创建和管理战斗实例
    /// </summary>
    public static class BattleManager
    {
        /// <summary>
        /// 创建一个新的战斗实例
        /// </summary>
        /// <param name="scene">父场景，默认使用 GameClient.Instance.Scene</param>
        /// <returns>创建的战斗实体</returns>
        public static BattleEntity CreateBattle(Scene scene = null)
        {
            scene ??= GameClient.Instance.Scene;
            var battleScene = Entity.Create<SubScene>(scene);
            var battleEntity = GameBattle.BattleManager.CreateBattle(battleScene);
            battleEntity.SetRenderUnitFactory(battleEntity.AddComponent<RenderUnitFactoryComponent>());
            return battleEntity;
        }

        /// <summary>
        /// 获取当前战斗实体
        /// </summary>
        /// <returns>当前战斗实体，如果没有则返回 null</returns>
        public static BattleEntity GetCurBattleEntity() => GameBattle.BattleManager.CurBattleEntity;

        /// <summary>
        /// 获取当前战斗场景
        /// </summary>
        /// <returns>当前战斗场景，如果没有则返回 null</returns>
        public static SubScene GetCurBattleScene() => GameBattle.BattleManager.CurScene;

        /// <summary>
        /// 销毁当前战斗实例，释放所有相关资源
        /// </summary>
        public static void DestroyBattle() => GameBattle.BattleManager.DestroyBattle();
    }
}