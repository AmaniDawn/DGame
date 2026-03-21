using System.Collections.Generic;
using Fantasy;
using Fantasy.Helper;

namespace GameLogic
{
	public partial class MainWindow
	{
		private static readonly List<string> AuthenticationList = new List<string>()
		{
			"127.0.0.1:20001", "127.0.0.1:20002", "127.0.0.1:20003"
		};

		public string Select(string userName)
		{
			var userNameHashCode = HashCodeHelper.MurmurHash3(userName);
			var authenticationListIndex = userNameHashCode % AuthenticationList.Count;
			// 按照现在的情况下，这个模出的值只会是0 - 3
			return AuthenticationList[(int)authenticationListIndex];
		}

		protected override void BindMemberProperty()
		{
		}

		protected override void RegisterEvent()
		{
			// AddUIEvent<GameAccountInfo>(ILoginUI_Event.GetAccountInfo, OnGetAccountInfo);
		}

		#region 事件

		private partial void OnClickRegisterBtn()
		{
			// 根据用户名来选择目标的鉴权服务器
			// var authenticationAddress = Select(m_inputUserName.text);
			// 根据鉴权服务器地址来创建一个新的网络会话
			var addressAttr = AuthenticationHelper.Select(m_inputUserName.text).Split(':');
			DataCenterSys.Instance.Register(addressAttr[0], int.Parse(addressAttr[1]), m_inputUserName.text, m_inputPassword.text).Coroutine();
		}

		private partial void OnClickLoginBtn()
		{
			var addressAttr = AuthenticationHelper.Select(m_inputUserName.text).Split(':');
			DataCenterSys.Instance.Login(addressAttr[0], int.Parse(addressAttr[1]), m_inputUserName.text, m_inputPassword.text).Coroutine();
			// DataCenterSys.Instance.Login("127.0.0.1", 20001, m_inputUserName.text, m_inputPassword.text).Coroutine();
		}

		private partial void OnClickGetBtn()
		{
			// LoginNetMgr.Instance.GetAccountInfoRequest().Coroutine();
		}

		// private void OnGetAccountInfo(GameAccountInfo accountInfo)
		// {
		// 	m_textMsg.text = accountInfo.LoginTime.ToString();
		// }

		#endregion
	}
}