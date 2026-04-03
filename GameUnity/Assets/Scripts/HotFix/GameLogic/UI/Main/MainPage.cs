using DGame;
using Fantasy.Async;

namespace GameLogic
{
	public partial class MainPage
	{
		#region Override

		protected override void OnRefresh()
		{
			RefreshRoomPlayerTexts();
		}

		public override void OnPageShowed(int oldShowType, int newShowType)
		{
			base.OnPageShowed(oldShowType, newShowType);
			RefreshRoomPlayerTexts();
		}

		public override void RefreshCurrentChildPage()
		{
			base.RefreshCurrentChildPage();
			RefreshRoomPlayerTexts();
		}

		#endregion

		#region 事件

		private partial void OnClickStartBtn()
		{
			CreateRoom().Coroutine();
		}

		private partial void OnClickJoinBtn()
		{
			JoinRoom().Coroutine();
		}

		private partial void OnClickLeaveBtn()
		{
			LeaveRoom().Coroutine();
		}

		#endregion

		#region Private

		/// <summary>
		/// 创建一个双人房间。
		/// </summary>
		private async FTask CreateRoom()
		{
			if (!await RoomNetMgr.Instance.CreateRoomRequest(2))
			{
				return;
			}

			var roomInfo = RoomDataMgr.Instance.CurrentRoomInfo;
			if (roomInfo != null)
			{
				m_inputRoomId.text = roomInfo.RoomId.ToString();
			}

			RefreshRoomPlayerTexts();
		}

		/// <summary>
		/// 根据输入框中的房间 ID 加入房间。
		/// </summary>
		private async FTask JoinRoom()
		{
			if (!int.TryParse(m_inputRoomId.text, out var roomId) || roomId <= 0)
			{
				GameModule.UIModule.ShowTipsUI(G.R("请输入正确的房间ID"));
				return;
			}

			if (!await RoomNetMgr.Instance.JoinRoomRequest(roomId))
			{
				return;
			}

			RefreshRoomPlayerTexts();
		}

		/// <summary>
		/// 离开当前房间。
		/// </summary>
		private async FTask LeaveRoom()
		{
			if (!RoomDataMgr.Instance.HasRoom)
			{
				GameModule.UIModule.ShowTipsUI(G.R("暂未加入房间"));
				return;
			}

			if (!await RoomNetMgr.Instance.LeaveRoomRequest())
			{
				return;
			}

			RefreshRoomPlayerTexts();
		}

		/// <summary>
		/// 刷新房间玩家文本显示。
		/// </summary>
		private void RefreshRoomPlayerTexts()
		{
			if (!RoomDataMgr.Instance.HasRoom)
			{
				m_textPlayer1.text = "暂未加入房间";
				m_textPlayer2.text = "暂未加入房间";
				return;
			}

			var playerInfos = RoomDataMgr.Instance.PlayerInfos;
			m_textPlayer1.text = playerInfos.Count > 0
				? $"玩家1：{playerInfos[0].RoleId}"
				: "暂未加入房间";
			m_textPlayer2.text = playerInfos.Count > 1
				? $"玩家2：{playerInfos[1].RoleId}"
				: "暂未加入玩家";
		}

		#endregion
	}
}
