using System.Collections.Generic;
using DGame;
using UnityEngine.UI;

namespace GameLogic
{
	public class TipsUI : UIWindow
	{
		#region 脚本工具生成的代码

		private Image m_imgBg;
		private Text m_textTips;

		protected override void ScriptGenerator()
		{
			m_imgBg = FindChildComponent<Image>("m_imgBg");
			m_textTips = FindChildComponent<Text>("m_imgBg/m_textTips");
		}

		#endregion

		#region Override

		protected override void OnCreate()
		{
			m_tips.Enqueue(UserData.ToString());
			RefreshUI();
		}

		protected override ModelType GetModelType() => ModelType.NoneType;

		protected override UILayer windowLayer => UILayer.Tips;

		protected override void OnDestroy()
		{
			CancelGameTimer();
		}

		#endregion

		#region 字段

		private Queue<string> m_tips = new Queue<string>();
		private GameTimer m_timer;
		private bool m_isShowing;
		private const float TIPS_DURATION = 1.0f;

		#endregion

		#region 函数

		public void RefreshUI()
		{
			if (m_isShowing)
			{
				return;
			}

			if (m_tips.Count <= 0)
			{
				Close();
				return;
			}

			m_isShowing = true;
			m_textTips.text = m_tips.Dequeue();

			m_timer =
				GameModule.GameTimerModule.CreateUnscaledOnceGameTimer(TIPS_DURATION, OnTipsDurationEnd);
		}

		private void OnTipsDurationEnd(object[] args)
		{
			m_isShowing = false;
			RefreshUI();
		}

		private void CancelGameTimer()
		{
			GameModule.GameTimerModule.DestroyGameTimer(m_timer);
			m_timer = null;
		}

		#endregion
	}
}