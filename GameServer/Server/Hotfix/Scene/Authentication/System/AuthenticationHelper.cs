using Fantasy;
using Fantasy.Async;
using Fantasy.Platform.Net;

namespace Hotfix;

public static class AuthenticationHelper
{
    /// <summary>
    /// 通过账户ID 计算Gate的外网地址
    /// </summary>
    /// <param name="accountId">账户ID</param>
    /// <returns>外网Ip和端口号</returns>
    public static (string outerIp, int outerPort, uint sceneId) GetOuterIp(long accountId)
    {
        // 通过配置表等方式获取Gate服务器组的信息
        var gateCfgList = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
        // 通过当前账号的ID拿到要分配的Gate服务器
        var gatePosition = accountId % gateCfgList.Count;
        // 通过计算出来的位置下标 拿到Gate服务器的配置
        var gateCfg = gateCfgList[(int)gatePosition];
        // 通过Gate的SceneConfig文件拿到外网ip地址和端口号
        var processConfig = ProcessConfigData.Instance.Get(gateCfg.ProcessConfigId);
        var machineConfig = MachineConfigData.Instance.Get(processConfig.MachineId);
        return (machineConfig.OuterIP, gateCfg.OuterPort, gateCfg.Id);
    }

    /// <summary>
    /// 登录账号
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public static async FTask<(uint errorCode, long accountId)> Login(Scene scene, string username, string password)
        => await scene.GetComponent<AuthenticationComponent>().Login(username, password);

    /// <summary>
    /// 注册新账号
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="source">注册来源</param>
    /// <returns></returns>
    public static async FTask<uint> Register(Scene scene, string username, string password, string source)
        => await scene.GetComponent<AuthenticationComponent>().Register(username, password, source);

    /// <summary>
    /// 删除登录账号缓存 仅供内部调用 不明白原理不要调用 否则后果自负
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="key">缓存Key</param>
    /// <param name="isDispose">是否销毁</param>
    internal static void RemoveLoginCache(Scene scene, string key, bool isDispose)
        => scene.GetComponent<AuthenticationComponent>().RemoveLoginCache(key, isDispose);

    /// <summary>
    /// 移除鉴权服务器账号缓存
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="userName">账户名</param>
    /// <param name="isDispose">是否销毁</param>
    public static void RemoveCache(Scene scene, string userName, bool isDispose)
        => scene.GetComponent<AuthenticationComponent>().RemoveCache(userName, isDispose);

    /// <summary>
    /// 移除账号
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="accountID">账户ID</param>
    /// <param name="source">移除原因/来源</param>
    /// <returns></returns>
    public static async FTask<uint> Register(Scene scene, long accountID, string source)
        => await scene.GetComponent<AuthenticationComponent>().Remove(accountID, source);
}