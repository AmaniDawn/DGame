using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;
// ReSharper disable InconsistentNaming
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 'required' 修饰符或声明为可以为 null。

namespace Fantasy;

public sealed class PlayerData : Entity
{
    /// <summary>
    /// Session的ID
    /// </summary>
    [BsonIgnore]
    public long SessionRuntimeId { get; set; }

    /// <summary>
    /// AccountID
    /// </summary>
    public long AccountID { get; set; }

    /// <summary>
    /// ServerID
    /// </summary>
    public int ServerID { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; }

    /// <summary>
    /// 头像ID
    /// </summary>
    public int HeadID { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public byte Sex { get; set; }

    /// <summary>
    /// 等级
    /// </summary>
    public uint Level { get; set; }

    /// <summary>
    /// 经验
    /// </summary>
    public uint Exp { get; set; }

    /// <summary>
    /// 战斗力
    /// </summary>
    public uint FightValue { get; set; }

    /// <summary>
    /// 钻石
    /// </summary>
    public uint Diamond { get; set; }

    /// <summary>
    /// 金币
    /// </summary>
    public uint Gold { get; set; }

    /// <summary>
    /// 体力
    /// </summary>
    public uint Stam { get; set; }

    /// <summary>
    /// 是否完成新手引导
    /// </summary>
    public byte IsFinGuide { get; set; }

    /// <summary>
    /// 个性签名
    /// </summary>
    public string Sign { get; set; }

    /// <summary>
    /// 所在主服
    /// </summary>
    public int WorldID { get; set; }

    /// <summary>
    /// 累计充值金额
    /// </summary>
    public uint TotalRmb { get; set; }

    /// <summary>
    /// 上次增加体力时间
    /// </summary>
    public long LastAddStamTime { get; set; }

    /// <summary>
    /// 每日购买体力次数
    /// </summary>
    public int DailyBuyStamCount { get; set; }

    /// <summary>
    /// 账号创建时间
    /// </summary>
    public long CreateTime { get; set; }

    /// <summary>
    /// 账号上次登陆时间
    /// </summary>
    public long LastLoginTime { get; set; }

    /// <summary>
    /// 已开放功能列表（持久化）。
    /// </summary>
    public List<int> OpenFuncList { get; set; } = new List<int>();
}
