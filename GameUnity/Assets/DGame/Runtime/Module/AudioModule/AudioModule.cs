using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using YooAsset;

namespace DGame
{
    internal sealed class AudioModule : Module, IAudioModule, IUpdateModule
    {
        public const string MUSIC_VOLUME_NAME = "MusicVolume";
        public const string SOUND_VOLUME_NAME = "SoundVolume";
        public const string UI_SOUND_VOLUME_NAME = "UISoundVolume";
        public const string VOICE_VOLUME_NAME = "VoiceVolume";

        private AudioMixer m_audioMixer;
        private Transform m_instanceRoot = null;
        private AudioGroupConfig[] m_audioGroupCfgs = null;
        private IResourceModule m_resourceModule;

        private float m_volume = 1.0f;
        private bool m_enable = true;
        private readonly AudioGroupCategory[] m_audioGroupCategories = new AudioGroupCategory[(int)AudioType.Max];
        private readonly float[] m_categoriesVolume = new float[(int)AudioType.Max];
        private bool m_unityAudioDisabled = false;

        public AudioMixer AudioMixer => m_audioMixer;
        public Transform InstanceRoot => m_instanceRoot;
        public Dictionary<string, AssetHandle> AudioClipPool { get; set; } = new Dictionary<string, AssetHandle>();

