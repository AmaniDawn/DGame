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
    }
}