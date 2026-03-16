using UnityEngine;
using UnityEngine.UI;
using DGame;
using Fantasy;

namespace GameLogic
{
	public partial class GameMainUIBtnItem
	{
		#region Override
		
		protected override void BindMemberProperty()
		{
			m_goLock.SetActive(false);
			m_goSelect.SetActive(false);
		}

		#endregion
		
		#region 字段
		
		public GameMainBtnType MainBtnType { get; private set; }
		public FuncType FuncType => (FuncType)MainBtnType;

		#endregion
		
		#region 函数

		public void Init(GameMainBtnType btnType)
		{
			MainBtnType = btnType;
		}
		
		public void RefreshOpenState()
		{
			bool isOpen = FuncOpenMgr.Instance.CheckFuncOpen(FuncType);
			m_goLock.SetActive(!isOpen);
			m_goUnlock.SetActive(isOpen);
		}

		public void SetSelectState(GameMainBtnType btnType)
		{
			bool isSelect = MainBtnType == btnType;
			m_goSelect.SetActive(isSelect);
			m_goNoSelect.SetActive(!isSelect);
		}

		#endregion
		
		#region 事件

		#endregion
	}
}
