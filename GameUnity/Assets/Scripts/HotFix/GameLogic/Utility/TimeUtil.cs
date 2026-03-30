using System;
using System.Collections.Generic;
using System.Text;
using DGame;

namespace GameLogic
{
    public static partial class Utility
    {
        public enum FormatTimeType
        {
            //HH:MM:SS
            HourMinSecType,

            //HH
            HourType,

            //HH
            TotalHourType,

            //HH:MM
            HourMinType,

            //MM:SS
            MinSecType,

            //SS:MS
            SecMillType,

            //{0}天{1}HH:{2}MM
            DayHourMinType,

            //30650 {0}天{1}:{2}:{3}
            DayHourMinSecType,

            //{0}小时{1}分钟
            ChineseHourMinType,

            //{0}小时{1}分钟{2}秒
            ChineseHourMinSecType,

            //{0}天{1}时{2}分{3}秒
            ChineseDayHourMinSecType,

            //HH:MM:SS
            DayToHourMinSecType,

            //{0}分{1}{2}秒
            ChineseMinSecTypeWithFenGe,

            //{0}分{2}秒
            ChineseMinSecType,
        }

        public class DateMask
        {
            public const uint DATE_MASK_YEAR = 1;
            public const uint DATE_MASK_MONTH = 2;
            public const uint DATE_MASK_DAY = 4;
            public const uint DATE_MASK_HOUR = 8;
            public const uint DATE_MASK_MIN = 16;
            public const uint DATE_MASK_SEC = 32;
        }

        public class DateTimeFormatParam
        {
            /// <summary>
            /// DateMask
            /// </summary>
            public uint DateMask;

            public string YearSeparator;
            public string MonthSeparator;
            public string DaySeparator;
            public string HourSeparator;
            public string MinSeparator;
            public string SecSeparator;
            public bool ToChinese; // 数字转成中文
            public bool ShowDouble; // 例如8，要不要显示08
        };

        public static class TimeUtil
        {
            public static readonly DateTime InitTime = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

            /// <summary>
            /// 服务器时间, 自1970年1月1日的秒数,utc
            /// </summary>
            public static long ServerTime = 0;

            /// <summary>
            /// 服务器时区和0（UTC）时区相差的秒数
            /// </summary>
            public static int ServerTimeGreenWich = 0;

            /// <summary>
            /// 上次更新服务器时间
            /// </summary>
            public static float LastUpdateServerTime = 0;

            /// <summary>
            /// 上次反馈时间
            /// </summary>
            public static long LastFeedBackTime = 0;

            /// <summary>
            /// 一小时秒数
            /// </summary>
            public const uint SecondOfHour = 3600u;

            /// <summary>
            /// 一天秒数
            /// </summary>
            public const uint SecondOfDay = 86400u;

            private static readonly StringBuilder m_stringBuilder = new StringBuilder();

            #region 服务器时间相关

            /// <summary>
            /// 更新服务器时间
            /// </summary>
            /// <param name="curSvrTime">服务器下发时间戳</param>
            /// <param name="secsWest">和0时区相差的秒数</param>
            public static void UpdateServerTime(long curSvrTime, int secsWest)
            {
                LastUpdateServerTime = GameTime.RealtimeSinceStartup;
                ServerTime = curSvrTime;
                ServerTimeGreenWich = secsWest;
            }

            /// <summary>
            /// 服务器UTC时间戳 int
            /// </summary>
            /// <returns></returns>
            public static long GetServerSeconds()
                => ServerTime + (long)GameTime.RealtimeSinceStartup - (long)LastUpdateServerTime;

            /// <summary>
            /// 服务器UTC时间戳 double
            /// </summary>
            /// <returns></returns>
            public static double GetServerDetailSeconds()
                => ServerTime + (double)GameTime.RealtimeSinceStartup - LastUpdateServerTime;

            /// <summary>
            /// 服务器所在时区时间 int
            /// </summary>
            /// <returns></returns>
            public static long GetSvrLocalSec() => GetServerSeconds() + ServerTimeGreenWich;

            /// <summary>
            /// 服务器所在时区时间 double
            /// </summary>
            /// <returns></returns>
            public static double GetSvrLocalDetailSeconds() => GetServerDetailSeconds() + ServerTimeGreenWich;

