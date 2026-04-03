using GameProto;

namespace GameLogic
{
    public class MonsterConfigMgr : Singleton<MonsterConfigMgr>
    {
        public MonsterConfig GetModelOrDefault(int modelID) => TbMonsterConfig.GetOrDefault(modelID);

        public bool TryGetModelCfg(int modelID, out MonsterConfig cfg) => TbMonsterConfig.TryGetValue(modelID, out cfg);

        public bool ContainsModelId(int modelID) => TbMonsterConfig.ContainsKey(modelID);
    }
}