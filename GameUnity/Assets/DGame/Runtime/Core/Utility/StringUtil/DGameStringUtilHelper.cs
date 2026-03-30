using System;
using System.Text;

namespace DGame
{
    /// <summary>
    /// 字符串工具帮助实现类
    /// </summary>
    public class DGameStringUtilHelper : Utility.StringUtil.IStringUtilHelper
    {
        private const int STRING_BUILDER_CAPACITY = 1024;

        [ThreadStatic]
        private static StringBuilder m_cacheStringBuilder = null;

        private static void CheckCachedStringBuilder()
        {
            if (m_cacheStringBuilder == null)
            {
                m_cacheStringBuilder = new StringBuilder(STRING_BUILDER_CAPACITY);
            }
        }

        /// <summary>
        /// 获取格式化字符串。
        /// </summary>
        /// <typeparam name="T">字符串参数的类型。</typeparam>
        /// <param name="format">字符串格式。</param>
        /// <param name="arg">字符串参数。</param>
        /// <returns>格式化后的字符串。</returns>
        public string Format<T>(string format, T arg)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6,
            T7 arg7, T8 arg8)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6,
            T7 arg7, T8 arg8, T9 arg9)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string format, T1 arg1, T2 arg2, T3 arg3,
            T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string format, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13,
            T14 arg14, T15 arg15)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
            return m_cacheStringBuilder.ToString();
        }

        public string Format<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string format, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13,
            T14 arg14, T15 arg15, T16 arg16)
        {
            if (format == null)
            {
                throw new DGameException("Format is invalid.");
            }

            CheckCachedStringBuilder();
            m_cacheStringBuilder.Length = 0;
            m_cacheStringBuilder.AppendFormat(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
            return m_cacheStringBuilder.ToString();
        }
    }
}