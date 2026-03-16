using System;
using GameProto;

namespace GameLogic
{
    public enum ConfigType
    {

    }

    public class CacheGm
    {
        public GmConfig CacheCfg = new GmConfig();
        public int BatchNum;
    }

    public class QuickGmButtonInfo
    {
        public string GmName;
        public string GmText;
        public Action<string, GmConfig> GmCfgAction;
        public Action<QuickGmButtonInfo> Action;
        public GmConfig GmCfg;

        public QuickGmButtonInfo(string gmName, string gmText, Action<QuickGmButtonInfo> action, GmConfig gmCfg = null)
        {
            GmName = gmName;
            GmText = gmText;
            Action = action;
            GmCfg = gmCfg;
        }

        public QuickGmButtonInfo(string gmName, string gmText, Action<string, GmConfig> gmCfgAction, GmConfig gmCfg = null)
        {
            GmName = gmName;
            GmText = gmText;
            GmCfgAction = gmCfgAction;
            GmCfg = gmCfg;
        }

        public void Execute()
        {
            GmCfgAction?.Invoke(GmName, GmCfg);
            Action?.Invoke(this);
        }
    }
}