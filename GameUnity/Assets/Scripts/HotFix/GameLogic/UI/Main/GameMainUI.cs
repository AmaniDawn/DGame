using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DGame;
using GameProto;

namespace GameLogic
{
	public enum GameMainBtnType
	{
		None = 0,
		
		/// <summary>
		/// 商城
		/// </summary>
		Shop = 1,
		
		/// <summary>
		/// 角色
		/// </summary>
		Actor = 2,
		
		/// <summary>
		/// 主界面
		/// </summary>
		Main = 3,
		
		/// <summary>
		/// 公会
		/// </summary>
		League = 4,
		
		/// <summary>
		/// 玩法
		/// </summary>
		GamePlay = 5,
		
		/// <summary>
		/// 家园
		/// </summary>
		Home = 6,
		
		/// <summary>
		/// 最大
		/// </summary>
		Max = 7
	}
	
	public partial class GameMainUI
	{
		#region Override

		public override bool CanEscClose => false;

		protected override ModelType GetModelType() => ModelType.NoneType;

		public override bool FullScreen => true;

		protected override void BindMemberProperty()
		{
			m_gridBtnNode = m_tfButtons.GetComponent<GridLayoutGroup>();
			m_btnGm.SetActive(false);
			m_itemBtn.SetActive(false);
			CreateGameMainButtons();
		}

		protected override void OnCreate()
		{
			bool showGm = false;
			if (!DGame.Utility.PlatformUtil.IsPcOrEditorPlatform())
			{
				showGm = false;
			}
			m_btnGm.SetActive(showGm);
			RefreshMainPage();
		}

		#endregion
		
		#region 字段
		
		private GridLayoutGroup m_gridBtnNode;
		private List<GameMainUIBtnItem> m_mainBtnList = new List<GameMainUIBtnItem>();
		private Dictionary<GameMainBtnType, UIWidget> m_pageDict = new Dictionary<GameMainBtnType, UIWidget>();
		private List<GameMainBtnType> m_loadingPage = new List<GameMainBtnType>();
		private GameMainBtnType m_curShowPageType = GameMainBtnType.Main;

		#endregion
		
		#region 函数
		
		private void RefreshMainPage()
		{
			if (!m_pageDict.ContainsKey(GameMainBtnType.Main))
			{
				var mainPage = CreateWidgetByType<MainPage>(m_tfPageNode);
				if (mainPage != null)
				{
					m_pageDict.Add(GameMainBtnType.Main, mainPage);
				}
			}
			SwitchPage(GameMainBtnType.Main);

			foreach (var item in m_mainBtnList)
			{
				item.RefreshOpenState();
				item.RegisterFuncTarget();
			}
		}

		public void RefreshFuncOpenState()
		{
			foreach (var item in m_mainBtnList)
			{
				item.RefreshOpenState();
				item.RegisterFuncTarget();
			}
		}

		private UIWidget GetPageByGameMainBtnType(GameMainBtnType btnType) 
			=> m_pageDict.TryGetValue(btnType, out UIWidget page) ? page : null;

		private void CreateGameMainButtons()
		{
			for (int i = (int)GameMainBtnType.None + 1; i < (int)GameMainBtnType.Max; i++)
			{
				var btnType = (GameMainBtnType)i;
				switch (btnType)
				{
					case GameMainBtnType.Shop:
						CreateGameButton<GameMainUIBtnShop>(btnType);
						break;

					case GameMainBtnType.Actor:
						CreateGameButton<GameMainUIBtnActor>(btnType);
						break;

					case GameMainBtnType.Main:
						CreateGameButton<GameMainUIBtnMain>(btnType);
						break;

					case GameMainBtnType.League:
						CreateGameButton<GameMainUIBtnLeague>(btnType);
						break;

					case GameMainBtnType.GamePlay:
						CreateGameButton<GameMainUIBtnGamePlay>(btnType);
						break;

					case GameMainBtnType.Home:
						CreateGameButton<GameMainUIBtnHome>(btnType);
						break;
				}
			}
			var showCnt = m_mainBtnList.Count;
			var tabWidth = ((RectTransform)m_tfButtons).sizeDelta.x / showCnt;
			m_gridBtnNode.cellSize = new Vector2(tabWidth, m_gridBtnNode.cellSize.y);
		}

