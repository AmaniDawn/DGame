using Fantasy;
using Fantasy.Async;
using Fantasy.Event;
// ReSharper disable InconsistentNaming

namespace Hotfix;

public sealed class OnSceneCreate_Init : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;
        switch (scene.SceneType)
        {
            case SceneType.Operations:
                // Operations 场景是 AuthWorld 的单例入口 用于驱动账号库迁移
                await scene.AddComponent<DatabaseComponent>().MigrateAsync();
                break;
            case SceneType.Address:
                // Address 场景是 GameWorld 的单例入口 用于驱动玩家库迁移
                await scene.AddComponent<DatabaseComponent>().MigrateAsync();
                break;
            case SceneType.Authentication:
                // 账号管理组件 用于注册和登录
                scene.AddComponent<AccountManagerComponent>();
                // 账号 Jwt Token 组件 用于生成和验证Token
                scene.AddComponent<AccountJwtComponent>();
                break;

            case SceneType.Gate:
                // 账号 Jwt Token 组件 用于生成和验证Token
                scene.AddComponent<AccountJwtComponent>();
                // 添加账号数据管理组件
                scene.AddComponent<PlayerManagerComponent>();
                break;
        }

        await FTask.CompletedTask;
    }
}