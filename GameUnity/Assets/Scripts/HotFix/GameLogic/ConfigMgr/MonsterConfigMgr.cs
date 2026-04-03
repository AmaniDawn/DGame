using GameProto;

namespace GameLogic
{
    public class MonsterConfigMgr : Singleton<MonsterConfigMgr>
    {
        public MonsterConfig GetOrDefault(int monsterID) => TbMonsterConfig.GetOrDefault(monsterID);

        public bool TryGetMonsterConfig(int monsterID, out MonsterConfig cfg) => TbMonsterConfig.TryGetValue(monsterID, out cfg);

        public bool ContainsMonsterId(int monsterID) => TbMonsterConfig.ContainsKey(monsterID);
    }
}