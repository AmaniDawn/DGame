using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;

namespace Fantasy;

/// <summary>
/// 数据库管理组件
/// <remarks>负责场景启动时驱动数据库版本迁移(ps:创建索引等)</remarks>
/// </summary>
public sealed class DatabaseComponent : Entity
{
    /// <summary>
    /// 当前数据库版本记录
    /// <remarks>避免重复查询数据库 不持久化到数据库</remarks>
    /// </summary>
    [BsonIgnore]
    public DbVersion DbVersion { get; set; }
}