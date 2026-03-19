using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

public sealed class C2A_LoginRequestHandler : MessageRPC<C2A_LoginRequest, A2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2A_LoginRequest request, A2C_LoginResponse response, Action reply)
    {
        var scene = session.Scene;
        var result = await AuthenticationHelper.Login(scene, request.UserName, request.Password);

        if (result.errorCode == 0)
        {
            var ipResult = AuthenticationHelper.GetOuterIp(result.accountId);
            // 颁发一个Token令牌给客户端
            response.Token = AuthenticationJwtHelper.GetToken(
                scene, result.accountId, ipResult.outerIp, ipResult.outerPort, ipResult.sceneId);
        }
        response.ErrorCode = result.errorCode;
        Log.Debug($"Login 当前的服务器是: {scene.SceneConfigId}");
    }
}