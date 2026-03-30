using System;

namespace DGame
{
    public abstract class BaseState<T> : IFsmState<T> where T : class
    {
        private IFsm<T> m_fsm;
        protected IFsm<T> OwnerFsm => m_fsm;
        protected AnimationWrapper Wrapper;

        public virtual void OnCreate(IFsm<T> fsm)
        {
            m_fsm = fsm;
            AddAnimationClip(Wrapper);
        }

        public virtual void OnEnter()
        {
            PlayAnimation();
        }

        public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds) { }

        public virtual void OnFixedUpdate() { }

        public virtual void OnExit() { }

        public virtual void OnDestroy() { }

        #region 动画相关

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="fadeDuration">过渡时间</param>
        protected void PlayAnimation(string animationName, float fadeDuration = 0.25f)
        {
            m_fsm?.PlayAnimation(animationName, fadeDuration);
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        protected void PlayAnimation()
        {
            if (Wrapper == null || Wrapper.Clip == null || string.IsNullOrEmpty(Wrapper.Clip.name))
            {
                return;
            }
            PlayAnimation(Wrapper.Clip.name, Wrapper.FadeDuration);
        }

        /// <summary>
        /// 添加动画片段
        /// </summary>
        /// <param name="wrapper">动画片段</param>
        protected void AddAnimationClip(AnimationWrapper wrapper)
        {
            m_fsm?.AddAnimationClip(wrapper);
        }

        #endregion

        #region 切换状态

        protected void SwitchState<TState>() where TState : class, IFsmState<T>
        {
            if (m_fsm == null)
            {
                throw new DGameException("当前持有状态机无效");
            }
            m_fsm?.SwitchState<TState>();
        }

        protected void SwitchState(Type stateType)
        {
            if (m_fsm == null)
            {
                throw new DGameException("当前持有状态机无效");
            }
            m_fsm?.SwitchState(stateType);
        }

        #endregion

        #region 状态机共享数据

        /// <summary>
        /// 是否存在有限状态机数据
        /// </summary>
        /// <param name="name">状态机数据名称</param>
        /// <returns></returns>
        protected bool ContainsShareData(string name)
        {
            return m_fsm.ContainsShareData(name);
        }

        /// <summary>
        /// 获取状态机数据
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        protected TData GetShareData<TData>(string name)
        {
            return m_fsm.GetShareData<TData>(name);
        }

        /// <summary>
        /// 获取状态机数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        protected bool TryGetShareData<TData>(string name, out TData data)
        {
            return m_fsm.TryGetShareData<TData>(name, out data);
        }

        /// <summary>
        /// 设置状态机数据
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <param name="data">数据</param>
        /// <typeparam name="TData">数据类型</typeparam>
        protected void AddShareData<TData>(string name, TData data)
        {
            m_fsm.AddShareData(name, data);
        }

        /// <summary>
        /// 更新状态机数据
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <param name="data">数据</param>
        /// <typeparam name="TData">数据类型</typeparam>
        protected void UpdateShareData<TData>(string name, TData data)
        {
            m_fsm.UpdateShareData(name, data);
        }

        /// <summary>
        /// 移除状态机数据
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <returns></returns>
        protected bool RemoveShareData(string name)
        {
            return m_fsm.RemoveShareData(name);
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        protected void ClearShareData()
        {
            m_fsm.ClearShareData();
        }

        #endregion
    }
}