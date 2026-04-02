using System;
using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位创建数据。
    /// <remarks>
    /// 通过 <see cref="LogicUnitCreatePayload"/> 承载类型专属数据，
    /// 避免每新增一种单位都要修改聚合结构。
    /// </remarks>
    /// </summary>
    public sealed class LogicUnitCreateData : MemoryObject
    {
        private LogicUnitCreatePayload m_payload; // 当前逻辑单位的类型专属创建数据
        private LogicUnitAttrData m_baseAttr; // 当前创建数据携带的基础属性数据

        /// <summary>
        /// 目标逻辑单位类型。
        /// </summary>
        public UnitType UnitType { get; private set; }

        /// <summary>
        /// 外部指定的逻辑单位 ID；为 0 时由战斗上下文自动分配。
        /// </summary>
        public ulong UnitId { get; private set; }

        /// <summary>
        /// 所属单位 ID。
        /// </summary>
        public ulong OwnerUnitId { get; private set; }

        /// <summary>
        /// 单位名称。
        /// </summary>
        public string UnitName { get; private set; } = string.Empty;

        /// <summary>
        /// 单位配置 ID。
        /// </summary>
        public uint ConfigId { get; private set; }

        /// <summary>
        /// 是否包含出生位姿。
        /// </summary>
        public bool HasBornPose { get; private set; }

        /// <summary>
        /// 出生位置。
        /// </summary>
        public FixedPointVector3 BornPosition { get; private set; } = FixedPointVector3.zero;

        /// <summary>
        /// 出生朝向。
        /// </summary>
        public FixedPointVector3 BornForward { get; private set; } = FixedPointVector3.forward;

        /// <summary>
        /// 出生缩放。
        /// </summary>
        public FixedPointVector3 BornScale { get; private set; } = FixedPointVector3.one;

        /// <summary>
        /// 单位基础属性初始化数据。
        /// </summary>
        public LogicUnitAttrData BaseAttr => m_baseAttr ??= LogicUnitAttrData.Create();

        /// <summary>
        /// 类型专属创建数据。
        /// </summary>
        public LogicUnitCreatePayload Payload => m_payload;

        /// <summary>
        /// 创建指定类型的逻辑单位创建数据。
        /// </summary>
        public static LogicUnitCreateData Create(UnitType unitType)
        {
            var createData = Spawn<LogicUnitCreateData>();
            createData.UnitType = unitType;
            return createData;
        }

        /// <summary>
        /// 创建玩家逻辑单位创建数据。
        /// </summary>
        public static LogicUnitCreateData CreatePlayer(int playerId, long actorId = 0, uint weaponConfigId = 0, uint clothingConfigId = 0, byte roleBodyType = 0)
            => Create(UnitType.GamePlayer).SetPayload(PlayerUnitCreatePayload.Create(playerId, actorId, weaponConfigId, clothingConfigId, roleBodyType));

        /// <summary>
        /// 创建怪物逻辑单位创建数据。
        /// </summary>
        public static LogicUnitCreateData CreateMonster(int spawnGroupId = 0, bool isElite = false)
            => Create(UnitType.Monster).SetPayload(MonsterUnitCreatePayload.Create(spawnGroupId, isElite));

        /// <summary>
        /// 设置逻辑单位 ID。
        /// </summary>
        public LogicUnitCreateData SetUnitId(ulong unitId)
        {
            UnitId = unitId;
            return this;
        }

        /// <summary>
        /// 设置所属单位 ID。
        /// </summary>
        public LogicUnitCreateData SetOwnerUnitId(ulong ownerUnitId)
        {
            OwnerUnitId = ownerUnitId;
            return this;
        }

        /// <summary>
        /// 设置单位名称。
        /// </summary>
        public LogicUnitCreateData SetUnitName(string unitName)
        {
            UnitName = unitName ?? string.Empty;
            return this;
        }

        /// <summary>
        /// 设置单位配置 ID。
        /// </summary>
        public LogicUnitCreateData SetConfigId(uint configId)
        {
            ConfigId = configId;
            return this;
        }

        /// <summary>
        /// 设置单位基础属性。
        /// </summary>
        public LogicUnitCreateData SetBaseAttr(int atk, int maxHp, FixedPoint64 moveSpeed)
        {
            BaseAttr.Atk = atk;
            BaseAttr.MaxHp = maxHp;
            BaseAttr.MoveSpeed = moveSpeed;
            return this;
        }

        /// <summary>
        /// 复制一份单位基础属性。
        /// </summary>
        public LogicUnitCreateData SetBaseAttr(LogicUnitAttrData attrData)
        {
            BaseAttr.CopyFrom(attrData);
            return this;
        }

        /// <summary>
        /// 设置出生位姿。
        /// </summary>
        public LogicUnitCreateData SetBornPose(FixedPointVector3 position, FixedPointVector3 forward, FixedPointVector3 scale)
        {
            HasBornPose = true;
            BornPosition = position;
            BornForward = forward.IsNearlyZero() ? FixedPointVector3.forward : forward.normalized;
            BornScale = scale;
            return this;
        }

        /// <summary>
        /// 清空出生位姿。
        /// </summary>
        public LogicUnitCreateData ClearBornPose()
        {
            HasBornPose = false;
            BornPosition = FixedPointVector3.zero;
            BornForward = FixedPointVector3.forward;
            BornScale = FixedPointVector3.one;
            return this;
        }

        /// <summary>
        /// 设置类型专属创建数据。
        /// </summary>
        public LogicUnitCreateData SetPayload(LogicUnitCreatePayload payload)
        {
            if (payload != null && UnitType != UnitType.None && payload.UnitType != UnitType)
            {
                throw new ArgumentException($"Payload unit type mismatch. Expected: {UnitType}, Actual: {payload.UnitType}");
            }

            ReleasePayload();
            m_payload = payload;
            if (payload != null)
            {
                UnitType = payload.UnitType;
            }
            return this;
        }

        /// <summary>
        /// 尝试获取指定类型的专属创建数据。
        /// </summary>
        public bool TryGetPayload<T>(out T payload) where T : LogicUnitCreatePayload
        {
            payload = m_payload as T;
            return payload != null;
        }

        public override void OnRelease()
        {
            ReleasePayload();
            ReleaseBaseAttr();
            UnitType = UnitType.None;
            UnitId = 0;
            OwnerUnitId = 0;
            UnitName = string.Empty;
            ConfigId = 0;
            HasBornPose = false;
            BornPosition = FixedPointVector3.zero;
            BornForward = FixedPointVector3.forward;
            BornScale = FixedPointVector3.one;
        }

        private void ReleasePayload()
        {
            if (m_payload == null)
            {
                return;
            }

            MemoryObject.Release(m_payload);
            m_payload = null;
        }

        private void ReleaseBaseAttr()
        {
            if (m_baseAttr == null)
            {
                return;
            }

            MemoryObject.Release(m_baseAttr);
            m_baseAttr = null;
        }
    }
}
