using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix;

public sealed class AccountCacheInfoTimeOutDestroySystem : DestroySystem<AccountCacheInfoTimeOutComponent>
{
    protected override void Destroy(AccountCacheInfoTimeOutComponent self)
    {
        if (self.TimeId != 0)
        {
            self.Scene.TimerComponent.Net.Remove(ref self.TimeId);
        }
        self.Key = string.Empty;
    }
}

public static class AccountCacheInfoTimeOutComponentSystem
{
    public static void TimeOut(this AccountCacheInfoTimeOutComponent self, string key, int timeout)
    {
        var scene = self.Scene;
        self.Key = key;
        var runtimeId = self.RuntimeId;
        // 创建一个任务计时器 用在timeout时间后执行 并且要清除掉当前鉴权服务器的缓存
        self.TimeId = scene.TimerComponent.Net.OnceTimer(timeout, () =>
        {
            // 每次使用 Component 的 RuntimeId 都是不一样的
            if (runtimeId != self.RuntimeId)
            {
                return;
            }

            self.TimeId = 0;
            AuthenticationHelper.RemoveLoginCache(scene, self.Key, true);
        });
    }
}