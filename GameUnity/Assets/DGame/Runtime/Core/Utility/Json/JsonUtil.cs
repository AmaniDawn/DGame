using System;

namespace DGame
{
    public static partial class Utility
    {
        public static class JsonUtil
        {
            private static IJsonHelper m_jsonHelper;

            public static void SetJsonHelper(IJsonHelper helper)
                => m_jsonHelper = helper;

            /// <summary>
            /// 将对象序列化成 Json 字符串
            /// </summary>
            /// <param name="obj">要序列化的对象</param>
            /// <returns></returns>
            public static string ToJson(object obj)
            {
                if (m_jsonHelper == null)
                {
                    throw new DGameException("JsonHelper 辅助器无效");
                }

                try
                {
                    return m_jsonHelper.ToJson(obj);
                }
                catch (Exception e)
                {
                    if (e is DGameException)
                    {
                        throw;
                    }

                    throw new DGameException(StringUtil.Format("不能转换成Json字符串，错误：'{0}'", e), e);
                }
            }

            /// <summary>
            /// 将 Json 字符串反序列化成对象
            /// </summary>
            /// <param name="json">Json字符串</param>
            /// <typeparam name="T">对象类型</typeparam>
            /// <returns></returns>
            public static T ToObject<T>(string json)
            {
                if (m_jsonHelper == null)
                {
                    throw new DGameException("JsonHelper 辅助器无效");
                }

                try
                {
                    return m_jsonHelper.ToObject<T>(json);
                }
                catch (Exception e)
                {
                    if (e is DGameException)
                    {
                        throw;
                    }

                    throw new DGameException(StringUtil.Format("不能转换成object，错误：'{0}'", e), e);
                }
            }

            /// <summary>
            /// 将 Json 字符串反序列化成对象
            /// </summary>
            /// <param name="json">Json字符串</param>
            /// <param name="objectType">对象类型</param>
            /// <returns></returns>
            public static object ToObject(string json, Type objectType)
            {
                if (m_jsonHelper == null)
                {
                    throw new DGameException("JsonHelper 辅助器无效");
                }

                try
                {
                    return m_jsonHelper.ToObject(json, objectType);
                }
                catch (Exception e)
                {
                    if (e is DGameException)
                    {
                        throw;
                    }

                    throw new DGameException(StringUtil.Format("不能转换成object，错误：'{0}'", e), e);
                }
            }

            /// <summary>
            /// 将 Json 字符串反序列化成对象 并填充到obj里
            /// </summary>
            /// <param name="json">Json字符串</param>
            /// <param name="obj">对象</param>
            /// <returns></returns>
            public static void FromJsonOverwrite(string json, object obj)
            {
                if (m_jsonHelper == null)
                {
                    throw new DGameException("JsonHelper 辅助器无效");
                }

                try
                {
                    m_jsonHelper.FromJsonOverwrite(json, obj);
                }
                catch (Exception e)
                {
                    if (e is DGameException)
                    {
                        throw;
                    }

                    throw new DGameException(StringUtil.Format("不能转换成object，错误：'{0}'", e), e);
                }
            }
        }
    }
}