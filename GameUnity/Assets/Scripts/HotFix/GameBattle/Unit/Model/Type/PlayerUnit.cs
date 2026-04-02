namespace GameBattle
{
    /// <summary>
    /// 玩家逻辑单位。
    /// </summary>
    public sealed class PlayerUnit : LogicUnit
    {
        /// <summary>
        /// 玩家 ID。
        /// </summary>
        public int PlayerId { get; private set; }

        /// <summary>
        /// 角色实体 ID。
        /// </summary>
        public long ActorId { get; private set; }

        /// <summary>
        /// 武器配置 ID。
        /// </summary>
        public uint WeaponConfigId { get; private set; }

        /// <summary>
        /// 服装配置 ID。
        /// </summary>
        public uint ClothingConfigId { get; private set; }

        /// <summary>
        /// 角色体型类型。
        /// </summary>
        public byte RoleBodyType { get; private set; }

        protected override bool OnApplyCreateData(LogicUnitCreateData createData)
        {
            if (createData.Payload == null)
            {
                return true;
            }

            if (!createData.TryGetPayload<PlayerUnitCreatePayload>(out var payload))
            {
                return false;
            }

            PlayerId = payload.PlayerId;
            ActorId = payload.ActorId;
            WeaponConfigId = payload.WeaponConfigId;
            ClothingConfigId = payload.ClothingConfigId;
            RoleBodyType = payload.RoleBodyType;
            return true;
        }

        protected override void OnDestroy()
        {
            PlayerId = 0;
            ActorId = 0;
            WeaponConfigId = 0;
            ClothingConfigId = 0;
            RoleBodyType = 0;
        }
    }
}