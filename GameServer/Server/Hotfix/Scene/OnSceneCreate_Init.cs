using Fantasy;
using Fantasy.Async;
using Fantasy.Event;

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
                break;

            case SceneType.Gate:
                break;
        }

        await FTask.CompletedTask;
    }
}