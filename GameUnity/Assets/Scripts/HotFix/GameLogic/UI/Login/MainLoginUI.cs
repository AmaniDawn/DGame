using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public partial class MainLoginUI
	{
		#region Ovrrider

		public override bool CanEscClose => false;

		protected override void BindMemberProperty()
		{
			m_goAccountNode.SetActive(false);
			m_toggleUserPrivacy.isOn = LoginMgr.Instance.IsAgreeUserPrivacy;
			
			m_quickAuthSaveData = ClientSaveDataMgr.Instance.GetSaveData<QuickAuthSaveData>();
			if (m_quickAuthSaveData != null)
			{
				m_inputAccount.text = m_quickAuthSaveData.Uid;
				m_inputPassword.text = m_quickAuthSaveData.Pwd;
			}
		}

		protected override void RegisterEvent()
		{
			AddUIEvent(ILoginUI_Event.OnLoginSuccess, OnLoginSuccess);
		}

		#endregion

		#region 字段

		private QuickAuthSaveData m_quickAuthSaveData;
		
		#endregion

		#region 函数


		#endregion

		#region 事件

		private partial void OnClickAgeTipsBtn()
		{
			GameEvent.Get<ICommonUI>().ShowComTipsUI(G.R("适龄提醒"), G.R("适龄提醒"));
		}

		private partial void OnToggleUserPrivacyChange(bool isOn)
		{
			LoginMgr.Instance.SetAgreeUserPrivacy(isOn);
		}

		private partial void OnClickUserAgreementBtn()
		{
			GameEvent.Get<ICommonUI>().ShowComTipsUI(G.R("用户协议"), G.R("用户协议"), true);
		}

		private partial void OnClickUserPrivacyBtn()
		{
			GameEvent.Get<ICommonUI>().ShowComTipsUI(G.R("隐私保护指引"), G.R("隐私保护指引"), true);
		}

		private partial void OnClickStartGameBtn()
		{
			UIModule.Instance.ShowWindowAsync<GameMainUI>();
		}

		private partial void OnClickSelectServerBtn()
		{
			UIModule.Instance.ShowWindowAsync<SelectServerUI>();
		}

		private partial void OnClickUserBtn()
		{
			m_goAccountNode.SetActive(true);
		}

		private partial void OnClickNotifyBtn()
		{
			GameEvent.Get<ICommonUI>().ShowComTipsUI(G.R("公告"), G.R("公告"));
		}

		private partial void OnClickCloseAccountBtn()
		{
			m_goAccountNode.SetActive(false);
		}

		private partial void OnClickLoginBtn()
		{
			if (m_quickAuthSaveData == null)
			{
				m_quickAuthSaveData = ClientSaveDataMgr.Instance.GetSaveData<QuickAuthSaveData>();
			}
			m_quickAuthSaveData.Uid = m_inputAccount.text;
			m_quickAuthSaveData.Pwd = m_inputPassword.text;
			m_quickAuthSaveData.Save();

			// DataCenterSys.Instance.AuthMgr.RequestAuth(m_user.text, m_pwd.text);
		}
		
		private void OnLoginSuccess()
		{
			m_goAccountNode.SetActive(false);
		}

		#endregion
	}
}