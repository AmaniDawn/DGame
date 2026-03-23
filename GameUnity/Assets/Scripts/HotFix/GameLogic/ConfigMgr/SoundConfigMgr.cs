using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
    public class SoundConfigMgr : Singleton<SoundConfigMgr>
    {
        public Dictionary<int, SoundConfig> DataMap => TbSoundConfig.DataMap;

        public List<SoundConfig> DataLis => TbSoundConfig.DataList;

        public bool TryGetValue(int soundId, out SoundConfig cfg)
            => TbSoundConfig.TryGetValue(soundId, out cfg);

        public SoundConfig GetOrDefault(int soundId) => TbSoundConfig.GetOrDefault(soundId);

        public bool ContainsKey(int soundId) => TbSoundConfig.ContainsKey(soundId);
    }
}