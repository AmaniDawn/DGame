using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DGame
{
    internal class AnimPlayable : IMemory, IAnimPlayable
    {
        private readonly List<AnimClip> m_animClips;
        private readonly List<AnimMixer> m_animMixers;

        private Animator m_animator;
        private bool m_isDestroyed;

        public string Name => m_animator != null ? m_animator.name : string.Empty;
        public int AnimClipCount => m_animClips.Count;
        public int AnimMixerCount => m_animMixers.Count;
        public bool IsDestroyed => m_isDestroyed;

        private PlayableGraph m_graph;
        private AnimationPlayableOutput m_output;
        private AnimationLayerMixerPlayable m_mixerRoot;

        public AnimPlayable()
        {
            m_animClips = new List<AnimClip>(8);
            m_animMixers = new List<AnimMixer>(8);
            m_isDestroyed = true;
        }

        public void OnRelease()
        {
            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];
                animClip?.Destroy();
            }
            for (int i = 0; i < m_animMixers.Count; i++)
            {
                var animMixer = m_animMixers[i];
                animMixer?.Destroy();
            }
            m_animClips?.Clear();
            m_animMixers?.Clear();
            m_graph.Destroy();
            m_isDestroyed = true;
        }

        public static AnimPlayable Create(Animator animator)
        {
            if (animator == null || animator.gameObject == null)
            {
                throw new DGameException("传入的Animator无效");
            }
            string name = animator.gameObject.name;
            AnimPlayable animPlayable = MemoryPool.Spawn<AnimPlayable>();
            animPlayable.m_animator = animator;
            animPlayable.m_graph = PlayableGraph.Create(name);
            animPlayable.m_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            animPlayable.m_mixerRoot = AnimationLayerMixerPlayable.Create(animPlayable.m_graph);
            animPlayable.m_output = AnimationPlayableOutput.Create(animPlayable.m_graph, name, animator);
            animPlayable.m_output.SetSourcePlayable(animPlayable.m_mixerRoot);
            animPlayable.m_isDestroyed = false;
            return animPlayable;
        }

        public void Update(float elapsedSeconds)
        {
            m_graph.Evaluate(elapsedSeconds);

            // 更新所有层级
            for (int i = 0; i < m_animMixers.Count; i++)
            {
                var animMixer = m_animMixers[i];

                if (animMixer.IsConnected)
                {
                    animMixer.Update(elapsedSeconds);
                }
            }
        }

        public void DestroyGraph() => MemoryPool.Release(this);

        public void PlayGraph() => m_graph.Play();

        public void StopGraph() => m_graph.Stop();

        /// <summary>
        /// 获取动画的信息
        /// </summary>
        /// <param name="animName"></param>
        /// <returns></returns>
        public AnimInfo GetAnimInfo(string animName)
        {
            int animHashCode = Animator.StringToHash(animName);
            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];

                if (animClip != null && animClip.AnimHashCode == animHashCode)
                {
                    return animClip.Info;
                }
            }
            return null;
        }

        private AnimClip GetAnimClip(string animName)
        {
            int animHashCode = Animator.StringToHash(animName);
            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];

                if (animClip != null && animClip.AnimHashCode == animHashCode)
                {
                    return animClip;
                }
            }
            return null;
        }

        public bool IsPlaying(string animName)
        {
            var animInfo = GetAnimClip(animName);

            return animInfo != null && animInfo.IsConnected && animInfo.IsPlaying;
        }

        private AnimMixer GetAnimMixer(int layer)
        {
            for (int i = 0; i < m_animMixers.Count; i++)
            {
                var animMixer = m_animMixers[i];

                if (animMixer != null && animMixer.Layer == layer)
                {
                    return animMixer;
                }
            }
            return null;
        }

        public void Play(string animName, float fadeLength)
        {
            var animClip = GetAnimClip(animName);

            if (animClip == null)
            {
                DLogger.Warning($"没有找到动画：{animName}");
                return;
            }

            int layer = animClip.Layer;
            var animMixer = GetAnimMixer(layer);

            if (animMixer == null)
            {
                animMixer = CreateAnimMixer(layer);
            }

            if (!animMixer.IsConnected)
            {
                animMixer.Connect(m_mixerRoot, animMixer.Layer);
            }

            animMixer.Play(animClip, fadeLength);
        }

        public void Stop(string animName)
        {
            var animClip = GetAnimClip(animName);
            if (animClip == null)
            {
                DLogger.Warning($"没有找到动画：{animName}");
                return;
            }

            if (!animClip.IsConnected)
            {
                return;
            }

            var animMixer = GetAnimMixer(animClip.Layer);

            if (animMixer == null)
            {
                throw new DGameException("animMixer无效");
            }
            animMixer.Stop(animClip.AnimHashCode);
        }

        /// <summary>
        /// 添加一个动画片段
        /// </summary>
        /// <param name="name">动画名</param>
        /// <param name="clip">资源</param>
        /// <param name="layer">层级</param>
        /// <param name="fadeDuration">过渡时间</param>
        /// <returns></returns>
        public bool AddAnimationClip(string name, AnimationClip clip, WrapMode wrapMode, int layer = 0, float fadeDuration = 0f)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("动画名称无效");
            }

            if (clip == null)
            {
                throw new DGameException("动画片段无效");
            }

            if (layer < 0)
            {
                throw new DGameException("动画层级必须大于等于0");
            }

            if (ContainsAnimationClip(name))
            {
                DLogger.Warning($"动画片段已经存在{name}");
                return false;
            }

            AnimClip animClip = new AnimClip(m_graph, clip, name, wrapMode, layer, fadeDuration);
            m_animClips.Add(animClip);
            return true;
        }

        public bool RemoveAnimationClip(string name)
        {
            if (!ContainsAnimationClip(name))
            {
                DLogger.Warning($"动画片段不存在{name}");
                return false;
            }
            AnimClip animClip = GetAnimClip(name);
            AnimMixer animMixer = GetAnimMixer(animClip.Layer);

            if (animMixer != null)
            {
                animMixer.RemoveAnimClip(animClip.AnimHashCode);
            }
            animClip.Destroy();
            m_animClips.Remove(animClip);
            return true;
        }

        public bool ContainsAnimationClip(string name)
        {
            int animHashCode = Animator.StringToHash(name);
            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];

                if (animClip != null && animClip.AnimHashCode == animHashCode)
                {
                    return true;
                }
            }
            return false;
        }

        private AnimMixer CreateAnimMixer(int layer)
        {
            int inputCount = m_mixerRoot.GetInputCount();

            if (layer == 0 && inputCount == 0)
            {
                m_mixerRoot.SetInputCount(1);
            }
            else
            {
                if (layer > inputCount - 1)
                {
                    m_mixerRoot.SetInputCount(layer + 1);
                }
            }
            var animMixer = new AnimMixer(m_graph, layer);
            m_animMixers.Add(animMixer);
            return animMixer;
        }
    }
}