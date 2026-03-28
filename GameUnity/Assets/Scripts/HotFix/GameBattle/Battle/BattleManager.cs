using Fantasy;

namespace GameBattle
{
    /// <summary>
    /// 战斗管理器，负责管理战斗实例的生命周期
    /// </summary>
    public static class BattleManager
    {
        /// <summary>
        /// 当前战斗场景
        /// </summary>
        public static SubScene CurScene { get; private set; }

        /// <summary>
        /// 当前战斗实体
        /// </summary>
        public static BattleEntity CurBattleEntity { get; private set; }

        /// <summary>
        /// 创建一个新的战斗实例
        /// </summary>
        /// <param name="subScene">战斗子场景</param>
        /// <returns>创建的战斗实体</returns>
        public static BattleEntity CreateBattle(SubScene subScene)
        {
            CurScene = subScene;
            CurBattleEntity = BattleEntity.Create(subScene);
            return CurBattleEntity;
        }

        /// <summary>
        /// 销毁当前战斗实例，释放所有相关资源
        /// </summary>
        public static void DestroyBattle()
        {
            CurScene.Dispose();
            CurScene = null;
            CurBattleEntity = null;
        }
    }
}