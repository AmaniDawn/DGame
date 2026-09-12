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
        sealed class LoadRequest : MemoryObject
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
            public AudioPlayOptions options;
            public bool hasOptions;

            public override void OnRelease()
            {
                path = null;
                async = false;
                inPool = false;
                options = default;
                hasOptions = false;
            }

            public static LoadRequest Spawn(string path, bool async, bool inPool, AudioPlayOptions options, bool hasOptions)
            {
                var request = MemoryObject.Spawn<LoadRequest>();
                request.path = path;
                request.async = async;
                request.inPool = inPool;
                request.options = options;
                request.hasOptions = hasOptions;
                return request;
            }
        }


#if UNITY_6000_1_OR_NEWER
        private EntityId m_instanceID;
#else
        private int m_instanceID;
#endif
        private AudioSource m_audioSource;
        private AudioData m_audioData;
        private IAudioModule m_audioModule;
        private IResourceModule m_resourceModule;
        private Transform m_transform;
        private float m_volume = 1.0f;
        private float m_duration;
        private float m_fadeOutTime;
        private float m_fadeOutStartVolume;
        private float m_fadeInTime;
        private float m_fadeInDuration;
        private float m_fadeOutDuration = 0.2f;
        private float m_endPauseTime;
        private float m_endPauseRemaining;
        private bool m_inPool = false;
        private bool m_isPaused;
        private bool m_isLoop;
        private AssetHandle m_loadingHandle;
        private bool m_destroyed;
        private AudioPlayOptions m_activeOptions;
        private bool m_requestAsync;
        private bool m_requestInPool;
        private AudioGroupCategory m_audioGroupCategory;

        private AudioAgentRuntimeState m_audioAgentRuntimeState = AudioAgentRuntimeState.None;

        private LoadRequest m_preLoadRequest = null;
        private string m_path;

        /// <summary>
        /// 实例化ID
        /// </summary>
#if UNITY_6000_1_OR_NEWER
        private EntityId InstanceID => m_instanceID;
#else
        private int InstanceID => m_instanceID;