            /// <summary>
            /// 服务器时间戳 DateTime
            /// </summary>
            /// <param name="highPrecision"></param>
            /// <returns></returns>
            public static DateTime GetSvrDateTime(bool highPrecision = true)
                => highPrecision
                    ? InitTime.AddSeconds(GetSvrLocalDetailSeconds())
                    : InitTime.AddSeconds(GetSvrLocalSec());

            /// <summary>
            /// 客户端所在时区时间 long
            /// </summary>
            /// <returns></returns>
            public static long GetClientLocalSec() => Utc2ClientLocalSec(GetServerSeconds());

            /// <summary>
            /// 客户端所在时区的时间 DateTime
            /// </summary>
            /// <returns></returns>
            public static DateTime GetClientLocalDateTime() =>
                InitTime.AddSeconds(GetServerDetailSeconds()).ToLocalTime();

            #endregion

            #region UTC时间和本地时间转换

            /// <summary>
            /// utc时间戳转换为客户端所在时区时间戳
            /// </summary>
            /// <param name="utcUnixSeconds"></param>
            /// <returns></returns>
            public static long Utc2ClientLocalSec(long utcUnixSeconds)
                => DateTimeOffset.FromUnixTimeSeconds(utcUnixSeconds)
                    .ToLocalTime().ToUnixTimeSeconds();

            /// <summary>
            /// utc时间戳转换为客户端所在时区时间戳
            /// </summary>
            /// <param name="utcUnixSeconds"></param>
            /// <returns></returns>
            public static DateTime Utc2ClientLocalDateTime(long utcUnixSeconds)
                => DateTimeOffset.FromUnixTimeSeconds(utcUnixSeconds).LocalDateTime;

            /// <summary>
            /// utc时间戳转换为服务器所在时区时间戳
            /// </summary>
            /// <param name="utcTime"></param>
            /// <returns></returns>
            public static double Utc2SvrLocalSec(double utcTime)
                => utcTime + ServerTimeGreenWich;

            /// <summary>
            /// utc时间戳转换为服务器所在时区DateTime
            /// </summary>
            /// <param name="utcTime"></param>
            /// <returns></returns>
            public static DateTime Utc2SvrLocalDateTime(double utcTime)
                => InitTime.AddSeconds(Utc2SvrLocalSec(utcTime));

            #endregion

            #region 工具方法

            public static uint GetServerTodaySeconds() => (uint)GetSvrDateTime().TimeOfDay.TotalSeconds;

            public static int GetTodayWeekDay()
                => GetSvrDateTime(false).DayOfWeek switch
                {
                    DayOfWeek.Monday => 1,
                    DayOfWeek.Tuesday => 2,
                    DayOfWeek.Wednesday => 3,
                    DayOfWeek.Thursday => 4,
                    DayOfWeek.Friday => 5,
                    DayOfWeek.Saturday => 6,
                    DayOfWeek.Sunday => 7,
                    _ => 0
                };

            /// <summary>
            /// 判断两个时间之间垮了多少天，offsetZeroSecond是从0点开始偏移的秒， 比如从5点开始算一天，就是 5*3600
            /// </summary>
            /// <param name="time1"></param>
            /// <param name="time2"></param>
            /// <param name="offsetZeroSecond"></param>
            /// <returns></returns>
            public static long CalcDiffDay(long time1, long time2, long offsetZeroSecond = 0)
            {
                long min = Math.Min(time1, time2) - offsetZeroSecond;
                long max = Math.Max(time1, time2) - offsetZeroSecond;
                // min按照 86400 取整
                min -= min % SecondOfDay;
                return (max - min) / SecondOfDay;
            }

            /// <summary>
            /// 获取到达下一个循环开始日的剩余时间  offsetZeroSecond是从0点开始偏移的秒， 比如从5点开始算一天，就是 5*3600
            /// </summary>
            /// <param name="time"></param>
            /// <param name="offsetZeroSecond"></param>
            /// <returns></returns>
            public static long GetHowManySecondToNextTime(long time, long offsetZeroSecond)
            {
                if (time == 0) return 0;
                long remainder = time % SecondOfDay;
                return (SecondOfDay - remainder) % SecondOfDay + offsetZeroSecond;
            }

            public static bool CheckIsSameDay(long lhs, long rhs)
                => lhs / SecondOfDay == rhs / SecondOfDay;

