using System;
using DGame;

namespace GameLogic
{
    /// <summary>
    /// 网络客户端状态。
    /// </summary>
    public enum GameClientStatus
    {
        /// <summary>
        /// 初始化
        /// </summary>
        StatusInit,

        /// <summary>
        /// 连接成功服务器
        /// </summary>
        StatusConnected,

        /// <summary>
        /// 重新连接
        /// </summary>
        StatusReconnect,

        /// <summary>
        /// 断开连接
        /// </summary>
        StatusClose,

        /// <summary>
        /// 登录中
        /// </summary>
        StatusLogin,

        /// <summary>
        /// 注册
        /// </summary>
        StatusRegister,

        /// <summary>
        /// AccountLogin成功，进入服务器了
        /// </summary>
        StatusEnter,
    }

    public sealed class GameClient : Singleton<GameClient>
    {
        /// <summary>
        /// 网络连接状态
        /// </summary>
        public GameClientStatus Status { get; set; } = GameClientStatus.StatusInit;

        private string m_lastAddress = string.Empty;
        private int m_lastPort = 0;
        private float m_lastLogDisconnectErrTime = 0f;

        /// <summary>
        /// 同步连接网络
        /// </summary>
        /// <param name="address">ip地址</param>
        /// <param name="port">端口号</param>
        /// <param name="reconnect">是否重连</param>
        public void Connect(string address, int port, bool reconnect = false)
        {
            if (Status == GameClientStatus.StatusConnected || Status == GameClientStatus.StatusLogin ||
                Status == GameClientStatus.StatusEnter)
            {
                return;
            }

            if (reconnect)
            {

            }

            m_lastAddress = address;
            m_lastPort = port;
            Status = reconnect ? GameClientStatus.StatusReconnect : GameClientStatus.StatusInit;
        }

        private void OnConnectComplete()
        {
            Status = GameClientStatus.StatusConnected;
            DLogger.Info("[GameClient] Connected to server success");
        }

        private void OnConnectFail()
        {
            Status = GameClientStatus.StatusClose;
            DLogger.Info("[GameClient] Connected to server fail");
        }

        private void OnConnectDisconnect()
        {
            Status = GameClientStatus.StatusClose;
            DLogger.Info("[GameClient] Disconnected to server");
        }

        public bool IsStatusCanSendMsg(int protocolCode)
        {
            bool canSend = false;

            // 自行根据类型处理是否可以发送消息给服务器
            if (Status == GameClientStatus.StatusLogin)
            {
                canSend = true;
            }
            if (Status == GameClientStatus.StatusRegister)
            {
                canSend = true;
            }
            if (Status == GameClientStatus.StatusEnter)
            {
                canSend = true;
            }

            if (!canSend)
            {
                float nowTime = GameTime.UnscaledTime;
                if (m_lastLogDisconnectErrTime + 5f < nowTime)
                {
                    DLogger.Error($"[GameClient] GameClient disconnect, send msg failed, protocolCode[{protocolCode}]");
                    m_lastLogDisconnectErrTime = nowTime;
                }
            }

            return canSend;
        }

        /// <summary>
        /// 断开当前网络连接
        /// </summary>
        public void Disconnect()
        { }

        /// <summary>
        /// 网络重连
        /// </summary>
        public void Reconnect()
        {
            if (string.IsNullOrEmpty(m_lastAddress) || m_lastPort <= 0)
            {
                return;
            }
        }

        /// <summary>
        /// 网络状态是否可以发送消息给服务器
        /// </summary>
        /// <param name="protocolCode">网络协议码</param>
        /// <returns></returns>
        public bool IsStatusCanSendMsg(uint protocolCode)
        {
            bool canSend = false;

            if (!canSend)
            {
                float nowTime = GameTime.UnscaledTime;
                if (m_lastLogDisconnectErrTime + 5f < nowTime)
                {
                    DLogger.Error($"[GameClient] GameClient disconnect, send msg failed, protocolCode[{protocolCode}]");
                    m_lastLogDisconnectErrTime = nowTime;
                }
            }

            return canSend;
        }

        /// <summary>
        /// 发送消息到服务器（不需要响应）
        /// </summary>
        /// <typeparam name="T">消息类型，必须实现 IMessage 接口</typeparam>
        /// <param name="message">要发送的消息实例</param>
        /// <param name="rpcID">RPC ID</param>
        /// <param name="routeID">路由 ID，用于消息路由</param>
        public void Send<T>(T message, uint rpcID = 0, long routeID = 0)
        {
            if (IsStatusCanSendMsg(rpcID))
            {
            }
        }

        /// <summary>
        /// 发送 RPC 请求到服务器并等待响应
        /// </summary>
        /// <typeparam name="T">请求类型，必须实现 IRequest 接口</typeparam>
        /// <param name="request">请求实例</param>
        /// <param name="routeId">路由 ID，用于消息路由</param>
        /// <returns>服务器的响应结果</returns>
        public void Call<T>(T request, long routeId = 0)
        {
        }

        /// <summary>
        /// 注册网络消息处理器
        /// </summary>
        /// <param name="protocolCode">协议码</param>
        /// <param name="ctx">消息处理回调</param>
        public void RegisterMsgHandler(uint protocolCode, Action ctx)
        {
        }

        /// <summary>
        /// 取消注册网络消息处理器
        /// </summary>
        /// <param name="protocolCode">协议码</param>
        /// <param name="ctx">消息处理回调</param>
        public void UnRegisterMsgHandler(uint protocolCode, Action ctx)
        {
        }
    }
}