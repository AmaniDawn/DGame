using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 注册协议
/// </summary>
public sealed class C2A_RegisterRequestHandler : MessageRPC<C2A_RegisterRequest, A2C_RegisterResponse>
{
    protected override async FTask Run(Session session, C2A_RegisterRequest request, A2C_RegisterResponse response, Action reply)
    {
        if (!session.CheckInterval(2000))
        {
            // 返回这个 3 代表操作过于频繁
            response.ErrorCode = 1003;
            return;
        }
        session.SetTimeOut(3000);
        response.ErrorCode = await AuthenticationHelper.Register(session.Scene, request.UserName, request.Password, "用户注册");
        Log.Debug($"Register 当前的服务器是: {session.Scene.SceneConfigId}");
    }
}