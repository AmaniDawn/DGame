using GameProto;

namespace GameLogic
{
    public class EffectConfigMgr : Singleton<EffectConfigMgr>
    {
        public bool TryGetValue(int effectId, out EffectConfig config)
            => TbEffectConfig.TryGetValue(effectId, out config);
    }
}