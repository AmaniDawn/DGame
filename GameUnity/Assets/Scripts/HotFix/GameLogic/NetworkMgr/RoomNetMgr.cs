using DGame;
using Fantasy;
using Fantasy.Async;
using GameProto;

namespace GameLogic
{
    /// <summary>
    /// 房间相关网络请求管理器。
    /// </summary>
    public sealed class RoomNetMgr : Singleton<RoomNetMgr>
    {
        /// <summary>
        /// 请求创建房间。
        /// </summary>
        /// <param name="playerCount">房间最大玩家数量。</param>
        /// <returns>成功时返回 true。</returns>
        public async FTask<bool> CreateRoomRequest(int playerCount)
        {
            var response = await GameClient.Instance.Call(new C2G_CreateRoomRequest
            {
                PlayerCount = playerCount
            });

            if (response is not G2C_CreateRoomResponse createRoomResponse)
            {
                return false;
            }

            if (createRoomResponse.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(createRoomResponse.ErrorCode);
                return false;
            }

            RoomDataMgr.Instance.SyncRoom(createRoomResponse.RoomInfo, createRoomResponse.PlayerCount, createRoomResponse.PlayerInfos);
            return true;
        }

        /// <summary>
        /// 请求加入房间。
        /// </summary>
        /// <param name="roomId">房间 ID。</param>
        /// <returns>成功时返回 true。</returns>
        public async FTask<bool> JoinRoomRequest(int roomId)
        {
            var response = await GameClient.Instance.Call(new C2G_JoinRoomRequest
            {
                RoomId = roomId
            });

            if (response is not G2C_JoinRoomResponse joinRoomResponse)
            {
                return false;
            }

            if (joinRoomResponse.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(joinRoomResponse.ErrorCode);
                return false;
            }

            RoomDataMgr.Instance.SyncRoom(joinRoomResponse.RoomInfo, joinRoomResponse.PlayerCount, joinRoomResponse.PlayerInfos);
            return true;
        }

        /// <summary>
        /// 请求离开当前房间。
        /// </summary>
        /// <returns>成功时返回 true。</returns>
        public async FTask<bool> LeaveRoomRequest()
        {
            var response = await GameClient.Instance.Call(new C2G_LeaveRoomRequest());
            if (response is not G2C_LeaveRoomResponse leaveRoomResponse)
            {
                return false;
            }

            if (leaveRoomResponse.ErrorCode != 0)
            {
                GameModule.UIModule.ShowTipsUI(leaveRoomResponse.ErrorCode);
                return false;
            }

            RoomDataMgr.Instance.Clear();
            return true;
        }
    }
}
