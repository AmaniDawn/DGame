namespace GameBattle
{
    /// <summary>
    /// 玩家逻辑单位专属创建数据。
    /// </summary>
    public sealed class PlayerUnitCreatePayload : LogicUnitCreatePayload
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

        public override UnitType UnitType => UnitType.GamePlayer;

        /// <summary>
        /// 创建玩家专属创建数据。
        /// </summary>
        public static PlayerUnitCreatePayload Create(int playerId, long actorId = 0, uint weaponConfigId = 0, uint clothingConfigId = 0, byte roleBodyType = 0)
        {
            var payload = Spawn<PlayerUnitCreatePayload>();
            payload.PlayerId = playerId;
            payload.ActorId = actorId;
            payload.WeaponConfigId = weaponConfigId;
            payload.ClothingConfigId = clothingConfigId;
            payload.RoleBodyType = roleBodyType;
            return payload;
        }

        public override void OnRelease()
        {
            PlayerId = 0;
            ActorId = 0;
            WeaponConfigId = 0;
            ClothingConfigId = 0;
            RoleBodyType = 0;
        }
    }
}