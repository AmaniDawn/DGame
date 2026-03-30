using System;

namespace DGame
{
    public static partial class Utility
    {
        public interface IJsonHelper
        {
            /// <summary>
            /// 将对象序列化成 Json 字符串
            /// </summary>
            /// <param name="obj">要序列化的对象</param>
            /// <returns></returns>
            string ToJson(object obj);

            /// <summary>
            /// 将 Json 字符串反序列化成对象
            /// </summary>
            /// <param name="json">Json字符串</param>
            /// <typeparam name="T">对象类型</typeparam>
            /// <returns></returns>
            T ToObject<T>(string json);

            /// <summary>
            /// 将 Json 字符串反序列化成对象
            /// </summary>
            /// <param name="json">Json字符串</param>
            /// <param name="objectType">对象类型</param>
            /// <returns></returns>
            object ToObject(string json, Type objectType);

            /// <summary>
            /// 将 Json 字符串反序列化成对象 并填充到obj里
            /// </summary>
            /// <param name="json">Json字符串</param>
            /// <param name="obj">对象</param>
            /// <returns></returns>
            void FromJsonOverwrite(string json, object obj);
        }
    }
}