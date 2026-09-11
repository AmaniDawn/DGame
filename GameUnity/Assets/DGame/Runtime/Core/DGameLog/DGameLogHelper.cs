using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;

#endif

namespace DGame
{

    /// <summary>
    /// 日志默认辅助系统
    /// </summary>
    public class DGameLogHelper : DGameLog.ILogHelper
    {
        private enum ELogLevel
        {
            Info,
            Debug,
            Assert,
            Warning,
            Error,
            Exception,
        }

        private const ELogLevel FILTER_LEVEL = ELogLevel.Info;
        private static readonly StringBuilder m_stringBuilder = new StringBuilder(1024);

        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="msg">日志消息</param>
        /// <exception cref="ArgumentOutOfRangeException">框架异常类</exception>
        public void Log(DGameLogLevel level, object msg)
        {
            if (!DLogger.EnableLog)
            {
                return;
            }
            switch (level)
            {
                case DGameLogLevel.Debug:
                    LogImp(ELogLevel.Debug, msg.ToString()); //Utility.StringUtil.Format("<color=#888888>{0}</color>", msg));
                    break;

                case DGameLogLevel.Info:
                    LogImp(ELogLevel.Info, msg.ToString());
                    break;

                case DGameLogLevel.Warning:
                    LogImp(ELogLevel.Warning, msg.ToString());
                    break;

                case DGameLogLevel.Error:
                    LogImp(ELogLevel.Error, msg.ToString());
                    break;

                case DGameLogLevel.Fatal:
                    LogImp(ELogLevel.Exception, msg.ToString());
                    break;

                default:
                    throw new DGameException(msg.ToString());
            }
        }

        /// <summary>
        /// 日志输出实现方法
        /// </summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        private static void LogImp(ELogLevel level, string msg)
        {
            if (level < FILTER_LEVEL)
            {
                return;
            }

            StringBuilder formatSb = GetFormatStringBuilder(level, msg, true);
            
            string logStr = formatSb.ToString();
            // 获取 C# 堆栈 Warning以上级别日志才获取堆栈
            if (level >= ELogLevel.Warning)
            {
                StackFrame[] stackFrames = new StackTrace().GetFrames();

                // ReSharper disable once PossibleNullReferenceException
                for (int i = 0; i < stackFrames.Length; i++)
                {
                    StackFrame frame = stackFrames[i];
                    // ReSharper disable once PossibleNullReferenceException
                    string declaringTypeName = frame.GetMethod().DeclaringType.FullName;
                    string methodName = frame.GetMethod().Name;
                    formatSb.AppendFormat("[{0}::{1}\n", declaringTypeName, methodName);
                }
            }
            switch (level)
            {
                case ELogLevel.Info:
                case ELogLevel.Debug:
                    UnityEngine.Debug.Log(logStr);
                    break;

                case ELogLevel.Assert:
                    UnityEngine.Debug.LogAssertion(logStr);
                    break;

                case ELogLevel.Warning:
                    UnityEngine.Debug.LogWarning(logStr);
                    break;

                case ELogLevel.Error:
                    UnityEngine.Debug.LogError(logStr);
                    break;

                case ELogLevel.Exception:
                    throw new Exception(logStr);
            }
        }

        /// <summary>
        /// 获取格式化的日志输出
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="logStr">日志消息</param>
        /// <param name="showColor">是否需要显示颜色</param>
        /// <returns></returns>
        private static StringBuilder GetFormatStringBuilder(ELogLevel level, string logStr, bool showColor)
        {
            m_stringBuilder.Clear();

            string body = showColor ? ColorizePerLine(logStr, GetBodyColor(level)) : logStr;
            switch (level)
            {
                case ELogLevel.Debug:
                    m_stringBuilder.AppendFormat("<color=#CFCFCF><b>[DEBUG] ► </b></color> - {0}", body);
                    break;
                case ELogLevel.Info:
                    m_stringBuilder.AppendFormat("<color=#CFCFCF><b>[INFO] ► </b></color> - {0}", body);
                    break;
                case ELogLevel.Assert:
                    m_stringBuilder.AppendFormat("<color=#FF00BD><b>[ASSERT] ► </b></color> - {0}", body);
                    break;
                case ELogLevel.Warning:
                    m_stringBuilder.AppendFormat("<color=#FF9400><b>[WARNING] ► </b></color> - {0}", body);
                    break;
                case ELogLevel.Error:
                    m_stringBuilder.AppendFormat("<color=red><b>[ERROR] ► </b></color>- {0}", body);
                    break;
                case ELogLevel.Exception:
                    m_stringBuilder.AppendFormat("<color=red><b>[EXCEPTION] ► </b></color> - {0}", body);
                    break;
            }

            return m_stringBuilder;
        }

        private static string GetBodyColor(ELogLevel level)
            => level switch
            {
                ELogLevel.Debug => "#00FF18",
                ELogLevel.Assert => "green",
                ELogLevel.Warning => "yellow",
                ELogLevel.Error => "red",
                ELogLevel.Exception => "red",
                _ => "#CFCFCF"
            };

