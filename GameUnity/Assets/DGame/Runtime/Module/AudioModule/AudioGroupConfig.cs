using System;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 音频轨道组配置
    /// </summary>
    [Serializable]
    public sealed class AudioGroupConfig
    {
        /// <summary>
        /// 音频分类 用于分别开启/关闭对应分类音频
        /// </summary>
        /// <remarks>和AudioMixer中分类保持一致</remarks>
        [Tooltip("音频类型")]
        public AudioType audioType;

        [SerializeField, Tooltip("是否静音")]
        private bool mute = false;

        [SerializeField, Range(0f, 1f), Tooltip("音量")]
        private float volume = 1.0f;

        [SerializeField, Tooltip("音频代理数量"), Range(1, 4)]
        private int agentHelperCount = 1;

        /// <summary>
        /// 音频源中3D声音的衰减模式
        /// <remarks>Logarithmic - 当你想要实现现实世界的衰减时使用此模式</remarks>
        /// <remarks>Linear - 当你想要随距离降低声音的音量时使用此模式</remarks>
        /// <remarks>Custom -当你想要使用自定义衰减时使用此模式</remarks>
        /// </summary>
        [Tooltip("音频源中3D声音的衰减模式")]
        public AudioRolloffMode audioRolloffMode = AudioRolloffMode.Logarithmic;

        /// <summary>
        /// 最小距离
        /// </summary>
        [Tooltip("最小距离"), Range(0f, 800f)]
        public float minDistance = 1.0f;

        /// <summary>
        /// 最大距离
        /// </summary>
        [Tooltip("最大距离"), Range(0f, 800f)]
        public float maxDistance = 500.0f;

        /// <summary>
        /// 音频轨道组配置的名称
        /// </summary>
        public string Name => audioType.ToString();

        /// <summary>
        /// 是否禁静音
        /// </summary>
        public bool Mute => mute;

        /// <summary>
        /// 音量
        /// </summary>
        public float Volume => volume;

        /// <summary>
        /// 音频代理数量
        /// <remarks>命名与AudioMixer中个数保持一致</remarks>
        /// </summary>
        public int AgentHelperCount => agentHelperCount;
    }
}