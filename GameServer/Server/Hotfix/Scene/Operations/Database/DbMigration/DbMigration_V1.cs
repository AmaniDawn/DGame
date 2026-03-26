using Fantasy;
using Fantasy.Async;
using Fantasy.Database;
using MongoDB.Driver;

// ReSharper disable InconsistentNaming

namespace Hotfix;

public sealed class DbMigration_V1 : IDbMigration
{
    /// <summary>
    /// 当前迁移版本号
    /// </summary>
    public int Version => 1;
    
    /// <summary>
    /// 执行迁移逻辑
    /// </summary>
    /// <param name="database"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async FTask Upgrade(IDatabase database)
    {
        // Account.Username 升序唯一索引
        // 用途：注册和登录均通过 Username 字段查询 唯一约束防止重名注册 提升查询效率
        await database.CreateIndex<Account>
        (
            [Builders<Account>.IndexKeys.Ascending(d=>d.Username)],
            [new CreateIndexOptions(){ Unique = true, Name = "idx_account_name"}]
        );
        
        // PlayerData.RoleName 升序唯一索引
        // 用途：注册和登录均通过 RoleName 字段查询 唯一约束防止重名注册 提升查询效率
        await database.CreateIndex<PlayerData>
        (
            [Builders<PlayerData>.IndexKeys.Ascending(d=>d.RoleName)],
            [new CreateIndexOptions(){ Unique = true, Name = "idx_player_data_role_name"}]
        );
    }
}