using UnityEngine;

namespace DGame
{
    public class AnimInfo
    {
        private readonly AnimClip m_animClip;

        private AnimInfo(){ }

        internal AnimInfo(AnimClip animClip) => m_animClip = animClip;

        /// <summary>
        /// 动画名字
        /// </summary>
        public string Name => m_animClip.Name;

        /// <summary>
        /// 动画的哈希值
        /// </summary>
        public int AnimHashCode => m_animClip.AnimHashCode;

        /// <summary>
        /// 动画长度
        /// </summary>
        public float Length => m_animClip.ClipLength;

        /// <summary>
        /// 动画层级
        /// </summary>
        public int Layer => m_animClip.Layer;

        /// <summary>
        /// 动画播放模式
        /// </summary>
        public WrapMode WrapMode => m_animClip.WrapMode;

        /// <summary>
        /// 动画权重
        /// </summary>
        public float Weight
        {
            get => m_animClip.Weight;
            set => m_animClip.Weight = value;
        }

        /// <summary>
        /// 动画时间轴
        /// </summary>
        public float Time
        {
            get => m_animClip.Time;
            set => m_animClip.Time = value;
        }

        /// <summary>
        /// 归一化时间轴
        /// </summary>
        public float NormalizedTime
        {
            get => m_animClip.NormalizedTime;
            set => m_animClip.NormalizedTime = value;
        }

        /// <summary>
        /// 播放速度
        /// </summary>
        public float Speed
        {
            get => m_animClip.Speed;
            set => m_animClip.Speed = value;
        }
    }
}