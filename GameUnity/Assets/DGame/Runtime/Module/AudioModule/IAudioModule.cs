using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using YooAsset;

namespace DGame
{
    public interface IAudioModule
    {
        /// <summary>
        /// 总音量控制
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// 总开关
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// 背景音乐音量
        /// </summary>
        float MusicVolume { get; set; }

        /// <summary>
        /// 音效音量
        /// </summary>
        float SoundVolume { get; set; }

        /// <summary>
        /// UI音效音量
        /// </summary>
        float UISoundVolume { get; set; }

        /// <summary>
        /// 人声音量
        /// </summary>
        float VoiceVolume { get; set; }

        /// <summary>
        /// 背景音乐开关
        /// </summary>
        bool MusicEnable { get; set; }

        /// <summary>
        /// 音效开关
        /// </summary>
        bool SoundEnable { get; set; }

        /// <summary>
        /// UI音效开关
        /// </summary>
        bool UISoundEnable { get; set; }

        /// <summary>
        /// 人声开关
        /// </summary>
        bool VoiceEnable { get; set; }

        /// <summary>
        /// 音频混响器
        /// </summary>
        AudioMixer AudioMixer { get; }

        /// <summary>
        /// 实例化根节点
        /// </summary>
        Transform InstanceRoot { get; }

        /// <summary>
        /// 音频资源句柄对象池 key->路径 value->资源句柄
        /// </summary>
        Dictionary<string, AssetHandle> AudioClipPool { get; set; }

        bool TryGetAssetHandle(string path, out AssetHandle handle);

        bool TryAddAssetHandle(string path, AssetHandle handle);

        /// <summary>
        /// 初始化音频模块
        /// </summary>
        /// <param name="audioGroupConfigs">音频轨道组配置文件</param>
        /// <param name="instanceRoot">实例化根节点</param>
        /// <param name="audioMixer">音频混响器</param>
        void Initialize(AudioGroupConfig[] audioGroupConfigs, Transform instanceRoot = null, AudioMixer audioMixer = null);

        /// <summary>
        /// 重启音频模块
        /// </summary>
        void Restart();

        /// <summary>
        /// 播放音频接口
        /// </summary>
        /// <remarks>如果超过最大发声数量，采用fadeout的方式服用最久播放的AudioSource</remarks>
        /// <param name="audioType">声音类型</param>
        /// <param name="path">声音文件路径</param>
        /// <param name="isLoop">是否循环</param>
        /// <param name="volume">音量</param>
        /// <param name="isAsync">是否异步</param>
        /// <param name="isInPool">是否进池</param>
        /// <returns></returns>
        AudioSourceAgent Play(AudioType audioType, string path, bool isLoop = false, float volume = 1.0f, bool isAsync = false, bool isInPool = false);

        /// <summary>
        /// 停止某类声音播放
        /// </summary>
        /// <param name="audioType">声音类型</param>
        /// <param name="fadeout">是否渐出</param>
        void Stop(AudioType audioType, bool fadeout);

        /// <summary>
        /// 停止播放所有声音
        /// </summary>
        /// <param name="fadeout">是否渐出</param>
        void StopAll(bool fadeout);

        /// <summary>
        /// 预先加载AudioClip 并放入对象池
        /// </summary>
        /// <param name="list"></param>
        void PreLoadPutInAudioPool(List<string> list);

        /// <summary>
        /// 将部分AudioClip从对象池移出。
        /// </summary>
        /// <param name="list">AudioClip的AssetPath集合。</param>
        void RemoveClipFromAudioPool(List<string> list);

        /// <summary>
        /// 清空AudioClip对象池
        /// </summary>
        void ClearSoundPool();
    }
}