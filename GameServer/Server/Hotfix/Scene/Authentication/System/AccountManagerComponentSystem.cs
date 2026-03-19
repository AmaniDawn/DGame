using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Helper;
using GameProto;

namespace Hotfix;

public static class AccountManagerComponentSystem
{
    public static async FTask<uint> Register(this Entity self, string username, string password)
    {
        // 1、校验账号密码
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            // 代表注册账号参数不完整或不合法
            return ErrorCodeDefine.REGISTER_INVALID_PARAMETER;
        }

        try
        {
            var scene = self.Scene;
            var worldDataBase = scene.World.Database;

            // 异步锁
            using (await scene.CoroutineLockComponent.Wait(1, 1, "AccountManagerComponentSystem.Register", 10000))
            {
                if (await worldDataBase.Exist<Account>(d => d.Username == username))
                {
                    // 账号已经存在
                    return ErrorCodeDefine.REGISTER_ACCOUNT_EXISTS;
                }

                // 创角新账号 并持久化到数据库
                using var account = Entity.Create<Account>(scene, true, true);

                account.Username = username;
                account.Password = password;
                account.CreateTime = TimeHelper.Now;
                // 插入数据库 如果存在则不插入
                await worldDataBase.Insert(account);

                return ErrorCodeDefine.SUCCESS;
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
            // 执行注册的时候发生的未知错误
            return ErrorCodeDefine.REGISTER_UNKNOW_EORROR;
        }
    }
}