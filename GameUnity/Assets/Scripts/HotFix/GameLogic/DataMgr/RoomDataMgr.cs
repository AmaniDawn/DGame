using System.Collections.Generic;
using Fantasy;
using GameProto;

namespace GameLogic
{
    /// <summary>
    /// 客户端房间运行时数据管理器。
    /// </summary>
    public sealed class RoomDataMgr : DataCenterModule<RoomDataMgr>
    {
        /// <summary>
        /// 当前房间信息。
        /// </summary>
        public CSRoomInfo CurrentRoomInfo { get; private set; }

        /// <summary>
        /// 当前房间玩家信息列表。
        /// </summary>
        public List<CSRoomPlayerInfo> PlayerInfos { get; } = new List<CSRoomPlayerInfo>();

        /// <summary>
        /// 当前房间玩家数量。
        /// </summary>
        public int PlayerCount { get; private set; }

        /// <summary>
        /// 是否已加入房间。
        /// </summary>
        public bool HasRoom => CurrentRoomInfo != null && CurrentRoomInfo.RoomId > 0;

        /// <summary>
        /// 角色登出时清理房间数据。
        /// </summary>
        public override void OnRoleLogout()
        {
            Clear();
        }

        /// <summary>
        /// 清空当前房间数据。
        /// </summary>
        public void Clear()
        {
            CurrentRoomInfo = null;
            PlayerCount = 0;
            PlayerInfos.Clear();
        }

        /// <summary>
        /// 同步当前房间数据。
        /// </summary>
        /// <param name="roomInfo">房间信息。</param>
        /// <param name="playerCount">当前房间玩家数量。</param>
        /// <param name="playerInfos">当前房间玩家信息。</param>
        public void SyncRoom(CSRoomInfo roomInfo, int playerCount, List<CSRoomPlayerInfo> playerInfos)
        {
            if (roomInfo == null)
            {
                Clear();
                return;
            }

            CurrentRoomInfo = new CSRoomInfo
            {
                RoomId = roomInfo.RoomId,
                RoomSeq = roomInfo.RoomSeq
            };

            PlayerCount = playerCount;
            PlayerInfos.Clear();
            if (playerInfos == null)
            {
                return;
            }

            for (int i = 0; i < playerInfos.Count; i++)
            {
                var playerInfo = playerInfos[i];
                if (playerInfo == null)
                {
                    continue;
                }

                PlayerInfos.Add(new CSRoomPlayerInfo
                {
                    RoleId = playerInfo.RoleId,
                    RoleName = playerInfo.RoleName,
                    Level = playerInfo.Level,
                    FightValue = playerInfo.FightValue
                });
            }
        }
    }
}