namespace GameBattle
{
    /// <summary>
    /// 怪物逻辑单位。
    /// </summary>
    public sealed class MonsterUnit : LogicUnit
    {
        /// <summary>
        /// 生成组 ID。
        /// </summary>
        public int SpawnGroupId { get; private set; }

        /// <summary>
        /// 是否精英怪。
        /// </summary>
        public bool IsElite { get; private set; }

        protected override bool OnApplyCreateData(LogicUnitCreateData createData)
        {
            if (createData.Payload == null)
            {
                return true;
            }

            if (!createData.TryGetPayload<MonsterUnitCreatePayload>(out var payload))
            {
                return false;
            }

            SpawnGroupId = payload.SpawnGroupId;
            IsElite = payload.IsElite;
            return true;
        }

        protected override void OnDestroy()
        {
            SpawnGroupId = 0;
            IsElite = false;
        }
    }
}
