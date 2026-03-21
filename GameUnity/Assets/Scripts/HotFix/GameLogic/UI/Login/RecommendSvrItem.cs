using UnityEngine;
using UnityEngine.UI;
using DGame;
using Fantasy;
using GameProto;

namespace GameLogic
{
	public partial class RecommendSvrItem
	{
		#region Override

		#endregion
		
		#region 字段
		
		private CSServerInfo m_serverInfo;

		#endregion
		
		#region 函数
		
		public void Init(CSRecentServerRoleInfo recommendSvr)
		{
			if (recommendSvr == null)
			{
				return;
			}

			m_textLevel.text = $"<color=#46981D>{recommendSvr.Level}</color>级";
			m_serverInfo = LoginDataMgr.Instance.GetServerInfo(recommendSvr.ServerID);
			if (m_serverInfo == null)
			{
				return;
			}
			m_textName.text = m_serverInfo.Name;
			if (TbServerStateConfig.TryGetValue(recommendSvr.ServerID, out var stateCfg))
			{
				m_imgState.color = stateCfg.Color.ParseColor();
			}

			if (m_serverInfo.Recommend)
			{
				
			}
		}

		public override void UpdateSelectState()
		{
			m_goNoSelect.SetActive(!m_isSelected);
			m_goSelect.SetActive(m_isSelected);
		}

		#endregion
		
		#region 事件

		#endregion
	}
}
