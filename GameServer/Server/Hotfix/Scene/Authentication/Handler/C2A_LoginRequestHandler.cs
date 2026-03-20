using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using GameProto;
// ReSharper disable InconsistentNaming

namespace Hotfix;

public sealed class C2A_LoginRequestHandler : MessageRPC<C2A_LoginRequest, A2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2A_LoginRequest request, A2C_LoginResponse response, Action reply)
    {
        var scene = session.Scene;
        var result = await scene.Login(request.UserName, request.Password);

        if (result.ErrorCode == ErrorCode.SUCCESS)
        {
            response.Token = result.Token;
            response.RoleID = result.RoleId;
            response.ServerInfoList = TbServerConfig.ServerInfoList;
            response.RecentServerList.AddRange(result.RecentServerList); 
        }
        response.ErrorCode = result.ErrorCode;
        session.SetLifeTime(TbFuncParamConfig.AccountRegisterSessionLifeTime);
    }
}