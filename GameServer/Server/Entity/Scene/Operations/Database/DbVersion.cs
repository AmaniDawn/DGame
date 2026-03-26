using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;

namespace Fantasy;

/// <summary>
/// 数据库前置版本的记录实体
/// </summary>
public sealed class DbVersion : Entity
{
    /// <summary>
    /// 当前已经执行的最高迁移的版本号
    /// <remarks>初始值为0(数据库首次初始化) 每次执行成功一个迁移后底层并持久化</remarks>
    /// </summary>
    [BsonElement("v")]
    public int Version { get; set; }
}