using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public interface IAnimModule
    {
        /// <summary>
        /// AnimPlayable数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 是否存在动画图
        /// </summary>
        /// <param name="name">动画图名称</param>
        /// <returns></returns>
        bool ContainsAnimPlayable(string name);

        /// <summary>
        /// 获取动画图
        /// </summary>
        /// <param name="name">动画图名称</param>
        /// <returns></returns>
        IAnimPlayable GetAnimPlayable(string name);

        /// <summary>
        /// 获取所有动画图
        /// </summary>
        /// <returns></returns>
        IAnimPlayable[] GetAllAnimPlayable();

        /// <summary>
        /// 获取所有动画图
        /// </summary>
        /// <param name="results">结果列表</param>
        void GetAllAnimPlayable(List<IAnimPlayable> results);

        /// <summary>
        /// 创建动画图
        /// </summary>
        /// <param name="animator">动画控制器</param>
        /// <returns></returns>
        IAnimPlayable CreateAnimPlayable(Animator animator);

        /// <summary>
        /// 创建动画图
        /// </summary>
        /// <param name="animator">动画控制器</param>
        /// <param name="animations">动画片段列表</param>
        /// <returns></returns>
        IAnimPlayable CreateAnimPlayable(Animator animator, List<AnimationClip> animations);

        /// <summary>
        /// 创建动画图
        /// </summary>
        /// <param name="animator">动画控制器</param>
        /// <param name="animations">动画片段列表</param>
        /// <returns></returns>
        IAnimPlayable CreateAnimPlayable(Animator animator, List<AnimationWrapper> animations);

        /// <summary>
        /// 创建动画图
        /// </summary>
        /// <param name="animator">动画控制器</param>
        /// <param name="animations">动画片段数组</param>
        /// <returns></returns>
        IAnimPlayable CreateAnimPlayable(Animator animator, AnimationWrapper[] animations);

        /// <summary>
        /// 删除动画图
        /// </summary>
        /// <param name="animPlayable"></param>
        /// <returns></returns>
        bool DestroyAnimPlayable(IAnimPlayable animPlayable);

        /// <summary>
        /// 删除动画图
        /// </summary>
        /// <param name="name">动画图名称</param>
        /// <returns></returns>
        bool DestroyAnimPlayable(string name);
    }
}