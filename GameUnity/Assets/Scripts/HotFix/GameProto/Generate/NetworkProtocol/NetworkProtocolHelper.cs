using System.Runtime.CompilerServices;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using System.Collections.Generic;
#pragma warning disable CS8618
namespace Fantasy
{
   public static class NetworkProtocolHelper
   {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateRoomResponse> C2G_CreateRoomRequest(this Session session, C2G_CreateRoomRequest C2G_CreateRoomRequest_request)
		{
			return (G2C_CreateRoomResponse)await session.Call(C2G_CreateRoomRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_CreateRoomResponse> C2G_CreateRoomRequest(this Session session, int playerCount)
		{
			using var C2G_CreateRoomRequest_request = Fantasy.C2G_CreateRoomRequest.Create();
			C2G_CreateRoomRequest_request.PlayerCount = playerCount;
			return (G2C_CreateRoomResponse)await session.Call(C2G_CreateRoomRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_JoinRoomResponse> C2G_JoinRoomRequest(this Session session, C2G_JoinRoomRequest C2G_JoinRoomRequest_request)
		{
			return (G2C_JoinRoomResponse)await session.Call(C2G_JoinRoomRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_JoinRoomResponse> C2G_JoinRoomRequest(this Session session, int roomId)
		{
			using var C2G_JoinRoomRequest_request = Fantasy.C2G_JoinRoomRequest.Create();
			C2G_JoinRoomRequest_request.RoomId = roomId;
			return (G2C_JoinRoomResponse)await session.Call(C2G_JoinRoomRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LeaveRoomResponse> C2G_LeaveRoomRequest(this Session session, C2G_LeaveRoomRequest C2G_LeaveRoomRequest_request)
		{
			return (G2C_LeaveRoomResponse)await session.Call(C2G_LeaveRoomRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LeaveRoomResponse> C2G_LeaveRoomRequest(this Session session)
		{
			using var C2G_LeaveRoomRequest_request = Fantasy.C2G_LeaveRoomRequest.Create();
			return (G2C_LeaveRoomResponse)await session.Call(C2G_LeaveRoomRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_RoomPlayerInfoChangedNotify(this Session session, G2C_RoomPlayerInfoChangedNotify G2C_RoomPlayerInfoChangedNotify_message)
		{
			session.Send(G2C_RoomPlayerInfoChangedNotify_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_RoomPlayerInfoChangedNotify(this Session session, CSRoomInfo roomInfo, int playerCount, List<CSRoomPlayerInfo> playerInfos)
		{
			using var G2C_RoomPlayerInfoChangedNotify_message = Fantasy.G2C_RoomPlayerInfoChangedNotify.Create();
			G2C_RoomPlayerInfoChangedNotify_message.RoomInfo = roomInfo;
			G2C_RoomPlayerInfoChangedNotify_message.PlayerCount = playerCount;
			G2C_RoomPlayerInfoChangedNotify_message.PlayerInfos = playerInfos;
			session.Send(G2C_RoomPlayerInfoChangedNotify_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2S_SyncFrameDataReq(this Session session, C2S_SyncFrameDataReq C2S_SyncFrameDataReq_message)
		{
			session.Send(C2S_SyncFrameDataReq_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2S_SyncFrameDataReq(this Session session, int forecastFrameId, int revClientFrameId, CSRoomInfo roomInfo, int roomPlayerId, CSOnePlayerFrameCmd frameData)
		{
			using var C2S_SyncFrameDataReq_message = Fantasy.C2S_SyncFrameDataReq.Create();
			C2S_SyncFrameDataReq_message.ForecastFrameId = forecastFrameId;
			C2S_SyncFrameDataReq_message.RevClientFrameId = revClientFrameId;
			C2S_SyncFrameDataReq_message.RoomInfo = roomInfo;
			C2S_SyncFrameDataReq_message.RoomPlayerId = roomPlayerId;
			C2S_SyncFrameDataReq_message.FrameData = frameData;
			session.Send(C2S_SyncFrameDataReq_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_BattleFinClientDataReq(this Session session, S2C_BattleFinClientDataReq S2C_BattleFinClientDataReq_message)
		{
			session.Send(S2C_BattleFinClientDataReq_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_BattleFinClientDataReq(this Session session, CSBattleStartParam startParam, uint durationTime)
		{
			using var S2C_BattleFinClientDataReq_message = Fantasy.S2C_BattleFinClientDataReq.Create();
			S2C_BattleFinClientDataReq_message.StartParam = startParam;
			S2C_BattleFinClientDataReq_message.DurationTime = durationTime;
			session.Send(S2C_BattleFinClientDataReq_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<S2C_StartBattleResponse> C2S_StartBattleRequest(this Session session, C2S_StartBattleRequest C2S_StartBattleRequest_request)
		{
			return (S2C_StartBattleResponse)await session.Call(C2S_StartBattleRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<S2C_StartBattleResponse> C2S_StartBattleRequest(this Session session)
		{
			using var C2S_StartBattleRequest_request = Fantasy.C2S_StartBattleRequest.Create();
			return (S2C_StartBattleResponse)await session.Call(C2S_StartBattleRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_NotifyBattleLoading(this Session session, S2C_NotifyBattleLoading S2C_NotifyBattleLoading_message)
		{
			session.Send(S2C_NotifyBattleLoading_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_NotifyBattleLoading(this Session session)
		{
			using var message = Fantasy.S2C_NotifyBattleLoading.Create();
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<S2C_BattleLoadDoneResponse> C2S_BattleLoadDoneRequest(this Session session, C2S_BattleLoadDoneRequest C2S_BattleLoadDoneRequest_request)
		{
			return (S2C_BattleLoadDoneResponse)await session.Call(C2S_BattleLoadDoneRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<S2C_BattleLoadDoneResponse> C2S_BattleLoadDoneRequest(this Session session)
		{
			using var C2S_BattleLoadDoneRequest_request = Fantasy.C2S_BattleLoadDoneRequest.Create();
			return (S2C_BattleLoadDoneResponse)await session.Call(C2S_BattleLoadDoneRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_NotifyEnterBattle(this Session session, S2C_NotifyEnterBattle S2C_NotifyEnterBattle_message)
		{
			session.Send(S2C_NotifyEnterBattle_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_NotifyEnterBattle(this Session session, int randSeed, int playerCount, byte isHaveRoomInfo, CSRoomInfo roomInfoList, int battleStatus, byte isGuide, uint startTime, ulong battleGID, byte multiPlayerBattle, ulong captainPlayerId, List<CSLevelPlayerData> playerDataList, CSChapterInfo chapter, int stage, int mapID)
		{
			using var S2C_NotifyEnterBattle_message = Fantasy.S2C_NotifyEnterBattle.Create();
			S2C_NotifyEnterBattle_message.RandSeed = randSeed;
			S2C_NotifyEnterBattle_message.PlayerCount = playerCount;
			S2C_NotifyEnterBattle_message.IsHaveRoomInfo = isHaveRoomInfo;
			S2C_NotifyEnterBattle_message.RoomInfoList = roomInfoList;
			S2C_NotifyEnterBattle_message.BattleStatus = battleStatus;
			S2C_NotifyEnterBattle_message.IsGuide = isGuide;
			S2C_NotifyEnterBattle_message.StartTime = startTime;
			S2C_NotifyEnterBattle_message.BattleGID = battleGID;
			S2C_NotifyEnterBattle_message.MultiPlayerBattle = multiPlayerBattle;
			S2C_NotifyEnterBattle_message.CaptainPlayerId = captainPlayerId;
			S2C_NotifyEnterBattle_message.PlayerDataList = playerDataList;
			S2C_NotifyEnterBattle_message.Chapter = chapter;
			S2C_NotifyEnterBattle_message.Stage = stage;
			S2C_NotifyEnterBattle_message.MapID = mapID;
			session.Send(S2C_NotifyEnterBattle_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CSChapterInfo(this Session session, CSChapterInfo CSChapterInfo_message)
		{
			session.Send(CSChapterInfo_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CSChapterInfo(this Session session, int chapterID, int difficult)
		{
			using var CSChapterInfo_message = Fantasy.CSChapterInfo.Create();
			CSChapterInfo_message.ChapterID = chapterID;
			CSChapterInfo_message.Difficult = difficult;
			session.Send(CSChapterInfo_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_BroadcastFrameData(this Session session, S2C_BroadcastFrameData S2C_BroadcastFrameData_message)
		{
			session.Send(S2C_BroadcastFrameData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_BroadcastFrameData(this Session session, CSRoomInfo roomInfo, int sveFrameId, int frameCount, List<CSSyncOneFrameData> frameDataList)
		{
			using var S2C_BroadcastFrameData_message = Fantasy.S2C_BroadcastFrameData.Create();
			S2C_BroadcastFrameData_message.RoomInfo = roomInfo;
			S2C_BroadcastFrameData_message.SveFrameId = sveFrameId;
			S2C_BroadcastFrameData_message.FrameCount = frameCount;
			S2C_BroadcastFrameData_message.FrameDataList = frameDataList;
			session.Send(S2C_BroadcastFrameData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_QueryFuncOpenListResponse> C2G_QueryFuncOpenListRequest(this Session session, C2G_QueryFuncOpenListRequest C2G_QueryFuncOpenListRequest_request)
		{
			return (G2C_QueryFuncOpenListResponse)await session.Call(C2G_QueryFuncOpenListRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_QueryFuncOpenListResponse> C2G_QueryFuncOpenListRequest(this Session session)
		{
			using var C2G_QueryFuncOpenListRequest_request = Fantasy.C2G_QueryFuncOpenListRequest.Create();
			return (G2C_QueryFuncOpenListResponse)await session.Call(C2G_QueryFuncOpenListRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_FuncOpenNotify(this Session session, G2C_FuncOpenNotify G2C_FuncOpenNotify_message)
		{
			session.Send(G2C_FuncOpenNotify_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_FuncOpenNotify(this Session session, List<int> newOpenFuncList)
		{
			using var G2C_FuncOpenNotify_message = Fantasy.G2C_FuncOpenNotify.Create();
			G2C_FuncOpenNotify_message.NewOpenFuncList = newOpenFuncList;
			session.Send(G2C_FuncOpenNotify_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_RegisterResponse> C2A_RegisterRequest(this Session session, C2A_RegisterRequest C2A_RegisterRequest_request)
		{
			return (A2C_RegisterResponse)await session.Call(C2A_RegisterRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_RegisterResponse> C2A_RegisterRequest(this Session session, string userName, string password)
		{
			using var C2A_RegisterRequest_request = Fantasy.C2A_RegisterRequest.Create();
			C2A_RegisterRequest_request.UserName = userName;
			C2A_RegisterRequest_request.Password = password;
			return (A2C_RegisterResponse)await session.Call(C2A_RegisterRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_LoginResponse> C2A_LoginRequest(this Session session, C2A_LoginRequest C2A_LoginRequest_request)
		{
			return (A2C_LoginResponse)await session.Call(C2A_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<A2C_LoginResponse> C2A_LoginRequest(this Session session, string userName, string password, uint loginType)
		{
			using var C2A_LoginRequest_request = Fantasy.C2A_LoginRequest.Create();
			C2A_LoginRequest_request.UserName = userName;
			C2A_LoginRequest_request.Password = password;
			C2A_LoginRequest_request.LoginType = loginType;
			return (A2C_LoginResponse)await session.Call(C2A_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2A_RecordRecentServer(this Session session, C2A_RecordRecentServer C2A_RecordRecentServer_message)
		{
			session.Send(C2A_RecordRecentServer_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2A_RecordRecentServer(this Session session, long roleID, int serverID)
		{
			using var C2A_RecordRecentServer_message = Fantasy.C2A_RecordRecentServer.Create();
			C2A_RecordRecentServer_message.RoleID = roleID;
			C2A_RecordRecentServer_message.ServerID = serverID;
			session.Send(C2A_RecordRecentServer_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, C2G_LoginRequest C2G_LoginRequest_request)
		{
			return (G2C_LoginResponse)await session.Call(C2G_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_LoginResponse> C2G_LoginRequest(this Session session, string token, int serverID)
		{
			using var C2G_LoginRequest_request = Fantasy.C2G_LoginRequest.Create();
			C2G_LoginRequest_request.Token = token;
			C2G_LoginRequest_request.ServerID = serverID;
			return (G2C_LoginResponse)await session.Call(C2G_LoginRequest_request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_RepeatLogin(this Session session, G2C_RepeatLogin G2C_RepeatLogin_message)
		{
			session.Send(G2C_RepeatLogin_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_RepeatLogin(this Session session)
		{
			using var message = Fantasy.G2C_RepeatLogin.Create();
			session.Send(message);
		}

   }
}