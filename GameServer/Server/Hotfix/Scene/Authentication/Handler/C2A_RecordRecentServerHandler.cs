using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
// ReSharper disable InconsistentNaming

namespace Hotfix;

public sealed class C2A_RecordRecentServerHandler : Message<C2A_RecordRecentServer>
{
    protected override async FTask Run(Session session, C2A_RecordRecentServer message)
    {
        await session.Scene.RecordRecentServer(message.RoleID, message.ServerID);
    }
}