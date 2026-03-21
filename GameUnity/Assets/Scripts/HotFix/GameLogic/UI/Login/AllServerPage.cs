using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DGame;
using Fantasy;
using SuperScrollView;

namespace GameLogic
{
	public partial class AllServerPage
	{
		#region Override

		protected override void BindMemberProperty()
		{
			m_itemLeft.SetActive(false);
			m_itemRight.SetActive(false);
			m_leftLoopListView = CreateWidget<UILoopListViewWidget<AllSvrLeftItem>>(m_scrollLeftScroll.gameObject);
			m_leftLoopListView.LoopRectView.InitListView(0, CreateLeftItem);
			m_rightLoopListView = CreateWidget<UILoopListViewWidget<AllSvrRightItem>>(m_scrollRightScroll.gameObject);
			m_rightLoopListView.LoopRectView.InitListView(0, CreateRightItem);
		}

		public override void OnPageShowed(int oldShowType, int newShowType)
		{
			if (newShowType == (int)ServerTabType.AllSvr)
			{
				RefreshUI();
			}
		}

		#endregion
		
		#region 字段

		private UILoopListViewWidget<AllSvrLeftItem> m_leftLoopListView;
		private UILoopListViewWidget<AllSvrRightItem> m_rightLoopListView;
		private readonly List<AllServerGroupData> m_groupDatas = new List<AllServerGroupData>();
		private readonly Dictionary<int, uint> m_recentRoleLevelMap = new Dictionary<int, uint>();
		private int m_selectGroupIndex = -1;
		private int m_selectServerIndex = -1;

		private class AllServerGroupData
		{
			public string GroupName;
			public int MinServerId;
			public int MaxServerId;
			public readonly List<CSServerInfo> ServerInfos = new List<CSServerInfo>();
		}

		#endregion
		
		#region 函数

		private void RefreshUI()
		{
			RefreshRecentRoleLevelMap();
			RefreshGroupDatas();
			RefreshLeftList();
			RefreshRightList(false);
		}

		private void RefreshRecentRoleLevelMap()
		{
			m_recentRoleLevelMap.Clear();
			var recentServerRoleInfos = LoginDataMgr.Instance.RecentServerRoleInfoList;
			if (recentServerRoleInfos == null)
			{
				return;
			}

			foreach (var recentServerRoleInfo in recentServerRoleInfos)
			{
				m_recentRoleLevelMap[recentServerRoleInfo.ServerID] = recentServerRoleInfo.Level;
			}
		}

		private void RefreshGroupDatas()
		{
			m_groupDatas.Clear();

			var serverInfos = LoginDataMgr.Instance.ServerInfoList;
			if (serverInfos == null || serverInfos.Count <= 0)
			{
				m_selectGroupIndex = -1;
				m_selectServerIndex = -1;
				return;
			}

			var groupMap = new Dictionary<int, AllServerGroupData>();
			for (int i = 0; i < serverInfos.Count; i++)
			{
				var serverInfo = serverInfos[i];
				if (!groupMap.TryGetValue(serverInfo.Group, out var groupData))
				{
					groupData = new AllServerGroupData
					{
						MinServerId = serverInfo.ServerID,
						MaxServerId = serverInfo.ServerID
					};
					groupMap.Add(serverInfo.Group, groupData);
					m_groupDatas.Add(groupData);
				}

				groupData.ServerInfos.Add(serverInfo);
				if (serverInfo.ServerID < groupData.MinServerId)
				{
					groupData.MinServerId = serverInfo.ServerID;
				}

				if (serverInfo.ServerID > groupData.MaxServerId)
				{
					groupData.MaxServerId = serverInfo.ServerID;
				}
			}

			m_groupDatas.Sort((left, right) => left.MaxServerId.CompareTo(right.MaxServerId));
			for (int i = 0; i < m_groupDatas.Count; i++)
			{
				var groupData = m_groupDatas[i];
				groupData.ServerInfos.Sort((left, right) =>
				{
					int recommendCompare = right.Recommend.CompareTo(left.Recommend);
					if (recommendCompare != 0)
					{
						return recommendCompare;
					}

					return left.ServerID.CompareTo(right.ServerID);
				});
				groupData.GroupName = groupData.MinServerId == groupData.MaxServerId
					? $"{groupData.MinServerId}服"
					: $"{groupData.MinServerId}-{groupData.MaxServerId}服";
			}

			m_selectGroupIndex = GetDefaultGroupIndex();
		}

		private void RefreshLeftList()
		{
			m_leftLoopListView.LoopRectView.SetListItemCount(m_groupDatas.Count);
			m_leftLoopListView.LoopRectView.RefreshAllShownItem();
			if (m_selectGroupIndex >= 0)
			{
				m_leftLoopListView.LoopRectView.MovePanelToItemIndex(m_selectGroupIndex, 0f);
			}
		}

