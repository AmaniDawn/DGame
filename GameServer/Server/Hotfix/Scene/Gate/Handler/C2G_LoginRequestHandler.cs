using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using GameProto;

// ReSharper disable NotAccessedVariable
// ReSharper disable InconsistentNaming
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。

namespace Hotfix;

public sealed class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2G_LoginRequest request, G2C_LoginResponse response, Action reply)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || request.ServerID == 0)
        {
            // 1.客户端逻辑问题
            // 2.恶意攻击
            session.Dispose();
            return;
        }
        var scene = session.Scene;

        if (!TbServerConfig.IsExist(request.ServerID))
        {
            session.Dispose();
            return;
        }

        if (!scene.ValidationToken(request.Token, out var accountId, out var accountName))
        {
            // Token验证不通过
            session.Dispose();
            response.ErrorCode = ErrorCode.RLOGIN_TOKEN_ERROR;
            return;
        }

        var playerManagerComponent = scene.GetComponent<PlayerManagerComponent>();

        if (playerManagerComponent.TryGet(accountId, request.ServerID, out var playerData))
        {
            // 只要发送了登录协议 就取消账号实体的延迟下线操作
            playerData.CancelDestroyTimeout();
            // 如果存在 要进行顶号或者重登、断线重连等流程
            // 在Gate缓存存在该账号
            if (session.RuntimeId == playerData.SessionRuntimeId)
            {
                // 1.同一个客户端发送重复登录请求
                response.ErrorCode = ErrorCode.LOGIN_ACCOUNT_ALREADY_ONLINE;
                return;
            }

            if (scene.TryGetEntity<Session>(playerData.SessionRuntimeId, out var oldSession))
            {
                // 2.客户端断线重连
                // 3.顶号
                // 先给旧的客户端通知重登消息 再给
                // 旧客户端连接设置一个延迟销毁
                oldSession.GetComponent<PlayerDataFlagComponent>().SetPlayerData(null);
                oldSession.Send(new G2C_RepeatLogin());
                oldSession.SetLifeTime(TbFuncParamConfig.OldSessionLifeTime);
            }
        }
        else
        {
            // 如果缓存中不存在
            // 首先先到数据库查询是否存在
            // 没有就要创建并保存到数据库
            playerData = await playerManagerComponent.Create(accountId, request.ServerID);
            // 执行上线操作
            await playerData.Online();
        }

        response.ErrorCode = ErrorCode.SUCCESS;
        response.PlayerData = playerData.ToCSPlayerData();
        // 记录玩家上线时间
        playerData.LastLoginTime = TimeHelper.Now;
        // 记录客户端的Session
        playerData.RecordSession(session.RuntimeId);
        // 给当前客户端的Session添加一个组件 当Session异常断开的时候 进行玩家账号数据下线逻辑
        session.AddComponent<PlayerDataFlagComponent>().SetPlayerData(playerData);
    }
}
