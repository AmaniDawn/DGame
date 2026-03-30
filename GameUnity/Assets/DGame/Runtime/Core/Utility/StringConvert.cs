using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DGame
{
    public static partial class Utility
    {
        public static class StringConvert
        {
            /// <summary>
            /// 正则表达式
            /// </summary>
            private static readonly Regex REGEX = new Regex(@"\{[-+]?[0-9]+\.?[0-9]*\}", RegexOptions.IgnoreCase);

            /// <summary>
            /// string 转换为 Bool
            /// </summary>
            /// <param name="value"></param>
            /// <returns>不等于0则为真 反之为假</returns>
            public static bool StringToBool(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Input string cannot be null or empty.");
                }

                if (int.TryParse(value, out int intValue))
                {
                    return intValue != 0;
                }

                return false;
            }

            /// <summary>
            /// string 转换成为数值
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <returns></returns>
            public static T StringToValue<T>(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Input string cannot be null or empty.");
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }

            /// <summary>
            /// string 转换成为数值数组
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <param name="separator">分隔符</param>
            /// <returns></returns>
            public static T[] StringToValueArray<T>(string value, char separator)
            {
                return StringToValue<T>(value, separator).ToArray();
            }

            /// <summary>
            /// string 转换成为数值列表
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <param name="separator">分隔符</param>
            /// <returns></returns>
            public static List<T> StringToValue<T>(string value, char separator)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    List<T> arr = new List<T>();
                    var span = value.AsSpan();

                    while (true)
                    {
                        int index = span.IndexOf(separator);

                        if (index == -1)
                        {
                            arr.Add((T)Convert.ChangeType(span.ToString(), typeof(T)));
                            break;
                        }

                        arr.Add((T)Convert.ChangeType(span.Slice(0, index).ToString(), typeof(T)));
                        span = span.Slice(index + 1);
                    }

                    return arr;
                }

                throw new ArgumentException("Input string cannot be null or empty.");
            }

            /// <summary>
            /// string 转换成为字符串列表
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="value"></param>
            /// <param name="separator">分隔符</param>
            /// <returns></returns>
            public static List<string> StringToValue(string value, char separator)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    List<string> arr = new List<string>();
                    var span = value.AsSpan();

                    while (true)
                    {
                        int index = span.IndexOf(separator);

                        if (index == -1)
                        {
                            arr.Add(span.ToString());
                            break;
                        }

                        arr.Add(span.Slice(0, index).ToString());
                        span = span.Slice(index + 1);
                    }

                    return arr;
                }
                else
                {
                    throw new ArgumentException("Input string cannot be null or empty.");
                }
            }

            /// <summary>
            /// string 进行二次切割
            /// </summary>
            /// <param name="value"></param>
            /// <param name="separator1">单个数据里元素分隔符</param>
            /// <param name="separator2">数据之间的分隔符</param>
            /// <returns></returns>
            public static List<T[]> StringToValueList<T>(string value, char separator1, char separator2)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    List<T[]> result = new List<T[]>();
                    var span = value.AsSpan();

                    while (true)
                    {
                        int index = span.IndexOf(separator2);

                        if (index == -1)
                        {
                            if (span.Length > 0)
                            {
                                result.Add(StringToValueSpan<T>(span, separator1));
                            }

                            break;
                        }

                        if (index > 0)
                        {
                            result.Add(StringToValueSpan<T>(span.Slice(0, index), separator1));
                        }

                        span = span.Slice(index + 1);
                    }

                    return result;
                }

                throw new ArgumentException("Input string cannot be null or empty.");
            }

            private static T[] StringToValueSpan<T>(ReadOnlySpan<char> span, char separator)
            {
                if (span.Length <= 0)
                {
                    throw new ArgumentException("Input string cannot be null or empty.");
                }

                var tempSpan = span;
                List<T> arr = new List<T>();

                while (true)
                {
                    int index = tempSpan.IndexOf(separator);

                    if (index == -1)
                    {
                        arr.Add((T)Convert.ChangeType(tempSpan.ToString(), typeof(T)));
                        break;
                    }

                    arr.Add((T)Convert.ChangeType(tempSpan.Slice(0, index).ToString(), typeof(T)));
                    tempSpan = tempSpan.Slice(index + 1);
                }

                return arr.ToArray();
            }

            #region 转枚举

            /// <summary>
            /// 转换为枚举
            /// 枚举索引转换为枚举类型
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="index"></param>
            /// <param name="ret"></param>
            /// <returns></returns>
            public static bool TryParseToEnum<T>(string index, out T ret) where T : struct, Enum
            {
                ret = default;

                if (Enum.TryParse<T>(index, out ret))
                {
                    return true;
                }

                if (int.TryParse(index, out int enumIndex))
                {
                    return TryParseToEnum(enumIndex, out ret);
                }

                return false;
            }

            /// <summary>
            /// 转换为枚举
            /// 枚举索引转换为枚举类型
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="index"></param>
            /// <param name="ret"></param>
            /// <returns></returns>
            public static bool TryParseToEnum<T>(int index, out T ret) where T : struct, Enum
            {
                if (Enum.IsDefined(typeof(T), index))
                {
                    ret = (T)(object)index; //(T)Enum.ToObject(typeof(T), index);//(T)(object)index;
                    return true;
                    // throw new ArgumentException($"Enum {typeof(T)} is not defined index {index}");
                }

                ret = default;
                return false;
                // return (T)Enum.ToObject(typeof(T), index);
            }

            #endregion

            #region 秒转时间字符串

            /// <summary>
            /// 秒转时间字符串
            /// </summary>
            /// <param name="totalSeconds">总秒数</param>
            /// <param name="hideLeadingZero">是否忽略显示0</param>
            /// <param name="separators">时 分 秒 分割符</param>
            /// <returns></returns>
            public static string SecondConvertToTimeString(int totalSeconds, bool hideLeadingZero = false,
                params string[] separators)
            {
                if (totalSeconds < 0) totalSeconds = 0;

                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds % 3600) / 60;
                int seconds = totalSeconds % 60;

                // 处理分隔符，提供默认值
                string hourSeparator = separators.Length > 0 ? separators[0] : ":";
                string minuteSeparator = separators.Length > 1 ? separators[1] : ":";
                string secondSeparator = separators.Length > 2 ? separators[2] : "";

                if (!hideLeadingZero)
                {
                    return $"{hours:D2}{hourSeparator}{minutes:D2}{minuteSeparator}{seconds:D2}{secondSeparator}";
                }

                // 智能格式：隐藏前导零
                if (hours > 0)
                {
                    return $"{hours}{hourSeparator}{minutes:D2}{minuteSeparator}{seconds:D2}{secondSeparator}";
                }
                else if (minutes > 0)
                {
                    return $"{minutes}{minuteSeparator}{seconds:D2}{secondSeparator}";
                }
                else
                {
                    return $"{seconds}{secondSeparator}";
                }
            }

            #endregion

            #region 数值转字符串

            /// <summary>
            /// 整数转为指定长度的字符串
            /// </summary>
            /// <param name="value"></param>
            /// <param name="length">字符串长度</param>
            /// <returns></returns>
            public static string NumberToString(int value, int length)
            {
                return value.ToString($"D{length}");
            }

            /// <summary>
            /// 浮点数转为指定保留小数点后几位的字符串返回
            /// </summary>
            /// <param name="value"></param>
            /// <param name="length">字符串长度</param>
            /// <returns></returns>
            public static string FloatToDecimalString(float value, int length)
            {
                return value.ToString($"F{length}");
            }

            /// <summary>
            /// 大数值转换成对应的字符串
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public static string BigValueConvertToString(int value)
            {
                if (value >= 100000000)
                {
                    int tempValue = value % 100000000 / 10000000;
                    return string.Format("{0}{1}", $"{value / 100000000}亿", tempValue != 0 ? $"{tempValue}千万" : "");
                }
                else if (value >= 10000)
                {
                    int tempValue = value % 10000 / 1000;
                    return string.Format("{0}{1}", $"{value / 10000}万", tempValue != 0 ? $"{tempValue}千" : "");
                }

                return value.ToString();
            }

            #endregion
        }
    }
}