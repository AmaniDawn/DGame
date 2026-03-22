using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

public sealed class C2G_QueryFuncOpenListRequestHandler : MessageRPC<C2G_QueryFuncOpenListRequest, G2C_QueryFuncOpenListResponse>
{
    protected override async FTask Run(Session session, C2G_QueryFuncOpenListRequest request, G2C_QueryFuncOpenListResponse response, Action reply)
    {
        if (session == null || session.IsDisposed)
        {
            return;
        }

        var playerDataFlagComponent = session.GetComponent<PlayerDataFlagComponent>();
        PlayerData playerData = playerDataFlagComponent?.playerData;
        if (playerData == null || playerData.IsDisposed)
        {
            response.ErrorCode = ErrorCode.FUNC_QUERY_ERROR;
            return;
        }

        var newOpenFuncList = playerData.RefreshOpenFuncState();
        if (newOpenFuncList.Count > 0)
        {
            await playerData.SaveDatabase();
        }

        response.ErrorCode = ErrorCode.SUCCESS;
        response.OpenFuncList = new List<int>();
        response.OpenFuncList.AddRange(playerData.GetOpenFuncList());
    }
}
