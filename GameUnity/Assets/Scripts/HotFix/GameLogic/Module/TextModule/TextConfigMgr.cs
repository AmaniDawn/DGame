using DGame;
using GameProto;

namespace GameLogic
{
    public class TextConfigMgr : Singleton<TextConfigMgr>
    {
        private int m_curLanguage => (int)GameModule.LocalizationModule.CurrentLanguage;

        public TextConfig GetTextConfig(int id) => TbTextConfig.GetOrDefault(id);

        public TextConfig GetTextConfig(TextDefine id) => GetTextConfig((int)id);

        public string GetText(int id, params object[] args)
        {
            var textConfig = GetTextConfig(id);
            if (textConfig == null)
            {
                return $"TextID[{id}]";
            }
            string content = textConfig.Content[m_curLanguage];

            if ((textConfig.ArgNum > 0 && args == null) || textConfig.ArgNum != args.Length)
            {
                DLogger.Error($"Invalid string arg num, TextId[{id}] config num[{textConfig.ArgNum}] input num[{(args != null ? args.Length : -1)}]");
                return content;
            }

            return string.Format(content, args);
        }

        public string GetText(uint id, params object[] args) => GetText((int)id, args);

        public string GetText(TextDefine id, params object[] args) => GetText((int)id, args);

        /// <summary>
        /// 格式化大数字显示（千、万、亿）
        /// </summary>
        /// <param name="num">要格式化的数字</param>
        /// <param name="isNeedThousand">是否需要显示"千"单位</param>
        /// <returns>格式化后的字符串</returns>
        public string FormatNum(ulong num, bool isNeedThousand = false)
        {
            const ulong TEN_THOUSAND = 10000;
            const ulong HUNDRED_MILLION = 100000000;

            if (num < TEN_THOUSAND)
            {
                return num.ToString();
            }

            if (isNeedThousand && num >= 1000 && num < HUNDRED_MILLION)
            {
                return GetText(TextDefine.ID_LABEL_COMM_THOUSAND, FormatFraction(num, 1000));
            }

            if (num >= HUNDRED_MILLION)
            {
                return GetText(TextDefine.ID_LABEL_COMM_BILLION, FormatFraction(num, HUNDRED_MILLION));
            }

            return GetText(TextDefine.ID_LABEL_COMM_MYRIAD, FormatFraction(num, TEN_THOUSAND));
        }

        /// <summary>
        /// 格式化小数部分，保留两位小数并去除尾随0
        /// </summary>
        private static string FormatFraction(ulong num, ulong divisor)
        {
            double value = num / (double)divisor;
            return value.ToString("0.##").TrimEnd('0').TrimEnd('.');
        }
    }
}