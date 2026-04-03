using System.Collections.Generic;
using DGame;
using Fantasy;
using GameProto;

namespace GameLogic
{
    public sealed class RoomDataMgr : DataCenterModule<RoomDataMgr>
    {
        public CSRoomInfo CurrentRoomInfo { get; private set; }

        public List<CSRoomPlayerInfo> PlayerInfos { get; } = new List<CSRoomPlayerInfo>();

        public int PlayerCount { get; private set; }

        public bool HasRoom => CurrentRoomInfo != null && CurrentRoomInfo.RoomId > 0;

        public override void OnRoleLogout()
        {
            Clear();
        }

        public void Clear()
        {
            CurrentRoomInfo = null;
            PlayerCount = 0;
            PlayerInfos.Clear();
            GameEvent.Get<IRoomLogicEvent>().OnRoomDataChange();
        }

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
                RoomSeq = roomInfo.RoomSeq,
                CaptainRoleId = roomInfo.CaptainRoleId,
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

            GameEvent.Get<IRoomLogicEvent>().OnRoomDataChange();
        }
    }
}
