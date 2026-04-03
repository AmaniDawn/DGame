using DGame;
using Fantasy.Async;

namespace GameLogic
{
	public partial class MainPage
	{
		#region Override

		protected override void RegisterEvent()
		{
			AddUIEvent(IRoomLogicEvent_Event.OnRoomDataChange, RefreshRoomPlayerTexts);
		}

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
			if (!IsRoomOwner())
			{
				GameModule.UIModule.ShowTipsUI(G.R("只有房主可以开始战斗"));
				return;
			}

			BattleNetMgr.Instance.StartBattleRequest().Coroutine();
		}

		private partial void OnClickJoinBtn()
		{
			JoinRoom().Coroutine();
		}

		private partial void OnClickLeaveBtn()
		{
			LeaveRoom().Coroutine();
		}

		private partial void OnClickCreateBtn()
		{
			CreateRoom().Coroutine();
		}

		#endregion

		#region Private

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

		private void RefreshRoomPlayerTexts()
		{
			if (!RoomDataMgr.Instance.HasRoom)
			{
				m_textRoomId.text = "房间号：暂无";
				m_textPlayer1.text = "暂无房主";
				m_textPlayer2.text = "暂无队员";
				m_btnStart.SetActive(false);
				return;
			}

			var roomInfo = RoomDataMgr.Instance.CurrentRoomInfo;
			m_textRoomId.text = roomInfo == null ? "房间号：暂无" : $"房间号：{roomInfo.RoomId}";

			var captainRoleId = roomInfo?.CaptainRoleId ?? 0;
			var ownerName = "暂无房主";
			var memberName = "暂无队员";
			var playerInfos = RoomDataMgr.Instance.PlayerInfos;
			for (var i = 0; i < playerInfos.Count; i++)
			{
				var playerInfo = playerInfos[i];
				if (playerInfo == null)
				{
					continue;
				}

				if (playerInfo.RoleId == captainRoleId)
				{
					ownerName = $"房主：{playerInfo.RoleName}";
				}
				else
				{
					memberName = $"队员：{playerInfo.RoleName}";
				}
			}

			m_textPlayer1.text = ownerName;
			m_textPlayer2.text = memberName;
			m_btnStart.SetActive(IsRoomOwner());
		}

		private bool IsRoomOwner()
		{
			var roomInfo = RoomDataMgr.Instance.CurrentRoomInfo;
			return roomInfo != null && roomInfo.CaptainRoleId > 0 && roomInfo.CaptainRoleId == DataCenterSys.Instance.CurRoleID;
		}

		#endregion
	}
}
