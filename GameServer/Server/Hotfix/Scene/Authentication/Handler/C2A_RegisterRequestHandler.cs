using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using GameProto;
// ReSharper disable All

namespace Hotfix;

/// <summary>
/// 注册协议
/// </summary>
public sealed class C2A_RegisterRequestHandler : MessageRPC<C2A_RegisterRequest, A2C_RegisterResponse>
{
    protected override async FTask Run(Session session, C2A_RegisterRequest request, A2C_RegisterResponse response, Action reply)
    {
        response.ErrorCode = await session.Scene.Register(request.UserName, request.Password);
        session.SetLifeTime(TbFuncParamConfig.AccountRegisterSessionLifeTime);
    }
}