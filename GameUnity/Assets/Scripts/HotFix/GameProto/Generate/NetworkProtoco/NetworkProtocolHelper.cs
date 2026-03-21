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
		public static void S2C_BattleFinClientData(this Session session, S2C_BattleFinClientData S2C_BattleFinClientData_message)
		{
			session.Send(S2C_BattleFinClientData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_BattleFinClientData(this Session session, CSBattleStartParam startParam, uint durationTime)
		{
			using var S2C_BattleFinClientData_message = Fantasy.S2C_BattleFinClientData.Create();
			S2C_BattleFinClientData_message.StartParam = startParam;
			S2C_BattleFinClientData_message.DurationTime = durationTime;
			session.Send(S2C_BattleFinClientData_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_NotifyEnterBattle(this Session session, S2C_NotifyEnterBattle S2C_NotifyEnterBattle_message)
		{
			session.Send(S2C_NotifyEnterBattle_message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void S2C_NotifyEnterBattle(this Session session, int randSeed, int playerCount, byte isHaveRoomInfo, List<CSRoomInfo> roomInfoList, int battleStatus, byte isGuide, uint startTime, ulong battleGID, byte multiPlayerBattle, ulong captainPlayerId)
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
			session.Send(S2C_NotifyEnterBattle_message);
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