		private void CreateGameButton<T>(GameMainBtnType btnType) where T : GameMainUIBtnItem, new()
		{
			var btn = CreateWidgetByPrefab<T>(m_itemBtn, m_tfButtons);
			btn.Init(btnType);
			btn.BindClickEvent(OnClickGameButton);
			m_mainBtnList.Add(btn);
		}

		private void OnClickGameButton(GameMainUIBtnItem item)
		{
			var btnType = item.MainBtnType;
			SwitchPage(btnType);
		}

		private void SetGameMainBtnState()
		{
			foreach (var btn in m_mainBtnList)
			{
				btn.SetSelectState(m_curShowPageType);
			}
		}

		public void SwitchPage(GameMainBtnType btnType, Action<UIWidget> onSwitchPage = null)
		{
			m_curShowPageType = btnType;
			CreatePageTabByGameMainBtnType(btnType, onSwitchPage);
			SetGameMainBtnState();
		}

		private void CreatePageTabByGameMainBtnType(GameMainBtnType btnType, Action<UIWidget> onSwitchPage = null)
		{
			var funcType = (FuncType)btnType;
			if (funcType == FuncType.FUNC_TYPE_NONE)
			{
				return;
			}
			if (CheckPageIsOpen(funcType))
			{
				if (m_pageDict.TryGetValue(btnType, out UIWidget page))
				{
					foreach (var item in m_pageDict.Values)
					{
						item.Show(false);
					}
					page.Show(m_curShowPageType == btnType);
					onSwitchPage?.Invoke(page);
				}
				else
				{
					foreach (var item in m_pageDict)
					{
						if (item.Key != btnType)
						{
							item.Value.Show(false);
						}
					}

					if (!m_loadingPage.Contains(btnType))
					{
						CreatePageAsync(btnType, onSwitchPage).Forget();
					}
				}
			}
		}

		private async UniTaskVoid CreatePageAsync(GameMainBtnType btnType, Action<UIWidget> onSwitchPage = null)
		{
			m_loadingPage.Add(btnType);
			UIWidget page = null;
			switch (btnType)
			{
				case GameMainBtnType.Shop:
					page = await CreateWidgetByTypeAsync<ShopPage>(m_tfPageNode);
					break;

				case GameMainBtnType.Actor:
					page = await CreateWidgetByTypeAsync<ActorPage>(m_tfPageNode);
					break;

				case GameMainBtnType.Main:
					page = await CreateWidgetByTypeAsync<MainPage>(m_tfPageNode);
					break;

				case GameMainBtnType.League:
					page = await CreateWidgetByTypeAsync<LeaguePage>(m_tfPageNode);
					break;

				case GameMainBtnType.GamePlay:
					page = await CreateWidgetByTypeAsync<GamePlayPage>(m_tfPageNode);
					break;

				case GameMainBtnType.Home:
					page = await CreateWidgetByTypeAsync<HomePage>(m_tfPageNode);
					break;
			}
			m_loadingPage.Remove(btnType);
			if (page == null)
			{
				DLogger.Warning($"加载界面失败: {btnType.ToString()}");
				return;
			}
			m_pageDict.Add(btnType, page);
			page.Show(m_curShowPageType == btnType);
			onSwitchPage?.Invoke(page);
		}

		private bool CheckPageIsOpen(FuncType funcType)
		{
			if (!FuncOpenMgr.Instance.CheckFuncOpen(funcType, true))
			{
				return false;
			}
			return true;
		}

		#endregion
		
		#region 事件

		private partial void OnClickGmBtn()
		{
			
		}

		#endregion
	}
}
