using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix;

public sealed class AccountTimeOutDestroySystem : DestroySystem<AccountTimeOutComponent>
{
    protected override void Destroy(AccountTimeOutComponent self)
    {
        if (self.TimeId != 0)
        {
            self.Scene.TimerComponent.Net.Remove(ref self.TimeId);
        }
    }
}

public static class AccountTimeOutComponentSystem
{
    public static void TimeOut(this AccountTimeOutComponent self, int timeout)
    {
        var scene = self.Scene;
        var account = (Account)self.Parent;
        var accountRuntimeId = account.RuntimeId;
        // 创建一个任务计时器 用在timeout时间后执行 并且要清除掉当前鉴权服务器的缓存
        self.TimeId = scene.TimerComponent.Net.OnceTimer(timeout, () =>
        {
            if (accountRuntimeId != account.RuntimeId)
            {
                return;
            }

            self.TimeId = 0;
            AuthenticationHelper.RemoveCache(scene, account.Username, true);
        });
    }
}