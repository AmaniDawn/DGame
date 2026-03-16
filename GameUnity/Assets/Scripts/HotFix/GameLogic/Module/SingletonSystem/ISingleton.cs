namespace GameLogic
{
    public interface ISingleton
    {
        /// <summary>
        /// 激活单例
        /// </summary>
        void Active();

        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
    }

    public interface IUpdate
    {
        /// <summary>
        /// 轮询
        /// </summary>
        void OnUpdate();
    }

    public interface IFixedUpdate
    {
        /// <summary>
        /// 物理轮询
        /// </summary>
        void OnFixedUpdate();
    }

    public interface ILateUpdate
    {
        /// <summary>
        /// 后轮询
        /// </summary>
        void OnLateUpdate();
    }

    public interface IDrawGizmos
    {
        /// <summary>
        /// 绘制Gizmos
        /// </summary>
        void OnDrawGizmos();
    }

    public interface IDrawGizmosSelected
    {
        /// <summary>
        /// 选中时绘制Gizmos
        /// </summary>
        void OnDrawGizmosSelected();
    }
}