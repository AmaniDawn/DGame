using UnityEngine;
using UnityEngine.UI;
using DGame;
using GameProto;

namespace GameLogic
{
	public partial class MainLoginUI
	{
		#region Ovrrider

		public override bool CanEscClose => false;

		protected override void BindMemberProperty()
		{
			m_goAccountNode.SetActive(false);
			m_toggleUserPrivacy.isOn = LoginDataMgr.Instance.IsAgreeUserPrivacy;
			
			m_quickAuthSaveData = ClientSaveDataMgr.Instance.GetSaveData<QuickAuthSaveData>();
			if (m_quickAuthSaveData != null)
			{
				m_inputAccount.text = m_quickAuthSaveData.Uid;
				m_inputPassword.text = m_quickAuthSaveData.Pwd;
			}
			m_goAccountNode.SetActive(true);
		}

		protected override void RegisterEvent()
		{
			AddUIEvent(ILoginUI_Event.OnLoginAuthSuccess, OnLoginAuthSuccess);
			AddUIEvent(ILoginUI_Event.OnLoginGateSuccess, Close);
		}

		protected override void OnCreate()
		{
			SetUIFit(m_rectContainer);
			if (!GameClient.Instance.IsStatusEnter)
			{
				m_autoLoginTimer = GameModule.GameTimerModule.CreateOnceGameTimer(0.2f, DoAutoLogin);
			}

			ShowAssetBundleVersion();
		}

		protected override void OnDestroy()
		{
			CancelTimer();
		}

		#endregion

		#region 字段

		private QuickAuthSaveData m_quickAuthSaveData;
		private GameTimer m_autoLoginTimer;
		
		#endregion

		#region 函数

		private void ShowAssetBundleVersion()
		{
			m_textVersion.text = GameModule.ResourceModule.PackageVersion;
		}

		private void CancelTimer()
		{
			GameModule.GameTimerModule.DestroyGameTimer(m_autoLoginTimer);
			m_autoLoginTimer = null;
		}

		private void DoAutoLogin(object[] args)
		{
			
		}

		private void DoLogin(bool isAutoLogin)
		{
			CancelTimer();
		}

		private void RefreshServerInfo()
		{
			var curServerInfo = LoginDataMgr.Instance.CurServerInfo;

			if (curServerInfo == null)
			{
				return;
			}

			if (ServerStateConfigMgr.Instance.TryGetValue(curServerInfo.State, out var stateConfig))
			{
				m_imgServer.color = stateConfig.Color.ParseColor();
			}
			m_textServer.text = curServerInfo.Name;
		}

		#endregion

		#region 事件

		private partial void OnClickAgeTipsBtn()
		{
			GameEvent.Get<ICommonUI>().ShowComTipsUI(G.R("适龄提醒"), G.R("适龄提醒"));
		}

		private partial void OnToggleUserPrivacyChange(bool isOn)
		{
			LoginDataMgr.Instance.SetAgreeUserPrivacy(isOn);
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
			if (!m_toggleUserPrivacy.isOn)
			{
				GameModule.UIModule.ShowTipsUI(G.R("请阅读并同意，服务协议和隐私保护指引"));
				return;
			}

			if (string.IsNullOrWhiteSpace(m_quickAuthSaveData.Token))
			{
				m_goAccountNode.SetActive(true);
				return;
			}

			if (LoginDataMgr.Instance.CurServerInfo == null 
			    || LoginDataMgr.Instance.CurServerInfo.ServerID <= 0)
			{
				GameModule.UIModule.ShowWindowAsync<SelectServerUI>();
				return;
			}

			LoginNetMgr.Instance.Login().Coroutine();
		}

		private partial void OnClickSelectServerBtn()
		{
			GameModule.UIModule.ShowWindowAsync<SelectServerUI>();
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

			if (string.IsNullOrWhiteSpace(m_inputAccount.text) || string.IsNullOrWhiteSpace(m_inputPassword.text))
			{
				GameModule.UIModule.ShowTipsUI(G.R("请输入帐号和密码"));
				return;
			}
			m_quickAuthSaveData.Uid = m_inputAccount.text;
			m_quickAuthSaveData.Pwd = m_inputPassword.text;
			m_quickAuthSaveData.Save();

			LoginNetMgr.Instance.LoginRequest(m_inputAccount.text, m_inputPassword.text).Coroutine();
		}

		private partial void OnClickRegisterBtn()
		{
			LoginNetMgr.Instance.RegisterRequest(m_inputAccount.text, m_inputPassword.text).Coroutine();
		}
		
		private void OnLoginAuthSuccess()
		{
			m_goAccountNode.SetActive(false);
			RefreshServerInfo();
		}

		#endregion
	}
}