using UnityEngine;

namespace DGame
{
    public enum AudioPlaybackMode : byte
    {
        Once = 0,
        Loop = 1,
        LoopWithInterval = 2
    }

    /// <summary>单次音频播放参数。使用 CreateDefault 构造后覆盖输入，合法零值不作为缺省值。</summary>
    public struct AudioPlayOptions
    {
        public int SoundId;
        public bool IsLoop;
        public AudioPlaybackMode PlaybackMode;
        public bool UsePlaybackMode;
        public float Volume;
        public Vector2 VolumeRange;
        public Vector2 PitchRange;
        public float FadeInTime;
        public float FadeOutTime;
        public float EndPauseTime;
        public float SpatialBlend;
        public float MinDistance;
        public float MaxDistance;
        public AudioRolloffMode RolloffMode;
        public Vector3 Position;
        public int Priority;
        public int MaxPlayCount;
        public bool IsConfigured;
        public bool UseRandomSelection;
        public string[] RandomLocations;
        public float[] RandomWeights;
        internal bool AllowEqualPriority;

        /// <summary>创建完整的播放默认值；值类型构造不会产生托管对象分配。</summary>
        public static AudioPlayOptions CreateDefault()
        {
            return new AudioPlayOptions
            {
                IsConfigured = true,
                Volume = 1f,
                VolumeRange = Vector2.one,
                PitchRange = Vector2.one,
                FadeOutTime = 0.2f,
                MinDistance = 1f,
                MaxDistance = 500f,
                RolloffMode = AudioRolloffMode.Logarithmic,
                Priority = 128
            };
        }

        internal AudioPlayOptions Normalize(AudioGroupConfig group)
        {
            var copy = this;
            if (!IsConfigured)
            {
                copy.Volume = 1f;
                copy.VolumeRange = Vector2.one;
                copy.PitchRange = Vector2.one;
                copy.FadeOutTime = 0.2f;
                copy.MinDistance = group.minDistance;
                copy.MaxDistance = group.maxDistance;
                copy.Priority = 128;
            }
            else
            {
                copy.Volume = Mathf.Clamp(Volume, 0f, 1f);
                copy.VolumeRange = NormalizeRange(VolumeRange, 0f, 1f, 0f);
                copy.PitchRange = NormalizeRange(PitchRange, 0.01f, 3f, 1f);
                copy.FadeOutTime = Mathf.Max(0f, FadeOutTime);
                copy.MinDistance = Mathf.Max(0f, MinDistance);
                copy.MaxDistance = Mathf.Max(copy.MinDistance, MaxDistance);
                copy.Priority = Mathf.Clamp(Priority, 0, 256);
            }
            copy.FadeInTime = Mathf.Max(0f, FadeInTime);
            copy.EndPauseTime = Mathf.Max(0f, EndPauseTime);
            if (UsePlaybackMode)
            {
                copy.IsLoop = PlaybackMode != AudioPlaybackMode.Once;
                if (PlaybackMode == AudioPlaybackMode.Loop) copy.EndPauseTime = 0f;
            }
            copy.SpatialBlend = Mathf.Clamp01(SpatialBlend);
            if (!IsConfigured)
            {
                copy.MinDistance = group.minDistance;
                copy.MaxDistance = group.maxDistance;
            }
            copy.RolloffMode = RolloffMode;
            copy.MaxPlayCount = Mathf.Max(0, MaxPlayCount);
            return copy;
        }

        private static Vector2 NormalizeRange(Vector2 value, float min, float max, float fallback)
        {
            float x = value.x <= 0f || float.IsNaN(value.x) || float.IsInfinity(value.x) ? fallback : Mathf.Clamp(value.x, min, max);
            float y = value.y <= 0f || float.IsNaN(value.y) || float.IsInfinity(value.y) ? fallback : Mathf.Clamp(value.y, min, max);
            return new Vector2(Mathf.Min(x, y), Mathf.Max(x, y));
        }
    }
}
