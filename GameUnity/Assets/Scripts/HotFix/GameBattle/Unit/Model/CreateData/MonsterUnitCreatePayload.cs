namespace GameBattle
{
    /// <summary>
    /// 怪物逻辑单位专属创建数据。
    /// </summary>
    public sealed class MonsterUnitCreatePayload : LogicUnitCreatePayload
    {
        /// <summary>
        /// 生成组 ID。
        /// </summary>
        public int SpawnGroupId { get; private set; }

        /// <summary>
        /// 是否精英怪。
        /// </summary>
        public bool IsElite { get; private set; }

        public override UnitType UnitType => UnitType.Monster;

        /// <summary>
        /// 创建怪物专属创建数据。
        /// </summary>
        public static MonsterUnitCreatePayload Create(int spawnGroupId = 0, bool isElite = false)
        {
            var payload = Spawn<MonsterUnitCreatePayload>();
            payload.SpawnGroupId = spawnGroupId;
            payload.IsElite = isElite;
            return payload;
        }

        public override void OnRelease()
        {
            SpawnGroupId = 0;
            IsElite = false;
        }
    }
}
