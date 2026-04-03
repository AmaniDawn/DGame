using System.Collections.Generic;
using Fantasy;
using Fantasy.Network;

namespace Hotfix;

public static class BattleGateNotifyHelper
{
    public static void BroadcastBattleLoading(this Scene scene, List<long> sessionRuntimeIds)
    {
        if (sessionRuntimeIds == null || sessionRuntimeIds.Count == 0)
        {
            return;
        }

        foreach (var sessionRuntimeId in sessionRuntimeIds)
        {
            if (!scene.TryGetEntity<Session>(sessionRuntimeId, out var targetSession) || targetSession == null)
            {
                continue;
            }

            targetSession.Send(new S2C_NotifyBattleLoading());
        }
    }

    public static void BroadcastEnterBattle(this Scene scene, G2Game_StartBattleResponse response)
    {
        if (response == null || response.SessionRuntimeIds == null || response.SessionRuntimeIds.Count == 0)
        {
            return;
        }

        foreach (var sessionRuntimeId in response.SessionRuntimeIds)
        {
            if (!scene.TryGetEntity<Session>(sessionRuntimeId, out var targetSession) || targetSession == null)
            {
                continue;
            }

            targetSession.Send(response.ToEnterBattleNotify());
        }
    }

    private static S2C_NotifyEnterBattle ToEnterBattleNotify(this G2Game_StartBattleResponse response)
        => new()
        {
            RandSeed = response.RandSeed,
            PlayerCount = response.PlayerCount,
            IsHaveRoomInfo = response.RoomId > 0 ? (byte)1 : (byte)0,
            RoomInfoList = response.RoomId > 0
                ? new CSRoomInfo
                {
                    RoomId = response.RoomId,
                    RoomSeq = response.RoomSeq,
                    CaptainRoleId = response.CaptainPlayerId,
                }
                : null,
            BattleStatus = response.BattleStatus,
            IsGuide = response.IsGuide,
            StartTime = response.StartTime,
            BattleGID = response.BattleGID,
            MultiPlayerBattle = response.MultiPlayerBattle,
            CaptainPlayerId = response.CaptainPlayerId,
            PlayerDataList = ClonePlayerDataList(response.PlayerDataList),
            Chapter = response.Chapter?.Clone(),
            Stage = response.Stage,
            MapID = response.MapID,
        };

    private static List<CSLevelPlayerData> ClonePlayerDataList(List<CSLevelPlayerData> source)
    {
        if (source == null || source.Count == 0)
        {
            return new List<CSLevelPlayerData>();
        }

        var result = new List<CSLevelPlayerData>(source.Count);
        foreach (var playerData in source)
        {
            if (playerData == null)
            {
                continue;
            }

            result.Add(playerData.Clone());
        }

        return result;
    }

    private static CSChapterInfo Clone(this CSChapterInfo source)
        => new()
        {
            ChapterID = source.ChapterID,
            Difficult = source.Difficult,
        };

    private static CSLevelPlayerData Clone(this CSLevelPlayerData source)
        => new()
        {
            PlayerShowData = source.PlayerShowData?.Clone(),
            PlayerBattleData = source.PlayerBattleData?.Clone(),
        };

    private static CSMiniRoleBaseShowData Clone(this CSMiniRoleBaseShowData source)
        => new()
        {
            Uin = source.Uin,
            RoleID = source.RoleID,
            WorldID = source.WorldID,
            Online = source.Online,
            FightVal = source.FightVal,
            VIPLevel = source.VIPLevel,
            RoleName = source.RoleName,
            Sex = source.Sex,
            Head = source.Head,
            HeadSex = source.HeadSex,
            HeadURL = source.HeadURL,
            HeadFrame = source.HeadFrame,
        };

    private static CSBattlePlayerData Clone(this CSBattlePlayerData source)
        => new()
        {
            RoleID = source.RoleID,
            PlayerBaseData = source.PlayerBaseData?.Clone(),
            PlayerRunData = source.PlayerRunData?.Clone(),
        };

    private static CSBattlePlayerBaseData Clone(this CSBattlePlayerBaseData source)
        => new()
        {
            PlayerLevel = source.PlayerLevel,
            BodyType = source.BodyType,
            FashionID = source.FashionID,
            WeaponFashionID = source.WeaponFashionID,
            CreateRoleDays = source.CreateRoleDays,
            FailureCount = source.FailureCount,
            AttrData = source.AttrData?.Clone(),
        };

    private static CSLevelUnitRunData Clone(this CSLevelUnitRunData source)
        => new()
        {
            Hp = source.Hp,
            Level = source.Level,
            Exp = source.Exp,
            Gold = source.Gold,
        };

    private static CSUnitBattleAttrData Clone(this CSUnitBattleAttrData source)
        => new()
        {
            Atk = source.Atk,
            Hp = source.Hp,
            MaxHp = source.MaxHp,
            MoveSpeed = source.MoveSpeed,
        };
}
