namespace DGame
{
    public interface IFsmState<T> where T : class
    {
        /// <summary>
        /// 状态创建的时候执行的方法
        /// </summary>
        /// <param name="fsm">状态机</param>
        void OnCreate(IFsm<T> fsm);

        /// <summary>
        /// 状态进入
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 状态更新
        /// </summary>
        void OnUpdate(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 物理帧更新更新
        /// </summary>
        void OnFixedUpdate();

        /// <summary>
        /// 状态退出
        /// </summary>
        void OnExit();

        /// <summary>
        /// 状态销毁
        /// </summary>
        void OnDestroy();
    }
}