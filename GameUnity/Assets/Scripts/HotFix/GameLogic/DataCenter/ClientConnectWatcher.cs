using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using YooAsset;

namespace GameLogic
{
    /// <summary>
    /// 网络连接监控器状态。
    /// </summary>
    public enum ClientConnectWatcherStatus
    {
        /// <summary>
        /// 初始状态
        /// </summary>
        StatusInit,

        /// <summary>
        /// 自动重连中
        /// </summary>
        StatusReconnectAuto,

        /// <summary>
        /// 等待玩家确认重连
        /// </summary>
        StatusReconnectConfirm,

        /// <summary>
        /// 检查客户端版本中
        /// </summary>
        StatusCheckingClientVersion,

        /// <summary>
        /// 等待退出
        /// </summary>
        StatusWaitExit
    }

    /// <summary>
    /// 网络连接监控器，负责检测网络断开并处理自动重连。
    /// </summary>
    public sealed class ClientConnectWatcher
    {
        // 自动重连次数
        private readonly int AUTO_RECONNECT_MAX_COUNT = 2;
        // 客户端消息响应超时时间
        private readonly float GAME_CLIENT_MSG_TIME_OUT = 5f;

        private GameClient m_client;
        private ClientConnectWatcherStatus m_status = ClientConnectWatcherStatus.StatusInit;
        private float m_statusTime;
        private int m_reconnectCount;
        private int m_disconnectReason;
        private bool m_enabled;
        private CancellationTokenSource m_versionCheckCts;  // 用于取消检测版本的异步操作

        /// <summary>
        /// 获取或设置是否启用网络监控。
        /// </summary>
        public bool Enabled
        {
            get => m_enabled;
            set
            {
                if (value != m_enabled)
                {
                    m_enabled = value;

                    if (m_enabled)
                    {
                        OnEnable();
                    }
                    else
                    {
                        OnDisable();
                    }
                }
            }
        }

        /// <summary>
        /// 获取或设置当前监控器状态。
        /// </summary>
        public ClientConnectWatcherStatus Status
        {
            get => m_status;
            set
            {
                if (value != m_status)
                {
                    m_status = value;
                    m_statusTime = GameTime.UnscaledTime;
                }
            }
        }

        /// <summary>
        /// 初始化网络连接监控器。
        /// </summary>
        /// <param name="client">游戏客户端实例</param>
        public ClientConnectWatcher(GameClient client)
        {
            m_client = client;
            m_statusTime = GameTime.UnscaledTime;
            m_status = ClientConnectWatcherStatus.StatusInit;
        }

        private void OnEnable()
        {
            Reset();
        }

        private void OnDisable()
        {
            Reset();
        }

        private void CancelVersionCheck()
        {
            m_versionCheckCts?.Cancel();
            m_versionCheckCts?.Dispose();
            m_versionCheckCts = null;
        }

        /// <summary>
        /// 重置监控器状态，取消正在进行的版本检查。
        /// </summary>
        public void Reset()
        {
            CancelVersionCheck();
            Status = ClientConnectWatcherStatus.StatusInit;
            m_reconnectCount = 0;
        }

        /// <summary>
        /// 每帧更新监控器状态，处理网络重连逻辑。
        /// </summary>
        public void Update()
        {
            if (!m_enabled || m_client.IsStatusEnter)
            {
                return;
            }

            switch (m_status)
            {
                case ClientConnectWatcherStatus.StatusInit:
                    UpdateOnInitStatus();
                    break;

                case ClientConnectWatcherStatus.StatusReconnectAuto:
                    UpdateOnReconnectAutoStatus();
                    break;

                case ClientConnectWatcherStatus.StatusReconnectConfirm:
                    UpdateOnReconnectConfirmStatus();
                    break;

                case ClientConnectWatcherStatus.StatusWaitExit:
                    UpdateOnWaitExitAutoStatus();
                    break;

                case ClientConnectWatcherStatus.StatusCheckingClientVersion:
                    UpdateOnCheckingClientVersionStatus();
                    break;
            }
        }

        private void UpdateOnCheckingClientVersionStatus()
        {
        }

        /// <summary>
        /// 触发重连操作，仅在等待玩家确认重连状态下有效。
        /// </summary>
        public void Reconnect()
        {
            if (m_status == ClientConnectWatcherStatus.StatusReconnectConfirm)
            {
                Status = ClientConnectWatcherStatus.StatusReconnectAuto;
            }
        }

        private void UpdateOnWaitExitAutoStatus()
        {
        }

        private void UpdateOnReconnectConfirmStatus()
        {
        }

        private void UpdateOnReconnectAutoStatus()
        {
            if (m_client.IsStatusEnter)
            {
                Status = ClientConnectWatcherStatus.StatusInit;
                m_reconnectCount = 0;
                return;
            }
            if (m_statusTime + GAME_CLIENT_MSG_TIME_OUT < GameTime.UnscaledTime)
            {
                DLogger.Error("UpdateOnReconnectAuto timeout: {0}", GAME_CLIENT_MSG_TIME_OUT);
                // 切换状态回默认状态 下一帧继续判断是需要手动重连还是自动重连
                Status = ClientConnectWatcherStatus.StatusInit;
            }
        }

        private void UpdateOnInitStatus()
        {
            if (m_reconnectCount < AUTO_RECONNECT_MAX_COUNT)
            {
                // 自动重连
                if (m_reconnectCount == 0)
                {
                    m_disconnectReason = m_client.LastNetErrorCode;
                }
                Status = ClientConnectWatcherStatus.StatusCheckingClientVersion;
                m_reconnectCount++;
                // 检查客户端版本
                CheckClientVersion().Forget();
            }
            else
            {
                // 玩家手动确认重连
                Status = ClientConnectWatcherStatus.StatusReconnectConfirm;
                m_reconnectCount++;
            }
        }

        /// <summary>
        /// 检查客户端版本
        /// </summary>
        public async UniTaskVoid CheckClientVersion()
        {
            m_versionCheckCts = new CancellationTokenSource();

            try
            {
                var operation = GameModule.ResourceModule.RequestPackageVersionAsync();
                await operation.ToUniTask();

                if (operation.Status != EOperationStatus.Succeed)
                {
                    DLogger.Error("check package version fail: {0}", operation.Error);
                }
                else if (operation.PackageVersion != GameModule.ResourceModule.PackageVersion)
                {
                    DLogger.Log("check package new version: {0} -> {1}",
                        GameModule.ResourceModule.PackageVersion, operation.PackageVersion);

                    // TODO: 提示版本更新
                }
            }
            catch (OperationCanceledException)
            {
                DLogger.Log("cancel check package version");
                return;
            }
            catch (Exception e)
            {
                DLogger.Error("check package version fail: {0}", e.Message);
            }
            finally
            {
                m_versionCheckCts?.Dispose();
                m_versionCheckCts = null;
            }

            // 只有状态还在 CheckingVersion 时才继续（防止竞态）
            // 版本检测完成，执行重连
            if (m_status == ClientConnectWatcherStatus.StatusCheckingClientVersion)
            {
                Status = ClientConnectWatcherStatus.StatusReconnectAuto;
                m_client.Reconnect();
            }
        }
    }
}