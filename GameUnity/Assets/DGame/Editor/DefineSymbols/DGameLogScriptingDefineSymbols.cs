using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    public static partial class ScriptingDefineSymbolsTools
    {
        private const string ENABLE_DGAME_LOG_SCRIPTING_DEFINE_SYMBOL = "ENABLE_DGAME_LOG";

        private const string ENABLE_DGAME_DEBUG_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL =
            "ENABLE_DGAME_DEBUG_AND_ABOVE_LOG";

        private const string ENABLE_DGAME_INFO_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL =
            "ENABLE_DGAME_INFO_AND_ABOVE_LOG";

        private const string ENABLE_DGAME_WARNING_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL =
            "ENABLE_DGAME_WARNING_AND_ABOVE_LOG";

        private const string ENABLE_DGAME_ERROR_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL =
            "ENABLE_DGAME_ERROR_AND_ABOVE_LOG";

        private const string ENABLE_DGAME_FATAL_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL =
            "ENABLE_DGAME_FATAL_AND_ABOVE_LOG";

        private const string ENABLE_DGAME_DEBUG_LOG_SCRIPTING_DEFINE_SYMBOL = "ENABLE_DGAME_DEBUG_LOG";
        private const string ENABLE_DGAME_INFO_LOG_SCRIPTING_DEFINE_SYMBOL = "ENABLE_DGAME_INFO_LOG";
        private const string ENABLE_DGAME_WARNING_LOG_SCRIPTING_DEFINE_SYMBOL = "ENABLE_DGAME_WARNING_LOG";
        private const string ENABLE_DGAME_ERROR_LOG_SCRIPTING_DEFINE_SYMBOL = "ENABLE_DGAME_ERROR_LOG";
        private const string ENABLE_DGAME_FATAL_LOG_SCRIPTING_DEFINE_SYMBOL = "ENABLE_DGAME_FATAL_LOG";

        private static readonly string[] m_aboveLogScriptingDefineSymbols = new string[]
        {
            ENABLE_DGAME_DEBUG_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_INFO_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_WARNING_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_ERROR_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_FATAL_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL
        };

        private static readonly string[] m_specifyLogScriptingDefineSymbols = new string[]
        {
            ENABLE_DGAME_DEBUG_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_INFO_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_WARNING_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_ERROR_LOG_SCRIPTING_DEFINE_SYMBOL,
            ENABLE_DGAME_FATAL_LOG_SCRIPTING_DEFINE_SYMBOL
        };

        /// <summary>
        /// 禁用所有日志脚本宏定义。
        /// </summary>
        [MenuItem("DGame Tools/日志系统/禁用所有的日志输出", false, 41)]
        public static void DisableAllLogs()
        {
            ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(ENABLE_DGAME_LOG_SCRIPTING_DEFINE_SYMBOL);

            foreach (string specifyLogScriptingDefineSymbol in m_specifyLogScriptingDefineSymbols)
            {
                ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(specifyLogScriptingDefineSymbol);
            }

            foreach (string aboveLogScriptingDefineSymbol in m_aboveLogScriptingDefineSymbols)
            {
                ScriptingDefineSymbolsTools.DisableScriptingDefineSymbol(aboveLogScriptingDefineSymbol);
            }
        }

        /// <summary>
        /// 开启所有日志脚本宏定义。
        /// </summary>
        [MenuItem("DGame Tools/日志系统/开启所有的日志输出", false, 42)]
        public static void EnableAllLogs()
        {
            DisableAllLogs();
            ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(ENABLE_DGAME_LOG_SCRIPTING_DEFINE_SYMBOL);
        }

        /// <summary>
        /// 开启调试及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem("DGame Tools/日志系统/开启调试及以上级别的日志输出", false, 43)]
        public static void EnableDebugAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(ENABLE_DGAME_DEBUG_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL);
        }

        /// <summary>
        /// 开启信息及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem("DGame Tools/日志系统/开启信息及以上级别的日志输出", false, 44)]
        public static void EnableInfoAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(ENABLE_DGAME_INFO_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL);
        }

        /// <summary>
        /// 开启警告及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem("DGame Tools/日志系统/开启警告及以上级别的日志输出", false, 45)]
        public static void EnableWarningAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(ENABLE_DGAME_WARNING_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL);
        }

        /// <summary>
        /// 开启错误及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem("DGame Tools/日志系统/开启错误及以上级别的日志输出", false, 46)]
        public static void EnableErrorAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(ENABLE_DGAME_ERROR_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL);
        }

        /// <summary>
        /// 开启严重错误及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem("DGame Tools/日志系统/开启严重错误及以上级别的日志输出", false, 47)]
        public static void EnableFatalAndAboveLogs()
        {
            SetAboveLogScriptingDefineSymbol(ENABLE_DGAME_FATAL_AND_ABOVE_LOG_SCRIPTING_DEFINE_SYMBOL);
        }

        /// <summary>
        /// 设置日志脚本宏定义。
        /// </summary>
        /// <param name="aboveLogScriptingDefineSymbol">要设置的日志脚本宏定义。</param>
        public static void SetAboveLogScriptingDefineSymbol(string aboveLogScriptingDefineSymbol)
        {
            if (string.IsNullOrEmpty(aboveLogScriptingDefineSymbol))
            {
                return;
            }

            foreach (string i in m_aboveLogScriptingDefineSymbols)
            {
                if (i == aboveLogScriptingDefineSymbol)
                {
                    DisableAllLogs();
                    ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(aboveLogScriptingDefineSymbol);
                    return;
                }
            }
        }

        /// <summary>
        /// 设置日志脚本宏定义。
        /// </summary>
        /// <param name="specifyLogScriptingDefineSymbols">要设置的日志脚本宏定义。</param>
        public static void SetSpecifyLogScriptingDefineSymbols(string[] specifyLogScriptingDefineSymbols)
        {
            if (specifyLogScriptingDefineSymbols == null || specifyLogScriptingDefineSymbols.Length <= 0)
            {
                return;
            }

            bool removed = false;

            foreach (string specifyLogScriptingDefineSymbol in specifyLogScriptingDefineSymbols)
            {
                if (string.IsNullOrEmpty(specifyLogScriptingDefineSymbol))
                {
                    continue;
                }

                foreach (string i in m_specifyLogScriptingDefineSymbols)
                {
                    if (i == specifyLogScriptingDefineSymbol)
                    {
                        if (!removed)
                        {
                            removed = true;
                            DisableAllLogs();
                        }

                        ScriptingDefineSymbolsTools.EnableScriptingDefineSymbol(specifyLogScriptingDefineSymbol);
                        break;
                    }
                }
            }
        }
    }
}