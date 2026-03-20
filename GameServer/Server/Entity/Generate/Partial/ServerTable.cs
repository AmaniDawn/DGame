using Fantasy;

namespace GameProto;

/// <summary>
/// 服务器配置表扩展。
/// </summary>
public partial class TbServerConfig
{
    private List<ServerInfo>? m_serverInfoList;

    /// <summary>
    /// 获取由配置表转换后的服务器信息缓存列表。
    /// </summary>
    public static List<ServerInfo> ServerInfoList => m_instance.GetServerInfoList();

    private List<ServerInfo> GetServerInfoList()
    {
        if (m_serverInfoList != null)
        {
            return m_serverInfoList;
        }

        m_serverInfoList = new List<ServerInfo>();
        foreach (var cfg in dataList)
        {
            m_serverInfoList.Add(ToServerInfo(cfg));
        }
        return m_serverInfoList;
    }

    /// <summary>
    /// 将服务器配置记录转换为 <see cref="ServerInfo"/> 实例。
    /// </summary>
    /// <param name="serverConfig">源服务器配置记录。</param>
    /// <returns>转换后的服务器信息。</returns>
    public ServerInfo ToServerInfo(ServerConfig serverConfig)
        => new()
        {
            ServerID = serverConfig.ID,
            Address = serverConfig.Address,
            Port = serverConfig.Port,
            Name = serverConfig.Name,
            Group = serverConfig.Group,
            State = serverConfig.State,
        };

    /// <summary>
    /// 判断指定服务器配置是否存在。
    /// </summary>
    /// <param name="serverId">要检查的服务器 ID。</param>
    /// <returns>如果存在返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsExist(int serverId) => m_instance._dataMap.ContainsKey(serverId);
}
