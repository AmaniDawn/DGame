using System.Collections.Generic;
using DGame;

namespace GameLogic
{
    public class GmCmdHandler
    {
        public void Init()
        {
            RegisterGmCmd("gc", Gc);
        }

        public void RegisterGmCmd(string cmd, HandleGM func)
        {
            ClientGM.Instance.RegisterGmCmd(cmd, func);
        }

        public void ShowText(string format, params object[] args)
        {
            DLogger.Info(format, args);
            string retStr = string.Format(format, args);
            // UIModule.Instance.ShowWindowAsync<GMWindow>(retStr);
        }

        private void Gc(List<string> paras)
        {
            GameModule.ResourceModule.ForceUnloadUnusedAssets(true);
        }
    }
}