using UnityEngine;
using UnityEngine.Audio;
using YooAsset;

namespace DGame
{
    /// <summary>
    /// 音频代理
    /// </summary>
    public class AudioSourceAgent
    {
        /// <summary>
        /// 音频加载请求
        /// </summary>
        class LoadRequest
        {
            /// <summary>
            /// 音频代理加载路径
            /// </summary>
            public string path;

            /// <summary>
            /// 是否异步
            /// </summary>
            public bool async;

            /// <summary>
            /// 是否进池
            /// </summary>
            public bool inPool;

            public LoadRequest(string path, bool async, bool inPool)
            {
                this.path = path;
                this.async = async;
                this.inPool = inPool;
            }
        }

        private const float FADEOUT_DURATION = 0.2f;
        private const float FADEIN_DURATION = 0.2f;

        private int m_instanceID;
        private AudioSource m_audioSource;
        private AudioData m_audioData;
        private IAudioModule m_audioModule;
        private IResourceModule m_resourceModule;
        private Transform m_transform;
        private float m_volume = 1.0f;
        private float m_duration;
        private float m_fadeOutTime;
        private float m_fadeInTime;
        private bool m_inPool = false;

        private AudioAgentRuntimeState m_audioAgentRuntimeState = AudioAgentRuntimeState.None;

        private LoadRequest m_preLoadRequest = null;

        /// <summary>
        /// 实例化ID
        /// </summary>
        private int InstanceID => m_instanceID;

        /// <summary>
        /// 资源操作句柄
        /// </summary>
        public AudioData AudioData => m_audioData;

        /// <summary>
        /// 音量
        /// </summary>
        public float Volume
        {
            get => m_volume;
            set
            {
                if (m_audioSource != null)
                {
                    m_volume = value;
                    m_audioSource.volume = value;
                }
            }
        }

        /// <summary>
        /// 音频代理是否空闲
        /// </summary>
        public bool IsFree => m_audioSource == null || m_audioAgentRuntimeState == AudioAgentRuntimeState.End;

        /// <summary>
        /// 音频代理播放存在的时间
        /// </summary>
        public float Duration => m_duration;

        /// <summary>
        /// 音频代理当前音频长度
        /// </summary>
        public float Length => m_audioSource != null && m_audioSource.clip != null ? m_audioSource.clip.length : 0;

        /// <summary>
        /// 音频代理实例化位置
        /// </summary>
        public Vector3 Position
        {
            get => m_transform == null ? Vector3.zero : m_transform.position;
            set => m_transform.position = value;
        }

        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool IsLoop
        {
            get => m_audioSource != null && m_audioSource.loop;
            set
            {
                if (m_audioSource != null)
                {
                    m_audioSource.loop = value;
                }
            }
        }

        internal bool IsPlaying => m_audioSource != null && m_audioSource.isPlaying;

        /// <summary>
        /// 音频代理当前音源组件
        /// </summary>
        public AudioSource AudioSource => m_audioSource;

        /// <summary>
        /// 创建音频代理辅助器
        /// </summary>
        /// <param name="path">生效路径</param>
        /// <param name="async">是否异步</param>
        /// <param name="audioGroupCategory">音频轨道（类别。</param>
        /// <param name="inPool">是否池化。</param>
        /// <returns>音频代理辅助器。</returns>
        public static AudioSourceAgent Create(string path, bool async, AudioGroupCategory audioGroupCategory, bool inPool = false)
        {
            AudioSourceAgent sourceAgent = new AudioSourceAgent();
            sourceAgent.Init(audioGroupCategory);
            sourceAgent.Load(path, async, inPool);
            return sourceAgent;
        }

        /// <summary>
        /// 初始化音频代理器
        /// </summary>
        /// <param name="audioGroupCategory">音频轨道</param>
        /// <param name="index">音频代理辅助器编号</param>
        public void Init(AudioGroupCategory audioGroupCategory, int index = 0)
        {
            m_audioModule = ModuleSystem.GetModule<IAudioModule>();
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            GameObject host = new GameObject(Utility.StringUtil.Format("Audio Agent Helper - {0} - {1}",
                audioGroupCategory.AudioMixerGroup.name, index));
            host.transform.SetParent(audioGroupCategory.InstanceRoot);
            host.transform.localPosition = Vector3.zero;
            m_transform = host.transform;
            m_audioSource = host.AddComponent<AudioSource>();
            m_audioSource.playOnAwake = false;
            AudioMixerGroup[] audioMixerGroups = audioGroupCategory.AudioMixer.FindMatchingGroups(
                Utility.StringUtil.Format("Master/{0}/{1}", audioGroupCategory.AudioMixerGroup.name,
                    $"{audioGroupCategory.AudioMixerGroup.name} - {index}"));
            m_audioSource.outputAudioMixerGroup = audioMixerGroups.Length > 0 ? audioMixerGroups[0] : audioGroupCategory.AudioMixerGroup;
            m_audioSource.rolloffMode = audioGroupCategory.AudioGroupConfig.audioRolloffMode;
            m_audioSource.minDistance = audioGroupCategory.AudioGroupConfig.minDistance;
            m_audioSource.maxDistance = audioGroupCategory.AudioGroupConfig.maxDistance;
            m_instanceID = m_audioSource.GetInstanceID();
        }

