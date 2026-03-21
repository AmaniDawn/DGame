using UnityEngine;
using UnityEngine.UI;
using DGame;

namespace GameLogic
{
	public partial class ComTipsUI
	{
		#region Override

		protected override ModelType GetModelType() => ModelType.NormalHaveClose;

		protected override void BindMemberProperty()
		{
			m_scrollTab.SetActive(false);
			m_itemTab.SetActive(false);
		}

		#endregion
		
		#region 字段

		private bool m_isUserPrivacy;

		#endregion
		
		#region 函数

		public void Init(string title, string content, bool isUserPrivacy)
		{
			m_textContent.text = content;
			m_textTitle.text = title;
			m_isUserPrivacy = isUserPrivacy;
		}

		#endregion
		
		#region 事件

		private partial void OnClickCloseBtn()
		{
			if (m_isUserPrivacy)
			{
				LoginDataMgr.Instance.SetFirstOpenUserPrivacy(false);
			}
			Close();
		}

		#endregion
	}
}
