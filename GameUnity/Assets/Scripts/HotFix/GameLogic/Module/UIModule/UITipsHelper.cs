using DGame;

namespace GameLogic
{
    public partial class UIModule
    {
        /// <summary>
        /// 显示提示UI（根据文本ID）
        /// </summary>
        /// <param name="result">文本配置ID</param>
        public void ShowTipsUI(uint result)
        {
            string text = TextConfigMgr.Instance.GetText(result);
            ShowTipsUI(text);
        }

        /// <summary>
        /// 显示错误提示UI（根据文本ID）
        /// </summary>
        /// <param name="result">文本配置ID</param>
        public void ShowErrorTipsUI(uint result)
        {
            string text = TextConfigMgr.Instance.GetText(result);
            ShowErrorTipsUI(text);
        }

        /// <summary>
        /// 显示提示UI
        /// </summary>
        /// <param name="msg">提示消息内容</param>
        public void ShowTipsUI(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                DLogger.Info(msg);
                ShowWindowAsync<TipsUI>(msg);
            }
        }
    }
}