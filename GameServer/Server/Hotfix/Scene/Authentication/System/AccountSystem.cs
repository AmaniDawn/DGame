using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix;

public sealed class AccountSystem : DestroySystem<Account>
{
    protected override void Destroy(Account self)
    {
        self.Username = null;
        self.Password = null;
        self.CreateTime = 0;
        self.LastLoginTime = 0;
    }
}