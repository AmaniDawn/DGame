namespace DGame
{
    public interface IUpdateModule
    {
        /// <summary>
        /// 模块轮询
        /// </summary>
        /// <param name="elapseSeconds">逻辑时间 秒为单位</param>
        /// <param name="realElapseSeconds">真实时间 秒为单位</param>
        void Update(float elapseSeconds, float realElapseSeconds);
    }

    /// <summary>
    /// 模块抽象基类
    /// </summary>
    public abstract class Module
    {
        /// <summary>
        /// 获取模块优先级
        /// </summary>
        /// <remarks>优先级高的模块会优先轮询 并且关闭操作会后执行</remarks>
        public virtual int Priority => 0;

        /// <summary>
        /// 创建模块
        /// </summary>
        public abstract void OnCreate();

        /// <summary>
        /// 销毁并清理模块
        /// </summary>
        public abstract void OnDestroy();
    }
}