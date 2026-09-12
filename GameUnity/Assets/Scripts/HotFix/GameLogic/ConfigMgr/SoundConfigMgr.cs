using System.Collections.Generic;
using GameProto;
using UnityEngine;

namespace GameLogic
{
    public class SoundConfigMgr : Singleton<SoundConfigMgr>
    {
        private sealed class SoundRandomData { public string[] Locations; public float[] Weights; }
        private readonly Dictionary<int, SoundRandomData> m_soundRandomDataCache = new Dictionary<int, SoundRandomData>();

        public Dictionary<int, SoundConfig> DataMap => TbSoundConfig.DataMap;
        public List<SoundConfig> DataLis => TbSoundConfig.DataList;
        public bool TryGetValue(int soundId, out SoundConfig cfg) => TbSoundConfig.TryGetValue(soundId, out cfg);
        public SoundConfig GetOrDefault(int soundId) => TbSoundConfig.GetOrDefault(soundId);
        public bool ContainsKey(int soundId) => TbSoundConfig.ContainsKey(soundId);

        public DGame.AudioSourceAgent Play(int soundId, DGame.AudioType audioType, bool isAsync = true, bool isInPool = true, Vector3 position = default)
        {
            if (!TryGetValue(soundId, out var cfg) || string.IsNullOrEmpty(cfg.Location)) return null;
            var soundRandomData = GetSoundRandomData(soundId, cfg.Location);
            var options = DGame.AudioPlayOptions.CreateDefault();
            options.SoundId = cfg.ID;
            options.PlaybackMode = (DGame.AudioPlaybackMode)cfg.PlayMode;
            options.UsePlaybackMode = true;
            options.VolumeRange = new Vector2(cfg.VolumeMin, cfg.VolumeMax);
            options.PitchRange = new Vector2(cfg.PitchMin, cfg.PitchMax);
            options.FadeInTime = cfg.FadeInTime;
            options.FadeOutTime = cfg.FadeOutTime;
            options.EndPauseTime = cfg.EndPauseTime;
            options.SpatialBlend = cfg.SpatialBlend;
            options.MinDistance = cfg.MinDistance;
            options.MaxDistance = cfg.MaxDistance;
            options.Priority = cfg.Priority;
            options.MaxPlayCount = cfg.MaxPlayCount;
            options.Position = position;
            options.UseRandomSelection = soundRandomData.Locations.Length > 1 || soundRandomData.Locations[0] != cfg.Location;
            options.RandomLocations = soundRandomData.Locations;
            options.RandomWeights = soundRandomData.Weights;
            return GameModule.AudioModule.Play(audioType, SelectSoundRandomLocation(soundRandomData), options, isAsync, isInPool);
        }

        private SoundRandomData GetSoundRandomData(int soundId, string fallback)
        {
            if (m_soundRandomDataCache.TryGetValue(soundId, out var cached)) return cached;
            var rows = TbSoundRandomConfig.GetListBySoundID(soundId);
            var locations = new List<string>();
            var weights = new List<float>();
            if (rows != null) for (int i = 0; i < rows.Count; i++)
                if (rows[i].Weight > 0f && !string.IsNullOrEmpty(rows[i].Location)) { locations.Add(rows[i].Location); weights.Add(rows[i].Weight); }
            if (locations.Count == 0) { locations.Add(fallback); weights.Add(1f); }
            cached = new SoundRandomData { Locations = locations.ToArray(), Weights = weights.ToArray() };
            m_soundRandomDataCache[soundId] = cached;
            return cached;
        }

        private static string SelectSoundRandomLocation(SoundRandomData snapshot)
        {
            float total = 0f; for (int i = 0; i < snapshot.Weights.Length; i++) total += snapshot.Weights[i];
            float roll = Random.value * total;
            for (int i = 0; i < snapshot.Locations.Length; i++) { roll -= snapshot.Weights[i]; if (roll <= 0f) return snapshot.Locations[i]; }
            return snapshot.Locations[snapshot.Locations.Length - 1];
        }
    }
}
