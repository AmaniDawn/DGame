using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
	internal enum ServerTabType
	{
		/// <summary>
		/// 无
		/// </summary>
		None = 0,

		/// <summary>
		/// 推荐服务器
		/// </summary>
		RecommendSvr,

		/// <summary>
		/// 所有服务器
		/// </summary>
		AllSvr,

		/// <summary>
		/// 最大
		/// </summary>
		Max
	}

	public partial class SelectServerUI
	{
		#region Override

		protected override ModelType GetModelType() => ModelType.NormalHaveClose;

		protected override void BindMemberProperty()
		{
			m_itemStateTips.SetActive(false);
			CreateSwitch();
		}

		protected override void OnCreate()
		{
			RefreshUI();
		}

		#endregion

		#region 字段

		private readonly List<string> switchTabNames = new List<string>() { G.R("推荐服务器"), G.R("所有服务器")};
		private readonly List<StateTipsItem> m_serverStateTipsItems = new List<StateTipsItem>();
		private List<ServerStateConfig> m_serverStates;
		private SwitchPageMgr m_switchPageMgr;

		#endregion

		#region 函数

		private void CreateSwitch()
		{
			m_switchPageMgr = new SwitchPageMgr(m_tfTabNode, m_tfPageNode, this);
			bool showRecommendSvr = LoginDataMgr.Instance.RecentServerRoleInfoList.Count > 0;
			// 推荐服务器
			m_switchPageMgr.BindChildPage<RecommendServerPage>((int)ServerTabType.RecommendSvr, switchTabNames[0]);
			m_switchPageMgr.CreateTab<SwitchTabItem>((int)ServerTabType.RecommendSvr, m_itemSwitch, showRecommendSvr);
			// 所有服务器
			m_switchPageMgr.BindChildPage<AllServerPage>((int)ServerTabType.AllSvr, switchTabNames[1]);
			m_switchPageMgr.CreateTab<SwitchTabItem>((int)ServerTabType.AllSvr, m_itemSwitch, !showRecommendSvr);
		}

		private void RefreshUI()
		{
			RefreshSvrStateGroup();
		}

		private void RefreshSvrStateGroup()
		{
			m_serverStates = ServerStateConfigMgr.Instance.DataList;

			if (m_serverStates == null)
			{
				return;
			}
			AsyncAdjustItemNum(m_serverStateTipsItems, m_serverStates.Count, m_tfServerStateGroup, m_itemStateTips);
			for (int i = 0; i < m_serverStateTipsItems.Count; i++)
			{
				var item = m_serverStateTipsItems[i];
				item.Init(m_serverStates[i]);
			}
		}

		#endregion

		#region 事件

		private partial void OnClickCloseBtn()
		{
			Close();
		}

		#endregion
	}
}