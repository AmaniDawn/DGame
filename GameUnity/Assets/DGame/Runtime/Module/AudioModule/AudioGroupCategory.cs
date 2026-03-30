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