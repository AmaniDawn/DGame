using System.Collections.Generic;
using DGame;
using Fantasy;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 进入战斗辅助方法。
    /// </summary>
    public static class BattleEnterHelper
    {
        private const int DefaultPlayerSpacing = 3;
        private const int DefaultMoveSpeed = 5;

        /// <summary>
        /// 根据服务端下发的进入战斗消息创建战斗和玩家单位。
        /// </summary>
        /// <param name="message">进入战斗消息。</param>
        public static void EnterBattle(S2C_NotifyEnterBattle message)
        {
            if (message == null)
            {
                return;
            }

            if (BattleManager.CurBattleContext != null)
            {
                BattleManager.DestroyBattle();
            }

            var battleContext = BattleManager.CreateBattle();
            var playerDataList = message.PlayerDataList ?? new List<CSLevelPlayerData>();
            if (playerDataList.Count == 0)
            {
                return;
            }

            var battleCenter = GetBattleCenter(message.MapID);
            for (var i = 0; i < playerDataList.Count; i++)
            {
                var createData = BuildPlayerCreateData(playerDataList[i], i, playerDataList.Count, battleCenter);
                if (createData == null)
                {
                    continue;
                }

                battleContext.CreateLogicUnit(createData);
            }
        }

        private static LogicUnitCreateData BuildPlayerCreateData(CSLevelPlayerData playerData, int index, int playerCount, FixedPointVector3 battleCenter)
        {
            if (playerData?.PlayerShowData == null || playerData.PlayerBattleData?.PlayerBaseData?.AttrData == null)
            {
                return null;
            }

            var playerShowData = playerData.PlayerShowData;
            var playerBattleData = playerData.PlayerBattleData;
            var playerBaseData = playerBattleData.PlayerBaseData;
            var attrData = playerBaseData.AttrData;
            var roleId = playerShowData.RoleID;
            var bornPosition = BuildBornPosition(index, playerCount, battleCenter);

            return LogicUnitCreateData.CreatePlayer(
                    index + 1,
                    actorId: (long)roleId,
                    weaponConfigId: playerBaseData.WeaponFashionID,
                    clothingConfigId: playerBaseData.FashionID,
                    roleBodyType: playerBaseData.BodyType)
                .SetUnitId(roleId)
                .SetOwnerUnitId(roleId)
                .SetUnitName(playerShowData.RoleName)
                .SetBaseAttr(attrData.Atk, attrData.MaxHp, attrData.MoveSpeed > 0 ? attrData.MoveSpeed : DefaultMoveSpeed)
                .SetBornPose(bornPosition, FixedPointVector3.forward, FixedPointVector3.one);
        }

        private static FixedPointVector3 GetBattleCenter(int mapId)
        {
            var mapConfig = LevelConfigMgr.Instance.GetMapConfigOrDefault(mapId);
            if (mapConfig?.BattleCenter == null)
            {
                return FixedPointVector3.zero;
            }

            return new FixedPointVector3(mapConfig.BattleCenter.X, 0, mapConfig.BattleCenter.Y);
        }

        private static FixedPointVector3 BuildBornPosition(int index, int playerCount, FixedPointVector3 battleCenter)
        {
            var offsetX = (index - (playerCount - 1) * 0.5f) * DefaultPlayerSpacing;
            return new FixedPointVector3(battleCenter.x + offsetX, battleCenter.y, battleCenter.z);
        }
    }
}