		private void RefreshRightList(bool keepCurrentServer)
		{
			var groupData = GetSelectGroupData();
			int serverCount = groupData?.ServerInfos.Count ?? 0;
			m_selectServerIndex = keepCurrentServer ? GetCurrentServerIndex(groupData) : GetDefaultServerIndex(groupData);

			m_rightLoopListView.LoopRectView.SetListItemCount(serverCount);
			m_rightLoopListView.LoopRectView.RefreshAllShownItem();
			if (m_selectServerIndex >= 0)
			{
				m_rightLoopListView.LoopRectView.MovePanelToItemIndex(m_selectServerIndex, 0f);
			}
		}

		private AllServerGroupData GetSelectGroupData()
		{
			return m_selectGroupIndex >= 0 && m_selectGroupIndex < m_groupDatas.Count ? m_groupDatas[m_selectGroupIndex] : null;
		}

		private int GetDefaultGroupIndex()
		{
			int curServerId = LoginDataMgr.Instance.CurServerInfo?.ServerID ?? 0;
			for (int i = 0; i < m_groupDatas.Count; i++)
			{
				if (GetServerIndex(m_groupDatas[i], curServerId) >= 0)
				{
					return i;
				}
			}

			return m_groupDatas.Count > 0 ? 0 : -1;
		}

		private int GetDefaultServerIndex(AllServerGroupData groupData)
		{
			int curServerIndex = GetCurrentServerIndex(groupData);
			if (curServerIndex >= 0)
			{
				return curServerIndex;
			}

			return groupData != null && groupData.ServerInfos.Count > 0 ? 0 : -1;
		}

		private int GetCurrentServerIndex(AllServerGroupData groupData)
		{
			if (groupData == null)
			{
				return -1;
			}

			return GetServerIndex(groupData, LoginDataMgr.Instance.CurServerInfo?.ServerID ?? 0);
		}

		private int GetServerIndex(AllServerGroupData groupData, int serverId)
		{
			if (groupData == null)
			{
				return -1;
			}

			for (int i = 0; i < groupData.ServerInfos.Count; i++)
			{
				if (groupData.ServerInfos[i].ServerID == serverId)
				{
					return i;
				}
			}

			return -1;
		}

		private uint GetRecentRoleLevel(int serverId)
		{
			return m_recentRoleLevelMap.TryGetValue(serverId, out var level) ? level : 0;
		}

		private void OnClickGroupItem(int groupIndex)
		{
			if (groupIndex < 0 || groupIndex >= m_groupDatas.Count || m_selectGroupIndex == groupIndex)
			{
				return;
			}

			m_selectGroupIndex = groupIndex;
			m_leftLoopListView.LoopRectView.RefreshAllShownItem();
			RefreshRightList(false);
		}

		private void OnClickServerItem(CSServerInfo serverInfo)
		{
			if (serverInfo == null)
			{
				return;
			}

			var groupData = GetSelectGroupData();
			if (groupData == null)
			{
				return;
			}

			m_selectServerIndex = GetServerIndex(groupData, serverInfo.ServerID);
			m_rightLoopListView.LoopRectView.RefreshAllShownItem();

			var curServerInfo = LoginDataMgr.Instance.CurServerInfo;
			curServerInfo.Address = serverInfo.Address;
			curServerInfo.Port = serverInfo.Port;
			curServerInfo.Name = serverInfo.Name;
			curServerInfo.State = serverInfo.State;
			curServerInfo.Group = serverInfo.Group;
			curServerInfo.Recommend = serverInfo.Recommend;
			curServerInfo.ServerID = serverInfo.ServerID;
		}

		private LoopListViewItem2 CreateLeftItem(LoopListView2 loopListView2, int index)
		{
			if (m_groupDatas == null || index < 0 || index >= m_groupDatas.Count)
			{
				return null;
			}

			var item = m_leftLoopListView.CreateItem(m_itemLeft);
			if (item == null)
			{
				return null;
			}

			item.SetItemIndex(index);
			item.UpdateItem(index);
			item.Init(m_groupDatas[index].GroupName, index, index == m_selectGroupIndex, OnClickGroupItem);
			return item.LoopItem;
		}

		private LoopListViewItem2 CreateRightItem(LoopListView2 loopListView2, int index)
		{
			var groupData = GetSelectGroupData();
			if (groupData == null || index < 0 || index >= groupData.ServerInfos.Count)
			{
				return null;
			}

			var item = m_rightLoopListView.CreateItem(m_itemRight);
			if (item == null)
			{
				return null;
			}

			item.SetItemIndex(index);
			item.UpdateItem(index);
			var serverInfo = groupData.ServerInfos[index];
			item.Init(serverInfo, GetRecentRoleLevel(serverInfo.ServerID), index == m_selectServerIndex, OnClickServerItem);
			return item.LoopItem;
		}

		#endregion
		
		#region 事件

		#endregion
	}
}