            /// <summary>
            /// 获取自动格式化时间字符串
            /// </summary>
            /// <param name="time">时间戳</param>
            /// <returns></returns>
            public static string GetFormatTimeAuto(float time)
            {
                if (time < 3600)
                {
                    return GetFormatTime(time, FormatTimeType.MinSecType);
                }

                if (time <= 86400)
                {
                    return GetFormatTime(time, FormatTimeType.HourMinSecType);
                }

                return GetFormatTime(time, FormatTimeType.DayHourMinSecType);
            }

            /// <summary>
            /// 获取格式化时间字符串
            /// </summary>
            /// <param name="time">时间戳</param>
            /// <param name="type">格式化类型</param>
            /// <param name="separator">分隔符</param>
            /// <returns></returns>
            public static string GetFormatTime(double time, FormatTimeType type, string separator = " : ")
            {
                var timeSpan = TimeSpan.FromSeconds(time);
                int totalHours = (int)timeSpan.TotalHours;
                int totalMinutes = (int)timeSpan.TotalMinutes;
                return type switch
                {
                    FormatTimeType.HourMinSecType =>
                        $"{timeSpan.Hours:D2}{separator}{timeSpan.Minutes:D2}{separator}{timeSpan.Seconds:D2}",
                    FormatTimeType.HourType => timeSpan.Hours.ToString(),
                    FormatTimeType.TotalHourType => totalHours.ToString(),
                    FormatTimeType.HourMinType => $"{timeSpan.Hours:D2}{separator}{timeSpan.Minutes:D2}",
                    FormatTimeType.MinSecType => $"{totalMinutes:D2}{separator}{timeSpan.Seconds:D2}",
                    FormatTimeType.SecMillType => $"{timeSpan.Seconds:D2}{separator}{timeSpan.Milliseconds / 10:D2}",
                    FormatTimeType.DayHourMinType => G.R(TextDefine.ID_LABEL_DAY_HOUR_MIN_TEXT, timeSpan.Days,
                        timeSpan.Hours.ToString("D2"), timeSpan.Minutes.ToString("D2")),
                    FormatTimeType.DayHourMinSecType => timeSpan.Days > 0
                        ? G.R(TextDefine.ID_LABEL_DAY_HOUR_MIN_SEC_TEXT, timeSpan.Days,
                            timeSpan.Hours.ToString("D2"), timeSpan.Minutes.ToString("D2"),
                            timeSpan.Seconds.ToString("D2"))
                        : $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}",
                    FormatTimeType.ChineseHourMinType => G.R(TextDefine.ID_LABEL_COMMON_HOUR_MIN_TEXT,
                        totalHours.ToString("D2"), timeSpan.Minutes.ToString("D2")),
                    FormatTimeType.ChineseDayHourMinSecType => G.R(TextDefine.ID_LABEL_CHINESE_DAY_HOUR_MIN_SEC_TEXT,
                        timeSpan.Days, timeSpan.Hours.ToString("D2"), timeSpan.Minutes.ToString("D2"),
                        timeSpan.Seconds.ToString("D2")),
                    FormatTimeType.ChineseHourMinSecType => G.R(TextDefine.ID_LABEL_CHINESE_HOUR_MIN_SEC_TEXT,
                        timeSpan.Hours.ToString("D2"), timeSpan.Minutes.ToString("D2"),
                        timeSpan.Seconds.ToString("D2")),
                    FormatTimeType.DayToHourMinSecType =>
                        $"{totalHours:D2}{separator}{timeSpan.Minutes:D2}{separator}{timeSpan.Seconds:D2}",
                    FormatTimeType.ChineseMinSecTypeWithFenGe => G.R(TextDefine.ID_LABEL_CHINESE_MIN_SEC_TXT_WITH_FENGE,
                        timeSpan.Minutes.ToString("D2"), separator, timeSpan.Seconds.ToString("D2")),
                    FormatTimeType.ChineseMinSecType => G.R(TextDefine.ID_LABEL_CHINESE_MIN_SEC_TXT_WITHOUT_FENGE,
                        timeSpan.Minutes.ToString("D2"), timeSpan.Seconds.ToString("D2")),
                    _ => string.Empty
                };
            }