        private static string ColorizePerLine(string logStr, string color)
        {
            if(string.IsNullOrEmpty(logStr))
            {
                return logStr;
            }

            if (logStr.IndexOf('\n') < 0)
            {
                return Utility.StringUtil.Format("<color={0}>{1}</color>", color, logStr);
            }

            var sb = new StringBuilder(logStr.Length + 32);
            int start = 0;

            for (int i = 0; i < logStr.Length; i++)
            {
                if (logStr[i] != '\n')
                {
                    continue;
                }

                int lineEnd = i > start && logStr[i - 1] == '\r' ? i - 1 : i;
                sb.Append("<color=").Append(color).Append('>')
                    .Append(logStr, start, lineEnd - start)
                    .Append("</color>")
                    .Append(logStr, lineEnd, i - lineEnd + 1);
                start = i + 1;
            }

            if (start < logStr.Length)
            {
                sb.Append("<color=").Append(color).Append('>')
                    .Append(logStr, start, logStr.Length - start)
                    .Append("</color>");
            }

            return sb.ToString();
        }
    }
}

#if UNITY_EDITOR

namespace DGame
{
    /// <summary>
    /// 日志重定向相关的实用函数。
    /// </summary>
    internal static class LogRedirection
    {
        /// <summary>
        /// 当在 Project 窗口或 Console 窗口双击资源时触发。
        /// </summary>
#if UNITY_6000_1_OR_NEWER
        /// <param name="entityId">双击的资源的 EntityId</param>
#else
        /// <param name="instanceID">双击的资源的实例 ID</param>
#endif
        /// <param name="line">日志对应的行号（如果是从 Console 窗口触发的）</param>
        /// <returns></returns>
        [OnOpenAsset(0)]
#if UNITY_6000_1_OR_NEWER
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            return OnOpenAsset(AssetDatabase.GetAssetPath(entityId), line);
        }
#else
        private static bool OnOpenAsset(int instanceID, int line)
        {
            return OnOpenAsset(AssetDatabase.GetAssetPath(instanceID), line);
        }
#endif

        private static bool OnOpenAsset(string assetPath, int line)
        {
            if (line <= 0)
            {
                return false;
            }

            // 判断资源类型
            if (!assetPath.EndsWith(".cs"))
            {
                return false;
            }

            bool autoFirstMatch = assetPath.Contains("Logger.cs") ||
                                   assetPath.Contains("DGameLogHelper.cs") ||
                                   assetPath.Contains("DGameLog.cs") ||
                                   assetPath.Contains("AssetsLogger.cs") ||
                                   assetPath.Contains("BLogger.cs") ||
                                   assetPath.Contains("DLogger.cs") ||
                                   assetPath.Contains("DGameException.cs");

            var stackTrace = GetStackTrace();
            if (!string.IsNullOrEmpty(stackTrace) && (stackTrace.Contains("[DEBUG]") ||
                                                      stackTrace.Contains("[INFO]") ||
                                                      stackTrace.Contains("[ASSERT]") ||
                                                      stackTrace.Contains("[WARNING]") ||
                                                      stackTrace.Contains("[ERROR]") ||
                                                      stackTrace.Contains("[EXCEPTION]")))

            {
                if (!autoFirstMatch)
                {
                    // var fullPath = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal));
                    // fullPath = $"{fullPath}{assetPath}";
                    var fullPath = UnityEngine.Application.dataPath.Replace("Assets", assetPath).Replace('/', '\\');
                    // 跳转到目标代码的特定行
                    InternalEditorUtility.OpenFileAtLineExternal(fullPath, line);
                    return true;
                }

                // 使用正则表达式匹配at的哪个脚本的哪一行
                var matches = Regex.Match(stackTrace, @"\(at (.+)\)",
                    RegexOptions.IgnoreCase);
                while (matches.Success)
                {
                    var pathLine = matches.Groups[1].Value;

                    if (!pathLine.Contains("Logger.cs") &&
                        !pathLine.Contains("DGameLogHelper.cs") &&
                        !pathLine.Contains("DGameLog.cs") &&
                        !pathLine.Contains("AssetsLogger.cs") &&
                        !pathLine.Contains("BLogger.cs") &&
                        !pathLine.Contains("DLogger.cs"))
                    {
                        var splitIndex = pathLine.LastIndexOf(":", StringComparison.Ordinal);
                        // 脚本路径
                        var path = pathLine.Substring(0, splitIndex);
                        // 行号
                        line = Convert.ToInt32(pathLine.Substring(splitIndex + 1));
                        // var fullPath = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("Assets", StringComparison.Ordinal));
                        // fullPath = $"{fullPath}{path}";
                        var fullPath = UnityEngine.Application.dataPath.Replace("Assets", path).Replace('/', '\\');
                        // 跳转到目标代码的特定行
                        InternalEditorUtility.OpenFileAtLineExternal(fullPath, line);
                        break;
                    }

                    matches = matches.NextMatch();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前日志窗口选中的日志的堆栈信息。
        /// </summary>
        /// <returns>选中日志的堆栈信息实例。</returns>
        private static string GetStackTrace()
        {
            // 通过反射获取ConsoleWindow类
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            // 获取窗口实例
            var fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                var consoleInstance = fieldInfo.GetValue(null);
                if (consoleInstance != null)
                    if (EditorWindow.focusedWindow == (EditorWindow)consoleInstance)
                    {
                        // 获取m_ActiveText成员
                        fieldInfo = consoleWindowType.GetField("m_ActiveText",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        // 获取m_ActiveText的值
                        if (fieldInfo != null)
                        {
                            var activeText = fieldInfo.GetValue(consoleInstance).ToString();
                            return activeText;
                        }
                    }
            }

            return null;
        }
    }
}

#endif
