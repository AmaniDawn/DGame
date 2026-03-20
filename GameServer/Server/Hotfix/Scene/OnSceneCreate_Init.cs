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
            case SceneType.Authentication:
                // 账号管理组件 用于注册和登录
                scene.AddComponent<AccountManagerComponent>();
                // 账号 Jwt Token 组件 用于生成和验证Token
                scene.AddComponent<AccountJwtComponent>();
                break;

            case SceneType.Gate:
                break;
        }

        await FTask.CompletedTask;
    }
}