#endif

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
        public bool IsFree => m_audioSource == null ||
            m_audioAgentRuntimeState == AudioAgentRuntimeState.None ||
            m_audioAgentRuntimeState == AudioAgentRuntimeState.End;

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
            get => m_isLoop;
            set
            {
                if (m_audioSource != null)
                {
                    m_isLoop = value;
                    m_audioSource.loop = value && m_endPauseTime <= 0f;
                }
            }
        }

        internal bool IsPlaying => m_audioSource != null && m_audioSource.isPlaying;

        /// <summary>
        /// 音频代理当前音源组件
        /// </summary>
        public AudioSource AudioSource => m_audioSource;
        internal string Path => m_path;
        internal int SoundId => m_activeOptions.SoundId;
        internal int PendingSoundId => m_preLoadRequest != null && m_preLoadRequest.hasOptions ? m_preLoadRequest.options.SoundId : SoundId;
        internal AudioPlayOptions ActiveOptions => m_activeOptions;
        internal bool RequestAsync => m_requestAsync;
        internal bool RequestInPool => m_requestInPool;
        internal bool HasPendingPath(string path) => m_preLoadRequest != null && m_preLoadRequest.path == path;
        internal int EffectivePriority => m_preLoadRequest != null && m_preLoadRequest.hasOptions ? m_preLoadRequest.options.Priority : Priority;

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

        internal static AudioSourceAgent CreateEmpty(AudioGroupCategory audioGroupCategory)
        {
            var sourceAgent = new AudioSourceAgent();
            sourceAgent.Init(audioGroupCategory);
            return sourceAgent;
        }

        /// <summary>
        /// 初始化音频代理器
        /// </summary>
        /// <param name="audioGroupCategory">音频轨道</param>
        /// <param name="index">音频代理辅助器编号</param>
        public void Init(AudioGroupCategory audioGroupCategory, int index = 0)
        {
            m_audioGroupCategory = audioGroupCategory;
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
#if UNITY_6000_1_OR_NEWER
            m_instanceID = m_audioSource.GetEntityId();
#else
            m_instanceID = m_audioSource.GetInstanceID();
#endif
        }

        public float Pitch { get => m_audioSource == null ? 1f : m_audioSource.pitch; set { if (m_audioSource != null) m_audioSource.pitch = value; } }
        public float FadeInTime { get => m_fadeInDuration; set => m_fadeInDuration = Mathf.Max(0f, value); }
        public float FadeOutTime { get => m_fadeOutDuration; set => m_fadeOutDuration = Mathf.Max(0f, value); }
        public float EndPauseTime
        {
            get => m_endPauseTime;
            set
            {
                m_endPauseTime = Mathf.Max(0f, value);
                if (m_audioSource != null) m_audioSource.loop = m_isLoop && m_endPauseTime <= 0f;
            }
        }
        public float SpatialBlend { get => m_audioSource == null ? 0f : m_audioSource.spatialBlend; set { if (m_audioSource != null) m_audioSource.spatialBlend = Mathf.Clamp01(value); } }
        public float MinDistance { get => m_audioSource == null ? 1f : m_audioSource.minDistance; set { if (m_audioSource != null) m_audioSource.minDistance = Mathf.Max(0f, value); } }
        public float MaxDistance { get => m_audioSource == null ? 500f : m_audioSource.maxDistance; set { if (m_audioSource != null) m_audioSource.maxDistance = Mathf.Max(MinDistance, value); } }
        public int Priority { get => m_audioSource == null ? 128 : m_audioSource.priority; set { if (m_audioSource != null) m_audioSource.priority = Mathf.Clamp(value, 0, 256); } }
        public AudioRolloffMode RolloffMode { get => m_audioSource == null ? AudioRolloffMode.Logarithmic : m_audioSource.rolloffMode; set { if (m_audioSource != null) m_audioSource.rolloffMode = value; } }

        internal void ApplyFadeInAfterPlay()
        {
            if (m_audioSource != null && m_audioSource.isPlaying && m_fadeInDuration > 0f)
            {
                m_fadeInTime = 0f;
                m_audioSource.volume = 0f;
                m_audioAgentRuntimeState = AudioAgentRuntimeState.FadingIn;
            }
        }

        public void Load(string path, bool async, bool inPool = false)
        {
            if (m_audioAgentRuntimeState != AudioAgentRuntimeState.None && m_audioAgentRuntimeState != AudioAgentRuntimeState.End)
            {
                m_preLoadRequest = LoadRequest.Spawn(path, async, inPool, default, false);
                if (m_audioAgentRuntimeState == AudioAgentRuntimeState.Playing || m_audioAgentRuntimeState == AudioAgentRuntimeState.FadingIn) BeginFadeOut();
                return;
            }
            m_activeOptions = default;
            LoadResource(path, async, inPool);
        }

        private void LoadResource(string path, bool async, bool inPool = false)
        {
            if (m_audioAgentRuntimeState == AudioAgentRuntimeState.None ||
                m_audioAgentRuntimeState == AudioAgentRuntimeState.End)
            {
                m_path = path;
                m_inPool = inPool;
                m_isPaused = false;
                m_duration = 0;
                if (!string.IsNullOrEmpty(path))
                {
                    if (inPool && m_audioModule.TryGetAssetHandle(path, out var handle))
                    {
                        m_loadingHandle = handle;
                        OnAssetLoadComplete(handle);
                        return;
                    }

                    if (async)
                    {
                        m_audioAgentRuntimeState = AudioAgentRuntimeState.Loading;
                        handle = m_resourceModule.LoadAssetAsyncHandle<AudioClip>(path);
                        m_loadingHandle = handle;
                        handle.Completed += OnAssetLoadComplete;
                    }
                    else
                    {
                        handle = m_resourceModule.LoadAssetSyncHandle<AudioClip>(path);
                        m_loadingHandle = handle;
                        OnAssetLoadComplete(handle);
                    }
                }
            }
        }

        internal void Load(string path, AudioPlayOptions options, bool async, bool inPool = false)
        {
            if (m_audioAgentRuntimeState == AudioAgentRuntimeState.None || m_audioAgentRuntimeState == AudioAgentRuntimeState.End)
            {
                m_path = path;
                m_inPool = inPool;
                m_activeOptions = options;
                m_requestAsync = async;
                m_requestInPool = inPool;
                BeginLoadOptions(path, options, async, inPool);
            }
            else
            {
                m_preLoadRequest = LoadRequest.Spawn(path, async, inPool, options, true);
                if (m_audioAgentRuntimeState == AudioAgentRuntimeState.Playing || m_audioAgentRuntimeState == AudioAgentRuntimeState.FadingIn) BeginFadeOut();
            }
        }

        private void BeginLoadOptions(string path, AudioPlayOptions options, bool async, bool inPool)
        {
            m_volume = options.Volume * UnityEngine.Random.Range(options.VolumeRange.x, options.VolumeRange.y);
            Pitch = UnityEngine.Random.Range(options.PitchRange.x, options.PitchRange.y);
            m_fadeInDuration = options.FadeInTime;
            m_fadeOutDuration = options.FadeOutTime;
            m_endPauseTime = options.EndPauseTime;
            m_isLoop = options.IsLoop;
            m_audioSource.loop = m_isLoop && m_endPauseTime <= 0f && !options.UseRandomSelection;
            SpatialBlend = options.SpatialBlend;
            MinDistance = options.MinDistance;
            MaxDistance = options.MaxDistance;
            RolloffMode = options.RolloffMode;
            Priority = options.Priority;
            Position = options.Position;
            LoadResource(path, async, inPool);
        }

        /// <summary>
        /// 停止播放音频代理辅助器
        /// </summary>
        /// <param name="fadeOut">是否渐出</param>
        public void Stop(bool fadeOut = false)
        {
            ReleasePendingRequest();
            m_isPaused = false;
            if (m_loadingHandle != null)
            {
                m_loadingHandle.Completed -= OnAssetLoadComplete;
                m_loadingHandle.Dispose();
                m_loadingHandle = null;
            }
            if (m_audioSource != null)
            {
                if (fadeOut)
                {
                    BeginFadeOut();
                }
                else
                {
                    ResetPlayback();
                    m_activeOptions = default;
                }
            }
        }

        private void BeginFadeOut()
        {
            if (m_audioSource == null || !m_audioSource.isPlaying || m_fadeOutDuration <= 0f)
            {
                CompleteAndPlayPending();
                return;
            }

            m_fadeOutTime = m_fadeOutDuration;
            m_fadeOutStartVolume = m_audioSource.volume;
            m_audioAgentRuntimeState = AudioAgentRuntimeState.FadingOut;
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            if (m_audioSource != null) { m_isPaused = true; m_audioSource.Pause(); }
        }

        /// <summary>
        /// 取消暂停播放
        /// </summary>
        public void UnPause()
        {
            if (m_audioSource != null) { m_isPaused = false; m_audioSource.UnPause(); }
        }

        /// <summary>
        /// 资源加载完成回调
        /// </summary>
        /// <param name="handle">资源句柄</param>
        private void OnAssetLoadComplete(AssetHandle handle)
        {
            if (m_destroyed || handle == null || !ReferenceEquals(handle, m_loadingHandle)) return;
            m_loadingHandle = null;
            bool pooled = false;
            if (m_inPool)
            {
                if (m_audioModule.TryGetAssetHandle(m_path, out var cachedHandle) && ReferenceEquals(cachedHandle, handle))
                {
                    pooled = true;
                }
                else
                {
                    pooled = m_audioModule.TryAddAssetHandle(m_path, handle);
                }
            }

            if (m_preLoadRequest != null)
            {
                if (!pooled)
                {
                    handle.Dispose();
                }
                m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
                var request = TakePendingRequest();
                if (request.hasOptions) Load(request.path, request.options, request.async, request.inPool);
                else Load(request.path, request.async, request.inPool);
                MemoryObject.Release(request);
            }
            else if(handle != null)
            {
                AudioData.Release(m_audioData);
                m_audioData = AudioData.Spawn(handle, pooled);
                m_audioSource.clip = handle.AssetObject as AudioClip;

                if (m_audioSource.clip != null)
                {
                    m_audioSource.volume = m_fadeInDuration > 0f ? 0f : m_volume;
                    m_audioSource.Play();
                    if (m_isPaused) m_audioSource.Pause();
                    m_fadeInTime = 0f;
                    m_audioAgentRuntimeState = m_fadeInDuration > 0f ? AudioAgentRuntimeState.FadingIn : AudioAgentRuntimeState.Playing;
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
            if (m_audioSource == null || m_isPaused)
            {
                return;
            }
            switch (m_audioAgentRuntimeState)
            {
                case AudioAgentRuntimeState.Playing:
                    if (!m_audioSource.isPlaying)
                    {
                        if (m_isLoop && m_activeOptions.UseRandomSelection && m_endPauseTime <= 0f)
                        {
                            RestartRandomClip();
                            break;
                        }
                        m_endPauseRemaining = m_endPauseTime;
                        m_audioAgentRuntimeState = m_endPauseRemaining > 0 ? AudioAgentRuntimeState.EndPause : AudioAgentRuntimeState.End;
                    }
                    break;

                case AudioAgentRuntimeState.FadingOut:
                    m_fadeOutTime = Mathf.Max(0f, m_fadeOutTime - elapseSeconds);
                    if (m_fadeOutTime <= 0f)
                    {
                        var request = TakePendingRequest();
                        ResetPlayback();
                        m_activeOptions = default;
                        if (request != null)
                        {
                            if (request.hasOptions) Load(request.path, request.options, request.async, request.inPool);
                            else Load(request.path, request.async, request.inPool);
                            MemoryObject.Release(request);
                        }
                    }
                    else
                    {
                        m_audioSource.volume = m_fadeOutStartVolume * m_fadeOutTime / m_fadeOutDuration;
                    }
                    break;
                case AudioAgentRuntimeState.FadingIn:
                    m_fadeInTime = Mathf.Min(m_fadeInDuration, m_fadeInTime + elapseSeconds);
                    m_audioSource.volume = m_fadeInDuration > 0f ? m_volume * m_fadeInTime / m_fadeInDuration : m_volume;
                    if (m_fadeInTime >= m_fadeInDuration)
                    {
                        m_audioAgentRuntimeState = AudioAgentRuntimeState.Playing;
                        m_audioSource.volume = m_volume;
                        if (m_preLoadRequest != null)
                        {
                            var request = TakePendingRequest();
                            if (request.hasOptions) Load(request.path, request.options, request.async, request.inPool);
                            else Load(request.path, request.async, request.inPool);
                            MemoryObject.Release(request);
                        }
                    }
                    break;
                case AudioAgentRuntimeState.EndPause:
                    m_endPauseRemaining -= elapseSeconds;
                    if (m_endPauseRemaining <= 0f)
                    {
                        if (m_isLoop && m_activeOptions.UseRandomSelection)
                        {
                            RestartRandomClip();
                        }
                        else if (m_isLoop && m_audioSource.clip != null)
                        {
                            m_audioSource.loop = false;
                            m_audioSource.Play();
                            m_audioAgentRuntimeState = AudioAgentRuntimeState.Playing;
                        }
                        else
                        {
                            m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
                        }
                    }
                    break;
            }

            m_duration += elapseSeconds;
        }

        private void ResetPlayback()
        {
            m_audioSource?.Stop();
            if (m_audioSource != null) m_audioSource.clip = null;
            AudioData.Release(m_audioData);
            m_audioData = null;
            m_audioAgentRuntimeState = AudioAgentRuntimeState.End;
            m_isPaused = false;
            m_fadeInTime = 0f;
            m_fadeOutTime = 0f;
        }

        private LoadRequest TakePendingRequest()
        {
            var request = m_preLoadRequest;
            m_preLoadRequest = null;
            return request;
        }

        private void ReleasePendingRequest()
        {
            if (m_preLoadRequest != null)
            {
                MemoryObject.Release(m_preLoadRequest);
                m_preLoadRequest = null;
            }
        }

        private void CompleteAndPlayPending()
        {
            var request = TakePendingRequest();
            ResetPlayback();
            m_activeOptions = default;
            if (request != null)
            {
                if (request.hasOptions) Load(request.path, request.options, request.async, request.inPool);
                else Load(request.path, request.async, request.inPool);
                MemoryObject.Release(request);
            }
        }

        private void RestartRandomClip()
        {
            var options = m_activeOptions;
            ResetPlayback();
            m_activeOptions = options;
            m_audioGroupCategory?.TryReplayRandom(this);
        }

        /// <summary>
        /// 销毁音频代理器
        /// </summary>
        public void Destroy()
        {
            Stop();
            m_destroyed = true;
            if (m_transform != null)
            {
                Object.Destroy(m_transform.gameObject);
            }
        }
    }
}
