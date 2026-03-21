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
		}

		protected override void RegisterEvent()
		{
			AddUIEvent(ILoginUI_Event.OnLoginAuthSuccess, OnLoginAuthSuccess);
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

			if (TbServerStateConfig.TryGetValue(curServerInfo.State, out var stateConfig))
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
				UIModule.Instance.ShowTipsUI(G.R("请阅读并同意，服务协议和隐私保护指引"));
				return;
			}
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

		private partial void OnClickRegisterBtn()
		{
			
		}
		
		private void OnLoginAuthSuccess()
		{
			m_goAccountNode.SetActive(false);
			RefreshServerInfo();
		}

		#endregion
	}
}