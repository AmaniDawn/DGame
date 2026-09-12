using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace DGame
{
    /// <summary>
    /// 音频轨道类别
    /// </summary>
    [Serializable]
    public class AudioGroupCategory
    {
        [SerializeField]
        private AudioMixer audioMixer = null;

        public List<AudioSourceAgent> audioAgents;

        private readonly AudioMixerGroup m_audioMixerGroup;
        private AudioGroupConfig m_audioGroupConfig;
        private int m_maxChannel;
        private bool m_enable = true;

        /// <summary>
        /// 音频混响器
        /// </summary>
        public AudioMixer AudioMixer => audioMixer;

        /// <summary>
        /// 音频混响器组
        /// </summary>
        public AudioMixerGroup AudioMixerGroup => m_audioMixerGroup;

        /// <summary>
        /// 音频组配置文件
        /// </summary>
        public AudioGroupConfig AudioGroupConfig => m_audioGroupConfig;

        /// <summary>
        /// 实例化根节点
        /// </summary>
        public Transform InstanceRoot { get; private set; }

        /// <summary>
        /// 音频轨道是否启用
        /// </summary>
        public bool Enable
        {
            get => m_enable;
            set
            {
                if (m_enable != value)
                {
                    m_enable = value;

                    if (!m_enable)
                    {
                        foreach (var audioAgent in audioAgents)
                        {
                            if (audioAgent != null)
                            {
                                audioAgent.Stop();
                            }
                        }
                    }
                }
            }
        }

        public AudioGroupCategory(int maxChannel, AudioMixer audioMixer, AudioGroupConfig audioGroupConfig)
        {
            var audioModule = ModuleSystem.GetModule<IAudioModule>();
            this.audioMixer = audioMixer;
            this.m_audioGroupConfig = audioGroupConfig;
            this.m_maxChannel = maxChannel;
            AudioMixerGroup[] audioMixerGroups =
                audioMixer.FindMatchingGroups(Utility.StringUtil.Format("Master/{0}",
                    audioGroupConfig.Name));

            if (audioMixerGroups.Length > 0)
            {
                m_audioMixerGroup = audioMixerGroups[0];
            }
            else
            {
                var masterGroups = audioMixer.FindMatchingGroups("Master");
                m_audioMixerGroup = masterGroups.Length > 0 ? masterGroups[0] : null;
                // m_audioMixerGroup = audioMixer.FindMatchingGroups("Master")[0];
            }

            audioAgents = new List<AudioSourceAgent>(32);
            InstanceRoot = new GameObject(Utility.StringUtil.Format("Audio Category - {0}", audioGroupConfig.Name)).transform;
            InstanceRoot.SetParent(audioModule.InstanceRoot);
            for (int index = 0; index < m_maxChannel; index++)
            {
                AudioSourceAgent audioSourceAgent = new AudioSourceAgent();
                audioSourceAgent.Init(this, index);
                audioAgents.Add(audioSourceAgent);
            }
        }

        /// <summary>
        /// 增加音频代理
        /// </summary>
        /// <param name="num"></param>
        public void AddAudioAgent(int num)
        {
            m_maxChannel += num;

            for (int i = 0; i < num; i++)
            {
                audioAgents.Add(null);
            }
        }

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="path"></param>
        /// <param name="async"></param>
        /// <param name="inPool"></param>
        /// <returns></returns>
        public AudioSourceAgent Play(string path, bool async, bool inPool = false)
        {
            if (!m_enable)
            {
                return null;
            }

            int freeChannel = -1;
            float duration = -1;

            for (int i = 0; i < audioAgents.Count; i++)
            {
                var audioAgent = audioAgents[i];

                if (audioAgent == null)
                {
                    freeChannel = i;
                    break;
                }

                if (audioAgent.AudioData?.AssetHandle == null || audioAgent.IsFree)
                {
                    freeChannel = i;
                    break;
                }
                else if (audioAgent.Duration > duration)
                {
                    duration = audioAgent.Duration;
                    freeChannel = i;
                }
            }

            if (freeChannel >= 0)
            {
                if (audioAgents[freeChannel] == null)
                {
                    audioAgents[freeChannel] = AudioSourceAgent.Create(path, async, this, inPool);
                }
                else
                {
                    audioAgents[freeChannel].Load(path, async, inPool);
                }
                return audioAgents[freeChannel];
            }
            else
            {
                DLogger.Error($"当前没有空闲的音频组件播放音频：{path}");
                return null;
            }
        }

        /// <summary>按音效配置申请代理；同资源超过上限拒绝，分类满载时淘汰最低优先级代理。</summary>
        public AudioSourceAgent Play(string path, AudioPlayOptions options, bool async, bool inPool = false)
        {
            if (!m_enable) return null;
            int sameCount = 0;
            int freeChannel = -1;
            for (int i = 0; i < audioAgents.Count; i++)
            {
                var agent = audioAgents[i];
                if (agent == null) { freeChannel = i; break; }
                if (!agent.IsFree && (options.SoundId > 0
                    ? (agent.SoundId == options.SoundId || agent.PendingSoundId == options.SoundId)
                    : (agent.Path == path || agent.HasPendingPath(path)))) sameCount++;
                if (freeChannel < 0 && agent.IsFree) freeChannel = i;
            }
            if (options.MaxPlayCount > 0 && sameCount >= options.MaxPlayCount) return null;
            if (freeChannel >= 0)
            {
                var freeAgent = audioAgents[freeChannel] ?? AudioSourceAgent.CreateEmpty(this);
                audioAgents[freeChannel] = freeAgent;
                freeAgent.Load(path, options, async, inPool);
                return freeAgent;
            }
            AudioSourceAgent candidate = null;
            for (int i = 0; i < audioAgents.Count; i++)
            {
                var agent = audioAgents[i];
                if (agent == null || agent.IsFree) continue;
                if (candidate == null || agent.EffectivePriority > candidate.EffectivePriority ||
                    (agent.EffectivePriority == candidate.EffectivePriority && agent.Duration > candidate.Duration)) candidate = agent;
            }
            if (candidate == null || candidate.EffectivePriority < options.Priority ||
                (candidate.EffectivePriority == options.Priority && !options.AllowEqualPriority)) return null;
            candidate.Stop(true);
            candidate.Load(path, options, async, inPool);
            return candidate;
        }

        internal bool TryReplayRandom(AudioSourceAgent agent)
        {
            var options = agent.ActiveOptions;
            if (!options.UseRandomSelection)
            {
                return false;
            }
            if (options.MaxPlayCount > 0 && options.SoundId > 0)
            {
                int count = 0;
                for (int i = 0; i < audioAgents.Count; i++)
                {
                    var other = audioAgents[i];
                    if (other == null || ReferenceEquals(other, agent) || other.IsFree) continue;
                    if (other.SoundId == options.SoundId || other.PendingSoundId == options.SoundId) count++;
                }
                if (count >= options.MaxPlayCount) return false;
            }
            string path = SelectRandomPath(options, agent.Path);
            if (string.IsNullOrEmpty(path)) return false;
            agent.Load(path, options, agent.RequestAsync, agent.RequestInPool);
            return true;
        }

        private static string SelectRandomPath(AudioPlayOptions options, string fallback)
        {
            float total = 0f;
            for (int i = 0; i < options.RandomLocations.Length; i++)
            {
                if (string.IsNullOrEmpty(options.RandomLocations[i])) continue;
                total += options.RandomWeights != null && i < options.RandomWeights.Length ? Mathf.Max(0f, options.RandomWeights[i]) : 1f;
            }
            if (total <= 0f) return fallback;
            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < options.RandomLocations.Length; i++)
            {
                if (string.IsNullOrEmpty(options.RandomLocations[i])) continue;
                roll -= options.RandomWeights != null && i < options.RandomWeights.Length ? Mathf.Max(0f, options.RandomWeights[i]) : 1f;
                if (roll <= 0f) return options.RandomLocations[i];
            }
            return fallback;
        }

        /// <summary>
        /// 停止音频
        /// </summary>
        /// <param name="fadeOut">是否渐出</param>
        public void Stop(bool fadeOut)
        {
            for (int i = 0; i < audioAgents.Count; i++)
            {
                var audioAgent = audioAgents[i];

                if (audioAgent != null)
                {
                    audioAgent.Stop(fadeOut);
                }
            }
        }

        /// <summary>
        /// 音频轨道轮询
        /// </summary>
        /// <param name="elapsedSeconds">逻辑时间（秒）</param>
        public void Update(float elapsedSeconds)
        {
            for (int i = 0; i < audioAgents.Count; i++)
            {
                var audioAgent = audioAgents[i];

                if (audioAgent != null)
                {
                    audioAgent.Update(elapsedSeconds);
                }
            }
        }
    }
}
