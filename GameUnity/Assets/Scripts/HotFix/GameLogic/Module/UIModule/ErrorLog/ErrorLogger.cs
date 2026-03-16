using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 错误日志记录器，用于捕获并显示Unity日志错误
    /// </summary>
    public class ErrorLogger : IDisposable
    {
        private readonly UIModule m_uiModule;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="uiModule">UI模块实例</param>
        public ErrorLogger(UIModule uiModule)
        {
            m_uiModule = uiModule;
            Application.logMessageReceived += LogHandler;
        }

        private void LogHandler(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                string des = $"客户端报错, \n#内容#：---{condition} \n#位置#：---{stacktrace}";
                m_uiModule.ShowWindowAsync<LogUI>(des);
            }
        }

        /// <summary>
        /// 释放资源，取消日志监听
        /// </summary>
        public void Dispose()
        {
            Application.logMessageReceived -= LogHandler;
        }
    }
}