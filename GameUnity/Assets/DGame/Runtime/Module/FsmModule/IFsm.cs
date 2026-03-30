using System;
using System.Collections.Generic;

namespace DGame
{
    public interface IFsm<T> where T : class
    {
        /// <summary>
        /// 状态机名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 状态机完整名称
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// 状态机持有者
        /// </summary>
        T Owner { get; }

        /// <summary>
        /// 状态数量
        /// </summary>
        int FsmStateCount { get; }

        /// <summary>
        /// 状态机是否在运行中
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 状态机是否被销毁
        /// </summary>
        bool IsDestroyed { get; }

        /// <summary>
        /// 状态机当前状态
        /// </summary>
        IFsmState<T> CurrentState { get; }

        /// <summary>
        /// 当前状态持续时间
        /// </summary>
        float CurrentStateTime { get; }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        void Start<TState>() where TState : IFsmState<T>;

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <param name="stateType"></param>
        void Start(Type stateType);

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        void SwitchState<TState>() where TState : IFsmState<T>;

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="type"></param>
        void SwitchState(Type type);

        /// <summary>
        /// 检查状态是否存在
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        bool ContainsFsmState<TState>() where TState : IFsmState<T>;

        /// <summary>
        /// 检查状态是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool ContainsFsmState(Type type);

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        IFsmState<T> GetState<TState>() where TState : IFsmState<T>;

        /// <summary>
        /// 获取状态
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IFsmState<T> GetState(Type type);

        /// <summary>
        /// 获取所有状态
        /// </summary>
        /// <returns></returns>
        IFsmState<T>[] GetAllStates();

        /// <summary>
        /// 获取所有状态
        /// </summary>
        /// <returns></returns>
        void GetAllStates(List<IFsmState<T>> states);

        void AddAnimationClip(params AnimationWrapper[] animations);

        void AddAnimationClip(List<AnimationWrapper> animations);

        void AddAnimationClip(AnimationWrapper animation);

        void PlayAnimation(string animationName, float fadeDuration = 0.25f);

        #region 状态机共享数据

        /// <summary>
        /// 是否存在有限状态机数据
        /// </summary>
        /// <param name="name">状态机数据名称</param>
        /// <returns></returns>
        bool ContainsShareData(string name);

        /// <summary>
        /// 获取状态机数据
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        TData GetShareData<TData>(string name);

        /// <summary>
        /// 获取状态机数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        bool TryGetShareData<TData>(string name, out TData data);

        /// <summary>
        /// 设置状态机数据
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <param name="data">数据</param>
        /// <typeparam name="TData">数据类型</typeparam>
        void AddShareData<TData>(string name, TData data);

        /// <summary>
        /// 更新状态机数据
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <param name="data">数据</param>
        /// <typeparam name="TData">数据类型</typeparam>
        void UpdateShareData<TData>(string name, TData data);

        /// <summary>
        /// 移除状态机数据
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <returns></returns>
        bool RemoveShareData(string name);

        /// <summary>
        /// 清空数据
        /// </summary>
        void ClearShareData();

        #endregion
    }
}