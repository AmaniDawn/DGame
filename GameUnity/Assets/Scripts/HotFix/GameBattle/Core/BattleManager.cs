using Fantasy;

namespace GameBattle
{
    /// <summary>
    /// 战斗管理器。
    /// <remarks>负责管理战斗实例的生命周期，提供战斗场景的创建、销毁和当前战斗状态的访问。</remarks>
    /// </summary>
    public static class BattleManager
    {
        /// <summary>
        /// 获取当前战斗场景。
        /// </summary>
        public static SubScene CurScene { get; private set; }

        /// <summary>
        /// 获取当前战斗上下文组件。
        /// </summary>
        public static BattleContextComponent CurBattleContextComponent { get; private set; }

        /// <summary>
        /// 创建一个新的战斗实例。
        /// </summary>
        /// <param name="subScene">战斗子场景。</param>
        /// <returns>创建的战斗上下文组件。</returns>
        public static BattleContextComponent CreateBattle(SubScene subScene)
        {
            CurScene = subScene;
            CurBattleContextComponent = BattleContextComponent.Create(subScene);
            return CurBattleContextComponent;
        }

        /// <summary>
        /// 销毁当前战斗实例。
        /// <remarks>释放所有相关资源</remarks>
        /// </summary>
        public static void DestroyBattle()
        {
            CurScene.Dispose();
            CurScene = null;
            CurBattleContextComponent = null;
        }
    }
}
