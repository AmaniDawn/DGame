namespace GameBattle
{
    /// <summary>
    /// 单例对象的统一生命周期接口。
    /// </summary>
    public interface ISingleton
    {
        /// <summary>
        /// 销毁当前单例实例并释放相关资源。
        /// </summary>
        void Destroy();
    }
}