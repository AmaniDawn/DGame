using System.Security.Cryptography;
using System.Text;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Helper;
using GameProto;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Hotfix;

public static class AccountManagerComponentSystem
{
    /// <summary>
    /// 注册新账号
    /// <remarks>协程锁保证不会并发注册 防止数据穿透</remarks>
    /// </summary>
    /// <param name="self">账号管理组件</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public static async FTask<uint> Register(this AccountManagerComponent self, string username, string password)
    {
        // 校验账号密码
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            // 代表注册账号参数不完整或不合法
            return ErrorCode.REGISTER_INVALID_PARAMETER;
        }

        try
        {
            var scene = self.Scene;
            var worldDataBase = scene.World.Database;
            var waitForId = HashCodeHelper.ComputeHash64(username);

            // 异步协程锁
            using (await scene.CoroutineLockComponent.Wait(CoroutineLockType.AccountRegisterLock, waitForId, "AccountManagerComponentSystem.Register", 10000))
            {
                if (await worldDataBase.Exist<Account>(d => d.Username == username))
                {
                    // 账号已经存在
                    return ErrorCode.REGISTER_ACCOUNT_EXISTS;
                }
                
                // MD5加密密码 转为全小写
                var hashPassword = Md5Format(password);

                // 创角新账号 并持久化到数据库
                using var account = Entity.Create<Account>(scene, true, true);

                account.Username = username;
                account.Password = hashPassword;
                account.CreateTime = TimeHelper.Now;
                // 插入数据库 如果存在则不插入
                await worldDataBase.Insert(account);

                return ErrorCode.SUCCESS;
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
            // 执行注册的时候发生的未知错误
            return ErrorCode.REGISTER_UNKNOW_EORROR;
        }
    }

    /// <summary>
    /// 登录账号
    /// </summary>
    /// <param name="self">账号管理组件</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public static async FTask<AccountLoginResult> Login(this AccountManagerComponent self, string username,
        string password)
    {
        // 校验账号密码
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            // 代表注册账号参数不完整或不合法
            return new AccountLoginResult(ErrorCode.LOGIN_INVALID_PARAMETER);
        }

        try
        {
            var scene = self.Scene;
            var worldDataBase = scene.World.Database;

            // 从数据库查询账号
            var account = await worldDataBase.First<Account>(d => d.Username == username);

            if (account == null)
            {
                // 账号不存在
                return new AccountLoginResult(ErrorCode.LOGIN_ACCOUNT_NOT_EXIST);
            }

            // 校验密码 MD5加密密码 转为全小写
            var hashPassword = Md5Format(password);

            if (hashPassword != account.Password)
            {
                // 密码错误
                return new AccountLoginResult(ErrorCode.LOGIN_ACCOUNT_EXISTS_PASSWORD_ERROR);
            }

            // 更新登录时间
            account.LastLoginTime = TimeHelper.Now;
            // 更新数据库
            await worldDataBase.Save(account);
            // 生成登录的Jwt Token
            var token = scene.GenerateJwtToken(account.Id, account.Username);

            return new AccountLoginResult(account.Id, token, ErrorCode.SUCCESS, account.RecentServerList);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return new AccountLoginResult(ErrorCode.LOGIN_UNKNOW_EORROR);
        }
    }

    /// <summary>
    /// 记录最近登录的服务器列表
    /// </summary>
    /// <param name="self"></param>
    /// <param name="roleId">账户ID</param>
    /// <param name="serverId">服务器ID</param>
    public static async FTask RecordRecentServer(this AccountManagerComponent self, long roleId, int serverId)
    {
        if (roleId == 0 || serverId == 0)
        {
            return;
        }

        if (!TbServerConfig.IsExist(serverId))
        {
            return;
        }
        
        var scene = self.Scene;
        var worldDataBase = scene.World.Database;
        var account = await worldDataBase.Query<Account>(roleId);

        if (account == null)
        {
            return;
        }

        if (account.RecentServerList.Count >= TbFuncParamConfig.RecentServerMaxCount)
        {
            account.RecentServerList.RemoveAt(TbFuncParamConfig.RecentServerMaxCount - 1);
        }

        account.RecentServerList.Remove(serverId);
        account.RecentServerList.Insert(0, serverId);
        
        await worldDataBase.Save(account);
    }

    /// <summary>
    /// 获取最近登录服务器对应的角色等级摘要
    /// </summary>
    /// <param name="self"></param>
    /// <param name="accountId">账号ID</param>
    /// <param name="recentServerList">最近登录服务器列表</param>
    public static async FTask<List<RecentServerRoleInfo>> GetRecentServerRoleInfos(this AccountManagerComponent self, long accountId, IReadOnlyList<int> recentServerList)
    {
        var result = new List<RecentServerRoleInfo>();

        if (accountId == 0 || recentServerList == null || recentServerList.Count == 0)
        {
            return result;
        }

        var worldDataBase = self.Scene.World.Database;
        foreach (var serverId in recentServerList)
        {
            if (serverId == 0)
            {
                continue;
            }

            var playerData = await worldDataBase.First<PlayerData>(d => d.AccountID == accountId && d.ServerID == serverId);
            if (playerData == null)
            {
                continue;
            }

            result.Add(new RecentServerRoleInfo
            {
                ServerID = serverId,
                Level = playerData.Level,
            });
        }

        return result;
    }

    private static string Md5Format(string password)
        => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password))).ToLower();
}
