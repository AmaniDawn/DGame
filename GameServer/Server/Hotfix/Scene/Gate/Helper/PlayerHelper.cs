using Fantasy;
using Fantasy.Async;

// ReSharper disable InconsistentNaming

namespace Hotfix;

public static class PlayerHelper
{
    /// <summary>
    /// 从数据库创建或加载一个游戏账号
    /// <remarks>先检查缓存 再检查数据库</remarks>
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    /// <returns></returns>
    public static FTask<PlayerData> Create(this Scene scene, long accountID, int serverID)
        => scene.GetComponent<PlayerManagerComponent>().Create(accountID, serverID);
    
    /// <summary>
    /// 将玩家账号数据手动添加到管理器组件缓存中
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="playerData"></param>
    public static void Add(this Scene scene, PlayerData playerData)
        => scene.GetComponent<PlayerManagerComponent>().Add(playerData);

    /// <summary>
    /// 获取玩家账号数据
    /// <remarks>不涉及数据库</remarks>
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    public static PlayerData? Get(this Scene scene, long accountID, int serverID)
        => scene.GetComponent<PlayerManagerComponent>().Get(accountID, serverID);

    /// <summary>
    /// 获取玩家账号数据
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    /// <param name="playerData"></param>
    public static bool TryGet(this Scene scene, long accountID, int serverID, out PlayerData playerData)
        => scene.GetComponent<PlayerManagerComponent>().TryGet(accountID, serverID, out playerData);

    /// <summary>
    /// 删除玩家账号缓存数据
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    /// <param name="isDispose"></param>
    public static bool Remove(this Scene scene, long accountID, int serverID, bool isDispose = true)
        => scene.GetComponent<PlayerManagerComponent>().Remove(accountID, serverID, isDispose);
    
    /// <summary>
    /// 删除玩家账号缓存数据
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="playerData"></param>
    /// <param name="isDispose"></param>
    public static bool Remove(this Scene scene, PlayerData playerData, bool isDispose = true)
        => scene.GetComponent<PlayerManagerComponent>().Remove(playerData, isDispose);
}
