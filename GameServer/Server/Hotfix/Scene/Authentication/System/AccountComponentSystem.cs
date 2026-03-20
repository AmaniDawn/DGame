using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix;

public sealed class AccountComponentDestroySystem : DestroySystem<Account>
{
    protected override void Destroy(Account self)
    {
        self.Username = string.Empty;
        self.Password = string.Empty;
        self.CreateTime = 0;
        self.LastLoginTime = 0;
        self.RecentServerList.Clear();
    }
}