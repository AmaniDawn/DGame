namespace GameLogic
{
    public class G
    {
        public static string R(string content) => content;

        public static string R(string content, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return content;
            }
            return string.Format(content, args);
        }

        public static string R(int id) => TextConfigMgr.Instance.GetText(id);

        public static string R(int id, params object[] args) => TextConfigMgr.Instance.GetText(id, args);

        public static string R(uint id) => TextConfigMgr.Instance.GetText(id);

        public static string R(uint id, params object[] args) => TextConfigMgr.Instance.GetText(id, args);

        public static string R(TextDefine id) => TextConfigMgr.Instance.GetText(id);

        public static string R(TextDefine id, params object[] args) => TextConfigMgr.Instance.GetText(id, args);
    }
}