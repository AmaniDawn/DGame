using System;
using UnityEngine;
using UnityEngine.UI;
using DGame;
using Fantasy;
using GameProto;

namespace GameLogic
{
	public partial class AllSvrRightItem
	{
		#region 字段

		private CSServerInfo m_serverInfo;
		private Action<CSServerInfo> m_onClickAction;

		#endregion

		#region 函数

		public void Init(CSServerInfo serverInfo, uint roleLevel, bool isSelected, Action<CSServerInfo> onClickAction)
		{
			m_serverInfo = serverInfo;
			if (m_serverInfo == null)
			{
				return;
			}

			m_textName.text = m_serverInfo.Name;
			m_textLevel.text = roleLevel > 0 ? $"<color=#46981D>{roleLevel}</color>级" : string.Empty;
			m_goNew.SetActive(m_serverInfo.Recommend);
			m_onClickAction = onClickAction;

			if (ServerStateConfigMgr.Instance.TryGetValue(m_serverInfo.State, out var stateCfg))
			{
				m_imgState.color = stateCfg.Color.ParseColor();
			}

			BindClickEvent(OnItemClick);
			SetSelected(isSelected);
		}

		public override void UpdateSelectState()
		{
			m_goNoSelect.SetActive(!m_isSelected);
			m_goSelect.SetActive(m_isSelected);
		}

		#endregion

		#region 事件

		private void OnItemClick(SelectItemBase item)
		{
			m_onClickAction?.Invoke(m_serverInfo);
		}

		#endregion
	}
}