        public void Load(string path, bool async, bool inPool = false)
        {
            m_inPool = inPool;

            if (m_audioAgentRuntimeState == AudioAgentRuntimeState.None ||
                m_audioAgentRuntimeState == AudioAgentRuntimeState.End)
            {
                m_duration = 0;
                if (!string.IsNullOrEmpty(path))
                {
                    if (inPool && m_audioModule.TryGetAssetHandle(path, out var handle))
                    {
                        OnAssetLoadComplete(handle);
                        return;
                    }

                    if (async)
                    {
                        m_audioAgentRuntimeState = AudioAgentRuntimeState.Loading;
                        handle = m_resourceModule.LoadAssetAsyncHandle<AudioClip>(path);
                        handle.Completed += OnAssetLoadComplete;
                    }
                    else
                    {
                        handle = m_resourceModule.LoadAssetSyncHandle<AudioClip>(path);
                        OnAssetLoadComplete(handle);
                    }
                }
            }
            else
            {
                m_preLoadRequest = new LoadRequest(path, async, inPool);

                if (m_audioAgentRuntimeState == AudioAgentRuntimeState.Playing)
                {
                    Stop(true);
                }
            }
        }

        /// <summary>
        /// 停止播放音频代理辅助器
        /// </summary>
        /// <param name="fadeOut">是否渐出</param>
        public void Stop(bool fadeOut = false)
        {
            if (m_audioSource != null)
            {
                if (fadeOut)
                {
                    m_fadeOutTime = FADEOUT_DURATION;
                    m_audioAgentRuntimeState = AudioAgentRuntimeState.FadingOut;
                }
                else
                {
                    m_audioSource.Stop();
                    m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
                }
            }
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            m_audioSource?.Pause();
        }

        /// <summary>
        /// 取消暂停播放
        /// </summary>
        public void UnPause()
        {
            m_audioSource?.UnPause();
        }

        /// <summary>
        /// 资源加载完成回调
        /// </summary>
        /// <param name="handle">资源句柄</param>
        private void OnAssetLoadComplete(AssetHandle handle)
        {
            if (handle != null)
            {
                if (m_inPool)
                {
                    m_audioModule.TryAddAssetHandle(handle.GetAssetInfo().Address, handle);
                }
            }

            if (m_preLoadRequest != null)
            {
                if (!m_inPool && handle != null)
                {
                    handle.Dispose();
                }
                m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
                string path = m_preLoadRequest.path;
                bool async = m_preLoadRequest.async;
                bool inPool = m_preLoadRequest.inPool;
                m_preLoadRequest = null;
                Load(path, async, inPool);
            }
            else if(handle != null)
            {
                AudioData.Release(m_audioData);
                m_audioData = AudioData.Spawn(handle, m_inPool);
                m_audioSource.clip = handle.AssetObject as AudioClip;

                if (m_audioSource.clip != null)
                {
                    m_audioSource.Play();
                    m_audioAgentRuntimeState = AudioAgentRuntimeState.Playing;
                }
                else
                {
                    m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
                }
            }
            else
            {
                m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
            }
        }

        /// <summary>
        /// 轮询音频代理器
        /// </summary>
        /// <param name="elapseSeconds">逻辑时间（秒）</param>
        public void Update(float elapseSeconds)
        {
            switch (m_audioAgentRuntimeState)
            {
                case AudioAgentRuntimeState.Playing:
                    if (!m_audioSource.isPlaying)
                    {
                        m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
                    }
                    break;

                case AudioAgentRuntimeState.FadingOut:
                    if (m_fadeOutTime > 0f)
                    {
                        m_fadeOutTime -= elapseSeconds;
                        m_audioSource.volume = m_volume * m_fadeOutTime / FADEOUT_DURATION;
                    }
                    else
                    {
                        Stop();
                        m_audioSource.volume = m_volume;
                        if (m_preLoadRequest != null)
                        {
                            string path = m_preLoadRequest.path;
                            bool async = m_preLoadRequest.async;
                            bool inPool = m_preLoadRequest.inPool;
                            m_preLoadRequest = null;
                            Load(path, async, inPool);
                        }
                    }
                    break;
                case AudioAgentRuntimeState.FadingIn:
                    if (m_fadeInTime < FADEIN_DURATION)
                    {
                        m_fadeInTime += elapseSeconds;
                        m_audioSource.volume = m_volume * m_fadeInTime / FADEIN_DURATION;
                    }
                    else
                    {
                        m_audioAgentRuntimeState = AudioAgentRuntimeState.Playing;
                        m_audioSource.volume = m_volume;
                        if (m_preLoadRequest != null)
                        {
                            string path = m_preLoadRequest.path;
                            bool async = m_preLoadRequest.async;
                            bool inPool = m_preLoadRequest.inPool;
                            m_preLoadRequest = null;
                            Load(path, async, inPool);
                        }
                    }
                    break;
            }

            m_duration += elapseSeconds;
        }

        /// <summary>
        /// 销毁音频代理器
        /// </summary>
        public void Destroy()
        {
            if (m_transform != null)
            {
                Object.Destroy(m_transform.gameObject);
            }

            AudioData.Release(m_audioData);
            m_audioData = null;
        }
    }
}