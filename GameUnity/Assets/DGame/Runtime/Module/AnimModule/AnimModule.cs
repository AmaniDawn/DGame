using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    internal sealed class AnimModule : Module, IUpdateModule, IAnimModule
    {
        private readonly Dictionary<string, IAnimPlayable> m_animPlayables;
        private readonly List<IAnimPlayable> m_tempAnimPlayableList;

        public int Count => m_animPlayables.Count;

        public override int Priority => 1;

        public AnimModule()
        {
            m_animPlayables = new Dictionary<string, IAnimPlayable>();
            m_tempAnimPlayableList = new List<IAnimPlayable>();
        }

        public override void OnCreate()
        { }

        public override void OnDestroy()
        {
            foreach (var item in m_animPlayables.Values)
            {
                item?.DestroyGraph();
            }
            m_animPlayables.Clear();
            m_tempAnimPlayableList.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (m_animPlayables == null || m_animPlayables.Count <= 0)
            {
                return;
            }
            m_tempAnimPlayableList.Clear();

            foreach (var item in m_animPlayables.Values)
            {
                m_tempAnimPlayableList.Add(item);
            }

            for (int i = 0; i < m_tempAnimPlayableList.Count; i++)
            {
                var animPlayable = m_tempAnimPlayableList[i];

                if (animPlayable == null || animPlayable.IsDestroyed)
                {
                    continue;
                }
                animPlayable.Update(elapseSeconds);
            }
        }

        public bool ContainsAnimPlayable(string name) => m_animPlayables.ContainsKey(name);

        public IAnimPlayable GetAnimPlayable(string name) => m_animPlayables.GetValueOrDefault(name, null);

        public IAnimPlayable[] GetAllAnimPlayable()
        {
            if (m_animPlayables == null || m_animPlayables.Count <= 0)
            {
                return null;
            }
            IAnimPlayable[] results = new IAnimPlayable[Count];
            int index = 0;
            foreach (var item in m_animPlayables.Values)
            {
                results[index++] = item;
            }
            return results;
        }

        public void GetAllAnimPlayable(List<IAnimPlayable> results)
        {
            if (results == null)
            {
                throw new DGameException("传入参数无效");
            }
            results.Clear();
            foreach (var item in m_animPlayables.Values)
            {
                results.Add(item);
            }
        }

        public IAnimPlayable CreateAnimPlayable(Animator animator)
        {
            var animPlayable = AnimPlayable.Create(animator);
            if (!m_animPlayables.TryAdd(animPlayable.Name, animPlayable))
            {
                throw new DGameException($"已存在同名的动画图: {animPlayable.Name}");
            }

            return animPlayable;
        }

        public IAnimPlayable CreateAnimPlayable(Animator animator, List<AnimationClip> animations)
        {
            List<AnimationWrapper> warps = new List<AnimationWrapper>(animations.Count);

            for (int i = 0; i < animations.Count; i++)
            {
                warps.Add(new AnimationWrapper()
                {
                    Clip = animations[i],
                    WrapMode = animations[i].wrapMode,
                    Layer = 0
                });
            }

            return CreateAnimPlayable(animator, warps);
        }

        public IAnimPlayable CreateAnimPlayable(Animator animator, List<AnimationWrapper> animations)
        {
            var animPlayable = AnimPlayable.Create(animator);
            if (!m_animPlayables.TryAdd(animPlayable.Name, animPlayable))
            {
                throw new DGameException($"已存在同名的动画图: {animPlayable.Name}");
            }
            for (int i = 0; i < animations.Count; i++)
            {
                var animation = animations[i];
                animPlayable.AddAnimationClip(animation.Clip.name, animation.Clip, animation.WrapMode, animation.Layer, animation.FadeDuration);
            }
            return animPlayable;
        }

        public IAnimPlayable CreateAnimPlayable(Animator animator, params AnimationWrapper[] animations)
        {
            var animPlayable = AnimPlayable.Create(animator);
            if (!m_animPlayables.TryAdd(animPlayable.Name, animPlayable))
            {
                throw new DGameException($"已存在同名的动画图: {animPlayable.Name}");
            }
            for (int i = 0; i < animations.Length; i++)
            {
                var animation = animations[i];
                animPlayable.AddAnimationClip(animation.Clip.name, animation.Clip, animation.WrapMode, animation.Layer, animation.FadeDuration);
            }
            return animPlayable;
        }

        public bool DestroyAnimPlayable(IAnimPlayable animPlayable)
        {
            if (animPlayable == null)
            {
                throw new DGameException("传入的动画图无效");
            }

            if (m_animPlayables.ContainsKey(animPlayable.Name))
            {
                animPlayable.DestroyGraph();
                m_animPlayables.Remove(animPlayable.Name);
                return true;
            }
            return false;
        }

        public bool DestroyAnimPlayable(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("传入的动画图名称无效");
            }
            var animPlayable = GetAnimPlayable(name);
            if (animPlayable != null)
            {
                animPlayable.DestroyGraph();
                m_animPlayables.Remove(animPlayable.Name);
                return true;
            }
            return false;
        }
    }
}