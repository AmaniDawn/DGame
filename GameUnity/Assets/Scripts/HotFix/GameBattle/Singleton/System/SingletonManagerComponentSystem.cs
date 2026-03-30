using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 负责在战斗上下文销毁时回收战斗域单例管理组件。
    /// </summary>
    public sealed class SingletonManagerComponentDestroySystem : DestroySystem<SingletonManagerComponent>
    {
        /// <summary>
        /// 销毁战斗域单例管理组件及其登记的全部单例。
        /// </summary>
        /// <param name="self">待销毁的单例管理组件。</param>
        protected override void Destroy(SingletonManagerComponent self)
        {
            self.Destroy();
        }
    }
}