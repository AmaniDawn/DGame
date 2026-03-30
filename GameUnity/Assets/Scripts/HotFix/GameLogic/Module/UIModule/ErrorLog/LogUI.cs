using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace GameLogic
{
	public partial class LogUI : UIWindow
	{
		#region 脚本工具生成的代码

		private Button m_btnClose;
		private Text m_textError;

		protected override void ScriptGenerator()
		{
			m_btnClose = FindChildComponent<Button>("m_btnClose");
			m_textError = FindChildComponent<Text>("m_textError");
			m_btnClose.onClick.AddListener(OnClickCloseBtn);
		}

		#endregion

		#region Override

		protected override ModelType GetModelType() => ModelType.NoneType;

		protected override UILayer windowLayer => UILayer.System;

		#endregion

		#region 字段

		private readonly Stack<string> m_errorTextStack = new Stack<string>();
		private bool m_isInit = false;

		#endregion

		#region 函数

		protected override void OnRefresh()
		{
			m_errorTextStack.Push(UserData.ToString());

			if (!m_isInit)
			{
				m_textError.text = m_errorTextStack.Pop();
				m_isInit = true;
			}
		}

		#endregion

		#region 事件

		private void OnClickCloseBtn()
		{
			PopErrorLog().Forget();
		}

		private async UniTaskVoid PopErrorLog()
		{
			if (m_errorTextStack.Count <= 0)
			{
				await UniTask.Yield();
				Close();
				return;
			}

			m_textError.text = m_errorTextStack.Pop();
		}

		#endregion
	}
}