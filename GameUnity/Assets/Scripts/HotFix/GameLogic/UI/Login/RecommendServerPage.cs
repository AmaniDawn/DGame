using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DGame;
using Fantasy;
using SuperScrollView;

namespace GameLogic
{
	public partial class RecommendServerPage
	{
		#region Override

		protected override void BindMemberProperty()
		{
			m_recommendSvrLoopListView = CreateWidget<UILoopListViewWidget<RecommendSvrItem>>(m_scrollServer.gameObject);
			m_recommendSvrLoopListView.LoopRectView.InitListView(0, CreateRecommendSvrItem);
		}

		public override void OnPageShowed(int oldShowType, int newShowType)
		{
			if (newShowType == (int)ServerTabType.RecommendSvr)
			{
				RefreshUI();
			}
		}

		#endregion
		
		#region 字段
		
		private UILoopListViewWidget<RecommendSvrItem> m_recommendSvrLoopListView;
		private List<CSRecentServerRoleInfo> m_recommendSvrList;
		private bool m_isInit = false;

		#endregion
		
		#region 函数

		private void RefreshUI()
		{
			m_recommendSvrList = LoginDataMgr.Instance.RecentServerRoleInfoList;
			m_recommendSvrLoopListView.LoopRectView.SetListItemCount(m_recommendSvrList.Count);
			m_recommendSvrLoopListView.LoopRectView.RefreshAllShownItem();
		}
		
		private LoopListViewItem2 CreateRecommendSvrItem(LoopListView2 loopListView2, int index)
		{
			if (m_recommendSvrList == null || m_recommendSvrList.Count <= 0 || index > m_recommendSvrList.Count - 1)
			{
				return null;
			}
			
			var item =  m_recommendSvrLoopListView.CreateItem(m_itemReSvr);

			if (item == null)
			{
				return null;
			}

			item.Init(m_recommendSvrList[index]);

			if (!m_isInit)
			{
				item.SetSelected(index == 0);
				m_isInit = true;
			}

			return item.LoopItem;
		}

		#endregion
		
		#region 事件

		#endregion
	}
}
