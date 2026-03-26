using Fantasy.Async;
using Fantasy.Database;

namespace Fantasy;

public interface IDbMigration
{
    /// <summary>
    /// 该迁移对应的版本
    /// <remarks>必须是从1开始连续递增</remarks>
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// 执行正向迁移
    /// <remarks>通常包括创建索引、删除过期索引、改动字段名等</remarks>
    /// </summary>
    /// <param name="database"></param>
    /// <returns></returns>
    FTask Upgrade(IDatabase database);
}