        public float Volume
        {
            get => m_unityAudioDisabled ? 0.0f : m_volume;
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }
                m_volume = value;
                AudioListener.volume = m_volume;
            }
        }

        public bool Enabled
        {
            get => !m_unityAudioDisabled && m_enable;
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }
                m_enable = value;
                AudioListener.volume = m_enable ? m_volume : 0.0f;
            }
        }

        public float MusicVolume
        {
            get => m_unityAudioDisabled ? 0.0f : m_categoriesVolume[(int)AudioType.Music];
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }
                float volume = Mathf.Clamp(value, 0.0001f, 1.0f);
                m_categoriesVolume[(int)AudioType.Music] = volume;
                AudioMixer.SetFloat(MUSIC_VOLUME_NAME, Mathf.Log10(volume) * 20f);
            }
        }

        public float SoundVolume
        {
            get => m_unityAudioDisabled ? 0.0f : m_categoriesVolume[(int)AudioType.Sound];
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }
                float volume = Mathf.Clamp(value, 0.0001f, 1.0f);
                m_categoriesVolume[(int)AudioType.Sound] = volume;
                AudioMixer.SetFloat(SOUND_VOLUME_NAME, Mathf.Log10(volume) * 20f);
            }
        }

        public float UISoundVolume
        {
            get => m_unityAudioDisabled ? 0.0f : m_categoriesVolume[(int)AudioType.UISound];
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }
                float volume = Mathf.Clamp(value, 0.0001f, 1.0f);
                m_categoriesVolume[(int)AudioType.UISound] = volume;
                AudioMixer.SetFloat(UI_SOUND_VOLUME_NAME, Mathf.Log10(volume) * 20f);
            }
        }

        public float VoiceVolume
        {
            get => m_unityAudioDisabled ? 0.0f : m_categoriesVolume[(int)AudioType.Voice];
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }
                float volume = Mathf.Clamp(value, 0.0001f, 1.0f);
                m_categoriesVolume[(int)AudioType.Voice] = volume;
                AudioMixer.SetFloat(VOICE_VOLUME_NAME, Mathf.Log10(volume) * 20f);
            }
        }

        public bool MusicEnable
        {
            get => !m_unityAudioDisabled && AudioMixer.GetFloat(MUSIC_VOLUME_NAME, out var db) && db > -80f;
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }

                if (m_audioGroupCategories[(int)AudioType.Music] != null)
                {
                    m_audioGroupCategories[(int)AudioType.Music].Enable = value;
                }

                AudioMixer.SetFloat(MUSIC_VOLUME_NAME, value ? Mathf.Log10(m_categoriesVolume[(int)AudioType.Music]) * 20f : -80f);
            }
        }

        public bool SoundEnable
        {
            get => !m_unityAudioDisabled && AudioMixer.GetFloat(SOUND_VOLUME_NAME, out var db) && db > -80f;
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }

                if (m_audioGroupCategories[(int)AudioType.Sound] != null)
                {
                    m_audioGroupCategories[(int)AudioType.Sound].Enable = value;
                }
                AudioMixer.SetFloat(SOUND_VOLUME_NAME, value ? Mathf.Log10(m_categoriesVolume[(int)AudioType.Sound]) * 20f : -80f);
            }
        }

        public bool UISoundEnable
        {
            get => !m_unityAudioDisabled && AudioMixer.GetFloat(UI_SOUND_VOLUME_NAME, out var db) && db > -80f;
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }

                if (m_audioGroupCategories[(int)AudioType.UISound] != null)
                {
                    m_audioGroupCategories[(int)AudioType.UISound].Enable = value;
                }
                AudioMixer.SetFloat(UI_SOUND_VOLUME_NAME, value ? Mathf.Log10(m_categoriesVolume[(int)AudioType.UISound]) * 20f : -80f);
            }
        }

        public bool VoiceEnable
        {
            get => !m_unityAudioDisabled && AudioMixer.GetFloat(VOICE_VOLUME_NAME, out var db) && db > -80f;
            set
            {
                if (m_unityAudioDisabled)
                {
                    return;
                }

                if (m_audioGroupCategories[(int)AudioType.Voice] != null)
                {
                    m_audioGroupCategories[(int)AudioType.Voice].Enable = value;
                }
                AudioMixer.SetFloat(VOICE_VOLUME_NAME, value ? Mathf.Log10(m_categoriesVolume[(int)AudioType.Voice]) * 20f : -80f);
            }
        }

        public override void OnCreate()
        {
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            Initialize(Settings.AudioSetting.audioGroupConfigs);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            for (int i = 0; i < m_audioGroupCategories.Length; i++)
            {
                m_audioGroupCategories[i]?.Update(elapseSeconds);
            }
        }

        public override void OnDestroy()
        {
            StopAll(false);
            ClearSoundPool();
        }

        public bool TryGetAssetHandle(string path, out AssetHandle handle)
        {
            return AudioClipPool.TryGetValue(path, out handle);
        }

        public bool TryAddAssetHandle(string path, AssetHandle handle)
        {
            return AudioClipPool.TryAdd(path, handle);
        }

        public void Initialize(AudioGroupConfig[] audioGroupConfigs, Transform instanceRoot = null,
            AudioMixer audioMixer = null)
        {
            if (m_instanceRoot == null)
            {
                m_instanceRoot = instanceRoot;
            }

            if (audioGroupConfigs == null)
            {
                throw new DGameException("音频轨道配置文件数据为空");
            }

            m_audioGroupCfgs = audioGroupConfigs;

            if (m_instanceRoot == null)
            {
                m_instanceRoot = new GameObject("[AudioModule Instance Root]").transform;
                m_instanceRoot.localScale = Vector3.one;
                UnityEngine.Object.DontDestroyOnLoad(m_instanceRoot);
            }

#if UNITY_EDITOR

            try
            {
                TypeInfo typeInfo = typeof(AudioSettings).GetTypeInfo();
                PropertyInfo propertyInfo = typeInfo.GetDeclaredProperty("unityAudioDisabled");
                m_unityAudioDisabled = (bool)propertyInfo.GetValue(null);

                if (m_unityAudioDisabled)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                DLogger.Error(e.ToString());
            }

#endif

            if (audioMixer != null)
            {
                m_audioMixer = audioMixer;
            }

            if (m_audioMixer == null)
            {
                m_audioMixer = Resources.Load<AudioMixer>("AudioMixer");
            }

            for (int i = 0; i < (int)AudioType.Max && i < m_audioGroupCfgs.Length; i++)
            {
                AudioType audioType = (AudioType)i;
                AudioGroupConfig audioGroupConfig = m_audioGroupCfgs[(int)audioType];
                m_audioGroupCategories[i] = new AudioGroupCategory(audioGroupConfig.AgentHelperCount, AudioMixer, audioGroupConfig);

                switch (audioType)
                {
                    case AudioType.Music:
                        MusicVolume = audioGroupConfig.Volume;
                        MusicEnable = !audioGroupConfig.Mute;
                        break;

                    case AudioType.Sound:
                        SoundVolume = audioGroupConfig.Volume;
                        SoundEnable = !audioGroupConfig.Mute;
                        break;

                    case AudioType.UISound:
                        UISoundVolume = audioGroupConfig.Volume;
                        UISoundEnable = !audioGroupConfig.Mute;
                        break;

                    case AudioType.Voice:
                        VoiceVolume = audioGroupConfig.Volume;
                        VoiceEnable = !audioGroupConfig.Mute;
                        break;
                }
            }
        }

        public void Restart()
        {
            if (m_unityAudioDisabled)
            {
                return;
            }
            ClearSoundPool();

            for (int i = 0; i < (int)AudioType.Max && i < m_audioGroupCategories.Length; i++)
            {
                var category = m_audioGroupCategories[i];

                if (category != null)
                {
                    for (int j = 0; j < category.audioAgents.Count; j++)
                    {
                        var agent = category.audioAgents[j];
                        agent?.Destroy();
                    }
                }
            }

            Initialize(Settings.AudioSetting.audioGroupConfigs);
        }

        public AudioSourceAgent Play(AudioType audioType, string path, bool isLoop = false, float volume = 1, bool isAsync = false,
            bool isInPool = false)
        {
            if (m_unityAudioDisabled || m_audioGroupCategories == null || (int)audioType >= m_audioGroupCategories.Length)
            {
                return null;
            }

            var audioSourceAgent = m_audioGroupCategories[(int)audioType]?.Play(path, isAsync, isInPool);
            if (audioSourceAgent != null)
            {
                audioSourceAgent.IsLoop = isLoop;
                audioSourceAgent.Volume = volume;
            }
            return audioSourceAgent;
        }

        public void Stop(AudioType audioType, bool fadeout)
        {
            if (m_unityAudioDisabled || m_audioGroupCategories == null || (int)audioType >= m_audioGroupCategories.Length)
            {
                return;
            }
            m_audioGroupCategories[(int)audioType]?.Stop(fadeout);
        }

        public void StopAll(bool fadeout)
        {
            if (m_unityAudioDisabled || m_audioGroupCategories == null)
            {
                return;
            }

            for (int i = 0; i < (int)AudioType.Max && i < m_audioGroupCategories.Length; i++)
            {
                m_audioGroupCategories[i]?.Stop(fadeout);
            }
        }

        public void PreLoadPutInAudioPool(List<string> list)
        {
            if (m_unityAudioDisabled || AudioClipPool == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var path = list[i];
                if (!AudioClipPool.ContainsKey(path))
                {
                    var handle = m_resourceModule.LoadAssetAsyncHandle<AudioClip>(path);
                    handle.Completed += OnAssetHandleCompleted;
                }
            }
        }

        private void OnAssetHandleCompleted(AssetHandle handle)
        {
            AudioClipPool?.TryAdd(handle.GetAssetInfo().Address, handle);
        }

        public void RemoveClipFromAudioPool(List<string> list)
        {
            if (m_unityAudioDisabled || AudioClipPool == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var path = list[i];


                if (AudioClipPool.TryGetValue(path, out var audioHandle))
                {
                    audioHandle?.Dispose();
                    AudioClipPool.Remove(path);
                }
            }
        }

        public void ClearSoundPool()
        {
            if (m_unityAudioDisabled || AudioClipPool == null)
            {
                return;
            }

            foreach (var handle in AudioClipPool.Values)
            {
                handle?.Dispose();
            }
            AudioClipPool.Clear();
        }
    }
}