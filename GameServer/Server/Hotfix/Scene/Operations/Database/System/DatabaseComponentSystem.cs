using Fantasy;
using Fantasy.Async;
using Fantasy.Database;
using Fantasy.Entitas;

namespace Hotfix;

public static class DatabaseComponentSystem
{
    private static readonly List<IDbMigration> m_dbMigrations = 
    [
        new DbMigration_V1() // V1 账号和角色基础索引
    ];
    
    public static async FTask MigrateAsync(this DatabaseComponent self)
    {
        var worldDatabase = self.Scene.World.Database;
        // 读取当前版本记录 首次运行集合为空 则初始记录版本号是0 表示尚未执行任何迁移
        var dbVersion = await worldDatabase.First<DbVersion>(d => d.Version <= 0);

        if (dbVersion == null)
        {
            dbVersion = Entity.Create<DbVersion>(self.Scene);
            dbVersion.Version = 0;
            await worldDatabase.Insert(dbVersion);
        }
        
        // 缓存记录 避免重复查询
        self.DbVersion = dbVersion;
        var curVersion = dbVersion.Version;
        // 筛选出尚未迁移的并按版本号升序排序
        var dbMigrations = m_dbMigrations
            .Where(d => d.Version > curVersion)
            .OrderBy(d => d.Version)
            .ToList();

        if (dbMigrations.Count <= 0)
        {
            return;
        }

        Log.Info($"[DatabaseComponent] 当前版本v{curVersion} 未执行迁移数量: {dbMigrations.Count}");

        foreach (var dbMigration in dbMigrations)
        {
            Log.Info($"[DatabaseComponent] 开始执行迁移v{dbMigration.Version}");
            await dbMigration.Upgrade(worldDatabase);
            // 每个迁移完成后立即执行持久化
            // 若在此之前服务器异常退出了 下次重启会重跑迁移而不是跳过 保证不漏执行
            dbVersion.Version = dbMigration.Version;
            await worldDatabase.Save(dbVersion);
            Log.Info($"[DatabaseComponent] 迁移v{dbMigration.Version}完成");
        }
    }
}