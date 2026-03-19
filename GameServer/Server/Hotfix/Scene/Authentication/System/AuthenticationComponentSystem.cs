using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Platform.Net;

#pragma warning disable CS8602 // 解引用可能出现空引用。
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。

namespace Hotfix;

public sealed class AuthenticationComponentDestroySystem : DestroySystem<AuthenticationComponent>
{
    protected override void Destroy(AuthenticationComponent self)
    {
        foreach (var account in self.CacheAccountList.Values.ToArray())
        {
            account.Dispose();
        }
        self.CacheAccountList.Clear();
    }
}

internal static class AuthenticationComponentSystem
{
    public static void UpdatePosition(this AuthenticationComponent self)
    {
        // 1、通过远程接口或者本地文件来拿到鉴权组
        // 2、通过配置文件
        var authenticationCfgList = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Authentication);
        // 拿到当前 Scene 的配置文件
        var sceneCfg = SceneConfigData.Instance.Get(self.Scene.SceneConfigId);
        // 获取到当前Scene在鉴权组的位置
        self.Position = authenticationCfgList.IndexOf(sceneCfg);
        // 获取鉴权组的总数
        self.AuthenticationCount = authenticationCfgList.Count;
        Log.Info($"鉴权服务器启动成功! Position: {self.Position} AuthenticationCount: {self.AuthenticationCount}");
    }

    internal static async FTask<(uint errorCode, long accountId)> Login(this AuthenticationComponent self, string userName, string password)
    {
        Log.Debug("登录请求");
        // 1、检查传递的参数是否完整以及合法
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            // 代表账号参数不完整或不合法
            return (1001, 0);
        }

        // 检查账号是否应该在当前鉴权服务器中处理
        var position = HashCodeHelper.MurmurHash3(userName) % self.AuthenticationCount;
        if (self.Position != position)
        {
            // 连接非法服务器地址
            return (1005, 0);
        }

        var scene = self.Scene;
        var worldDatabase = scene.World.Database;
        var userNameHashCode = userName.GetHashCode();

        // 利用协程锁 解决异步原子性问题
        using (var @lock =
               await scene.CoroutineLockComponent.Wait((int)LockType.Authentication_LoginLock, userNameHashCode))
        {
            // 如果用户频繁发送登录亲贵 导致服务器会频繁请求数据库或缓存
            // 针对这个问题可以利用缓存解决这个问题
            // 1、创建一个新的字典容器在 AuthenticationComponent 中存储登录信息
            // 2、key = userName + password  Value =
            // 3、为了防止缓存暴涨 需要一个定期清理的过程 Value要是一个实体
            // 4、因为这个实体下面可以挂载组件 作用是定时清理这个缓存

            // 问题:
            // 1、如果用户的密码改了怎么办？
            // 因为缓存中有定时清除 所以遇到改密码的情况下 最多等待这个缓存清除了 然后就可以登录了
            // 2、如果不这样做 还有其他办法？
            // 通过防火墙的策略来限制用户请求 比如100ms请求一次
            Account account = null;
            var cacheKey = userName + password;
            if (self.CacheLoginAccountList.TryGetValue(cacheKey, out var cacheInfo))
            {
                Log.Debug("[缓存] 从登录缓存数据中获取登录数据");
                account = cacheInfo.GetComponent<Account>();
                if (account == null)
                {
                    return (1004, 0);
                }
                else
                {
                    return (0, account.Id);
                }
            }

            uint result = 0;
            cacheInfo = Entity.Create<AccountCacheInfo>(scene, true, true);
            account = await worldDatabase.First<Account>(d => d.Username == userName && d.Password == password);

            if (account == null)
            {
                Log.Debug("[数据库] 用户名或者密码错误");
                // 用户名或者密码错误
                result = 1004;
            }
            else
            {
                Log.Debug("[数据库] 登录成功");
                // 更新登录时间 保存到数据库
                account.LastLoginTime = TimeHelper.Now;
                await worldDatabase.Save(account);
                account.Deserialize(scene);
                cacheInfo.AddComponent(account);
            }

            // 添加TimeOut组件用来定时清除账号缓存
            cacheInfo.AddComponent<AccountCacheInfoTimeOutComponent>().TimeOut(cacheKey, 5000);
            // 缓存登录账号数据
            self.CacheLoginAccountList.TryAdd(cacheKey, cacheInfo);

            if (result != 0)
            {
                return (result, 0);
            }

            return (result, account.Id);
        }
    }

    /// <summary>
    /// 鉴权注册接口
    /// </summary>
    internal static async FTask<uint> Register(this AuthenticationComponent self, string userName, string password,
        string source)
    {
        // 1、检查传递的参数是否完整以及合法
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            // 代表注册账号参数不完整或不合法
            return 1001;
        }

        // 检查账号是否应该在当前鉴权服务器中处理
        var position = HashCodeHelper.MurmurHash3(userName) % self.AuthenticationCount;
        if (self.Position != position)
        {
            // 连接非法服务器地址
            return 1005;
        }

        var userNameHashCode = userName.GetHashCode();
        var scene = self.Scene;

        // 利用协程锁 解决异步原子性问题
        using (var @lock = await scene.CoroutineLockComponent.Wait((int)LockType.Authentication_RegisterLock, userNameHashCode))
        {
            // 利用缓存来减少频繁请求数据或缓存的压力
            if (self.CacheAccountList.TryGetValue(userName, out var account))
            {
                Log.Info($"[Register] 缓存中的数据");
                // 代表用户已经存在
                return 1002;
            }

            Log.Info($"[Register] 数据库中的数据");
            // 2、数据库查询账号是否存在
            var worldDatabase = scene.World.Database;
            bool isExist = await worldDatabase.Exist<Account>(d => d.Username == userName);

            if (isExist)
            {
                // 代表用户已经存在
                return 1002;
            }

            // 3、执行到这里 表示数据库或缓存没有该账号的注册信息 需要创建一个
            account = Entity.Create<Account>(scene, true, true);
            account.Username = userName;
            account.Password = password;
            account.CreateTime = TimeHelper.Now;

            // 4、写入实体到数据库中
            await worldDatabase.Save(account);
            // 5、把账号缓存到字典中
            self.CacheAccountList.TryAdd(userName, account);
            // 添加TimeOut组件用来定时清除账号缓存
            account.AddComponent<AccountTimeOutComponent>().TimeOut(4000);

            Log.Info($"[Register] source: [{source}]注册一个账号 用户名: [{userName}] 账户ID: [{account.Id}]");
            // 代表注册成功
            return 0;
        }
    }

    /// <summary>
    /// 删除鉴权服务器账号缓存
    /// </summary>
    /// <param name="self"></param>
    /// <param name="userName">账户名</param>
    /// <param name="isDispose">是否销毁</param>
    internal static void RemoveCache(this AuthenticationComponent self, string userName, bool isDispose)
    {
        if (!self.CacheAccountList.Remove(userName, out var account))
        {
            return;
        }

        Log.Info($"[RemoveCache] Remove cache source: 用户名: [{userName}] 账号缓存字典数量: {self.CacheAccountList.Count}");

        if (isDispose)
        {
            account.Dispose();
        }
    }

    /// <summary>
    /// 删除登录账号缓存
    /// </summary>
    /// <param name="self"></param>
    /// <param name="key">缓存Key</param>
    /// <param name="isDispose">是否销毁</param>
    internal static void RemoveLoginCache(this AuthenticationComponent self, string key, bool isDispose)
    {
        if (!self.CacheLoginAccountList.Remove(key, out var cacheInfo))
        {
            return;
        }

        Log.Info($"[RemoveLoginCache] Remove Login cache source: key: [{key}] 账号缓存字典数量: {self.CacheLoginAccountList.Count}");

        if (isDispose)
        {
            cacheInfo.Dispose();
        }
    }

    /// <summary>
    /// 移除账号
    /// </summary>
    /// <param name="self"></param>
    /// <param name="accountID">账号ID</param>
    /// <param name="source">移除原因/来源</param>
    /// <returns></returns>
    internal static async FTask<uint> Remove(this AuthenticationComponent self, long accountID, string source)
    {
        var scene = self.Scene;
        // 这里没必要加协程锁
        using (var @lock = await scene.CoroutineLockComponent.Wait((int)LockType.Authentication_RemoveLock, accountID))
        {
            // 2、数据库查询账号是否存在
            var worldDatabase = scene.World.Database;
            await worldDatabase.Remove<Account>(accountID);
            Log.Info($"[Remove] source: [{source}]移除账号成功accountID: {accountID}");
            return 0;
        }
    }
}