            /// <summary>
            /// 获取格式化时间字符串
            /// </summary>
            /// <param name="time">时间戳</param>
            /// <param name="param">格式化参数</param>
            /// <param name="toSvrLocalTime">是否转服务器时区时间</param>
            /// <returns></returns>
            public static string GetDateTimeFormat(long time, DateTimeFormatParam param, bool toSvrLocalTime = true)
            {
                if (param == null) return string.Empty;
                DateTime dt = toSvrLocalTime ? Utc2SvrLocalDateTime(time) : InitTime.AddSeconds(time);
                m_stringBuilder.Clear();

                if ((param.DateMask & DateMask.DATE_MASK_YEAR) != 0)
                {
                    m_stringBuilder.Append(param.ToChinese ? Num2ChineseLikeYear(dt.Year) : dt.Year.ToString())
                        .Append(param.YearSeparator);
                }

                if ((param.DateMask & DateMask.DATE_MASK_MONTH) != 0)
                {
                    if (param.ToChinese)
                    {
                        m_stringBuilder.Append(Number2Chinese(dt.Month));
                    }
                    else
                    {
                        if (param.ShowDouble && dt.Month < 10)
                        {
                            m_stringBuilder.Append(0);
                        }

                        m_stringBuilder.Append(dt.Month);
                    }

                    m_stringBuilder.Append(param.MonthSeparator);
                }

                if ((param.DateMask & DateMask.DATE_MASK_DAY) != 0)
                {
                    if (param.ToChinese)
                    {
                        m_stringBuilder.Append(Number2Chinese(dt.Day));
                    }
                    else
                    {
                        if (param.ShowDouble && dt.Day < 10)
                        {
                            m_stringBuilder.Append(0);
                        }

                        m_stringBuilder.Append(dt.Day);
                    }

                    m_stringBuilder.Append(param.DaySeparator);
                }

                if ((param.DateMask & DateMask.DATE_MASK_HOUR) != 0)
                {
                    m_stringBuilder.Append(param.ToChinese ? Number2Chinese(dt.Hour) : $"{dt.Hour:D2}");
                    m_stringBuilder.Append(param.HourSeparator);
                }

                if ((param.DateMask & DateMask.DATE_MASK_MIN) != 0)
                {
                    m_stringBuilder.Append(param.ToChinese ? Number2Chinese(dt.Minute) : $"{dt.Minute:D2}");
                    m_stringBuilder.Append(param.MinSeparator);
                }


                if ((param.DateMask & DateMask.DATE_MASK_SEC) != 0)
                {
                    m_stringBuilder.Append(param.ToChinese ? Number2Chinese(dt.Second) : $"{dt.Second:D2}");
                    m_stringBuilder.Append(param.SecSeparator);
                }

                return m_stringBuilder.ToString();
            }

            /// <summary>
            /// 获取时间与当前服务器时间相差多久
            /// </summary>
            /// <param name="time"></param>
            /// <param name="showHourMin"></param>
            /// <returns></returns>
            public static string GetSpanTimeFormat(long time, bool showHourMin = true)
            {
                long timeSpan = GetServerSeconds() - time;
                return timeSpan switch
                {
                    < 60 => G.R(TextDefine.ID_LABEL_TIME_SPAN_SECOND),
                    < 3600 => G.R(TextDefine.ID_LABEL_TIME_SPAN_MINUTE, timeSpan / 60),
                    < 86400 when showHourMin => G.R(TextDefine.ID_LABEL_TIME_SPAN_HOUR, timeSpan / 3600,
                        timeSpan / 60 % 60),
                    < 86400 => G.R(TextDefine.ID_LABEL_TIME_SPAN_HOUR_WITHOUT_MIN, timeSpan / 3600),
                    _ => G.R(TextDefine.ID_LABEL_TIME_SPAN_DAY, timeSpan / 86400)
                };
            }

