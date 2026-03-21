using System;
using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public partial class AllSvrLeftItem
	{
		#region 字段

		public int m_groupIndex;
		private Action<int> m_onClickAction;

		#endregion
		
		#region 函数

		public void Init(string groupName, int groupIndex, bool isSelected, Action<int> onClickAction)
		{
			m_textSelect.text = groupName;
			m_textNoSelect.text = groupName;
			BindClickEvent(OnItemClick);
			m_groupIndex = groupIndex;
			m_onClickAction = onClickAction;
			SetSelected(isSelected);
		}

		public override void UpdateSelectState()
		{
			m_goSelect.SetActive(m_isSelected);
			m_goNoSelect.SetActive(!m_isSelected);
		}

		#endregion

		#region 事件

		private void OnItemClick(SelectItemBase item)
		{
			m_onClickAction?.Invoke(m_groupIndex);
		}

		#endregion
	}
}