            /// <summary>
            /// 获取时间剩余
            /// </summary>
            /// <param name="timeLeft">剩余时间戳</param>
            /// <param name="param">格式化参数</param>
            /// <param name="format">格式化字符串</param>
            /// <param name="ignoreFirstZero">是否忽略首位0</param>
            /// <returns></returns>
            public static string GetTimeLeftFormat(uint timeLeft, DateTimeFormatParam param, string format = null, bool
                ignoreFirstZero = true)
            {
                if (param == null)
                {
                    return string.Empty;
                }

                var timeSpan = TimeSpan.FromSeconds(timeLeft);
                var sb = new StringBuilder();
                bool hasShown = false;

                // 天
                if ((param.DateMask & DateMask.DATE_MASK_DAY) != 0)
                {
                    if (!ignoreFirstZero || timeSpan.Days != 0)
                    {
                        sb.Append(FormatValue(timeSpan.Days, param.DaySeparator, format, string.Empty));
                        hasShown = true;
                    }
                }

                // 小时（如果不显示天，天累加到小时）
                int hours = timeSpan.Hours;

                if ((param.DateMask & DateMask.DATE_MASK_DAY) == 0)
                {
                    hours += timeSpan.Days * 24;
                }

                if ((param.DateMask & DateMask.DATE_MASK_HOUR) != 0)
                {
                    if (!ignoreFirstZero || hasShown || hours != 0)
                    {
                        sb.Append(FormatValue(hours, param.HourSeparator, format));
                        hasShown = true;
                    }
                }

                // 分钟（如果不显示小时，小时累加到分钟）
                int minutes = timeSpan.Minutes;

                if ((param.DateMask & DateMask.DATE_MASK_HOUR) == 0)
                {
                    minutes += hours * 60;
                }

                if ((param.DateMask & DateMask.DATE_MASK_MIN) != 0)
                {
                    if (!ignoreFirstZero || hasShown || minutes != 0)
                    {
                        sb.Append(FormatValue(minutes, param.MinSeparator, format));
                        hasShown = true;
                    }
                }

                // 秒（总是显示）
                int seconds = timeSpan.Seconds;

                if ((param.DateMask & DateMask.DATE_MASK_MIN) == 0)
                {
                    seconds += minutes * 60;
                }

                if ((param.DateMask & DateMask.DATE_MASK_SEC) != 0)
                {
                    sb.Append(FormatValue(seconds, param.SecSeparator, format));
                }

                return sb.ToString();
            }

            private static string FormatValue(int value, string suffix, string outerFormat, string innerFormat = "D2")
            {
                string formatted = innerFormat != null ? value.ToString(innerFormat) : value.ToString();
                string result = formatted + suffix;
                return string.IsNullOrEmpty(outerFormat) ? result : string.Format(outerFormat, result);
            }

            /// <summary>
            /// 获取年份的大写 2026 => 二零二六
            /// </summary>
            /// <param name="num"></param>
            /// <returns></returns>
            public static string Num2ChineseLikeYear(int num)
            {
                var stack = new Stack<string>();

                while (num > 0)
                {
                    stack.Push(G.R(TextDefine.ID_LABEL_ZERO + num % 10));
                    num /= 10;
                }

                return string.Concat(stack);
            }

            /// <summary>
            /// 数字转中文
            /// </summary>
            /// <param name="number"></param>
            /// <returns></returns>
            public static string Number2Chinese(int number)
            {
                if (number == 0)
                {
                    return G.R(TextDefine.ID_LABEL_ZERO);
                }

                var str = GetNumChinese(number.ToString());
                return number is >= 10 and <= 19 ? str.Substring(1) : str;
            }

            /// <summary>
            /// 获取中文大写数字 0-99
            /// </summary>
            /// <param name="str"></param>
            /// <returns></returns>
            public static string GetNumChinese(string str)
            {
                // 取第一个
                if (str.Length == 1)
                {
                    int digit = str[0] - '0';
                    return digit == 0 ? "" : G.R(TextDefine.ID_LABEL_ZERO + digit);
                }

                // 两位数
                int tens = str[0] - '0';
                int ones = str[1] - '0';

                var sb = new StringBuilder();

                if (tens > 0)
                {
                    sb.Append(G.R(TextDefine.ID_LABEL_ZERO + tens));
                }

                sb.Append(G.R(TextDefine.ID_LABEL_TEN));

                if (ones > 0)
                {
                    sb.Append(G.R(TextDefine.ID_LABEL_ZERO + ones));
                }

                return sb.ToString();
            }

            #endregion

            #region 反馈时间

            public static void UpdateFeedBackTime() => LastFeedBackTime = GetServerSeconds();

            public static long GetCanFeedBackCoolTime() => LastFeedBackTime + 1 - GetServerSeconds();

            #endregion
        }
    }
}