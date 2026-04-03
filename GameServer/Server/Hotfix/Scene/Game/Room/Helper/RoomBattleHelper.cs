using System;
using System.Collections.Generic;
using System.Linq;
using Fantasy;
using Fantasy.Helper;
using GameProto;

namespace Hotfix;

public static class RoomBattleHelper
{
    public static void ClearBattleProgress(this RoomComponent roomComponent)
    {
        roomComponent.IsBattleStarted = false;
        roomComponent.IsBattleEntered = false;

        foreach (var playerInfo in roomComponent.PlayerInfos.Values)
        {
            playerInfo.IsBattleReady = false;
            playerInfo.IsBattleLoaded = false;
        }
    }

    public static void BeginBattleLoading(this RoomComponent roomComponent)
    {
        foreach (var playerInfo in roomComponent.PlayerInfos.Values)
        {
            playerInfo.IsBattleReady = false;
            playerInfo.IsBattleLoaded = false;
        }

        roomComponent.IsBattleStarted = true;
        roomComponent.IsBattleEntered = false;
    }

    public static bool MarkBattleLoaded(this RoomComponent roomComponent, long roleId)
    {
        if (!roomComponent.TryGetPlayer(roleId, out var playerInfo) || playerInfo == null)
        {
            return false;
        }

        playerInfo.IsBattleLoaded = true;
        return true;
    }

    public static bool AreAllPlayersBattleLoaded(this RoomComponent roomComponent)
        => roomComponent.PlayerInfos.Count > 0 && roomComponent.PlayerInfos.Values.All(static player => player.IsBattleLoaded);

    public static void FillBattleLoadingSessions(this G2Game_StartBattleResponse response, RoomComponent roomComponent)
    {
        response.SessionRuntimeIds = roomComponent.GetOrderedPlayers()
            .Where(static player => player.SessionRuntimeId != 0)
            .Select(static player => player.SessionRuntimeId)
            .Distinct()
            .ToList();
    }

    public static bool TryGetBattleChapterConfig(this RoomComponent roomComponent, out ChapterConfig? chapterConfig)
    {
        var playerCount = roomComponent.GetPlayerCount();
        chapterConfig = TbChapterConfig.DataList
            .Where(static config => config is { CanMultiPlayer: true } && config.MapID > 0)
            .OrderBy(static config => config.ChapterID)
            .FirstOrDefault(config => config.MultiPlayerCnt == playerCount);

        chapterConfig ??= TbChapterConfig.DataList
            .Where(static config => config is { CanMultiPlayer: true } && config.MapID > 0)
            .OrderBy(static config => config.ChapterID)
            .FirstOrDefault();

        return chapterConfig != null;
    }

    public static void FillBattleStart(this G2Game_StartBattleResponse response, RoomComponent roomComponent, ChapterConfig chapterConfig)
    {
        var players = roomComponent.GetOrderedPlayers();

        response.RoomId = roomComponent.RoomId;
        response.RoomSeq = roomComponent.RoomSeq;
        response.PlayerCount = players.Count;
        response.RandSeed = BuildBattleRandSeed(roomComponent);
        response.BattleStatus = 0;
        response.IsGuide = 0;
        response.StartTime = unchecked((uint)TimeHelper.Now);
        response.BattleGID = BuildBattleGid(roomComponent);
        response.MultiPlayerBattle = players.Count > 1 ? (byte)1 : (byte)0;
        response.CaptainPlayerId = roomComponent.CaptainRoleId > 0
            ? (ulong)roomComponent.CaptainRoleId
            : players.Count > 0 ? (ulong)players[0].RoleId : 0;
        response.Stage = 1;
        response.MapID = chapterConfig.MapID;
        response.SessionRuntimeIds = players
            .Where(static player => player.SessionRuntimeId != 0)
            .Select(static player => player.SessionRuntimeId)
            .Distinct()
            .ToList();
        response.PlayerDataList = new List<CSLevelPlayerData>(players.Count);
        for (var i = 0; i < players.Count; i++)
        {
            response.PlayerDataList.Add(players[i].ToCSLevelPlayerData(i + 1));
        }

        response.Chapter = new CSChapterInfo
        {
            ChapterID = chapterConfig.ChapterID,
            Difficult = chapterConfig.Difficult,
        };
    }

    private static int BuildBattleRandSeed(RoomComponent roomComponent)
        => roomComponent.RoomId ^ roomComponent.RoomSeq ^ unchecked((int)TimeHelper.Now);

    private static ulong BuildBattleGid(RoomComponent roomComponent)
        => ((ulong)(uint)roomComponent.RoomId << 32) | (uint)roomComponent.RoomSeq;

    private static CSLevelPlayerData ToCSLevelPlayerData(this RoomPlayerInfo playerInfo, int playerId)
    {
        var maxHp = BuildMaxHp(playerInfo);
        return new CSLevelPlayerData
        {
            PlayerShowData = new CSMiniRoleBaseShowData
            {
                RoleID = (ulong)playerInfo.RoleId,
                WorldID = (uint)Math.Max(playerInfo.ServerId, 0),
                Online = 1,
                FightVal = playerInfo.FightValue,
                RoleName = playerInfo.RoleName,
                Sex = playerInfo.Sex,
                Head = (uint)Math.Max(playerInfo.HeadId, 0),
            },
            PlayerBattleData = new CSBattlePlayerData
            {
                RoleID = (ulong)playerInfo.RoleId,
                PlayerBaseData = new CSBattlePlayerBaseData
                {
                    PlayerLevel = (int)playerInfo.Level,
                    BodyType = (byte)ToRoleBodyType(playerInfo.Sex),
                    FashionID = 0,
                    WeaponFashionID = 0,
                    CreateRoleDays = 0,
                    FailureCount = 0,
                    AttrData = new CSUnitBattleAttrData
                    {
                        Atk = BuildAtk(playerInfo),
                        Hp = maxHp,
                        MaxHp = maxHp,
                        MoveSpeed = 5,
                    },
                },
                PlayerRunData = new CSLevelUnitRunData
                {
                    Hp = maxHp,
                    Level = (int)playerInfo.Level,
                    Exp = 0,
                    Gold = 0,
                },
            },
        };
    }

    private static int BuildAtk(RoomPlayerInfo playerInfo)
        => Math.Max(10, (int)Math.Max(playerInfo.FightValue / 10u, playerInfo.Level * 5u));

    private static int BuildMaxHp(RoomPlayerInfo playerInfo)
        => Math.Max(100, (int)Math.Max(playerInfo.FightValue, playerInfo.Level * 100u));

    private static RoleBodyType ToRoleBodyType(byte sex)
        => sex == (byte)RoleBodyType.ROLE_BODY_FEMALE ? RoleBodyType.ROLE_BODY_FEMALE : RoleBodyType.ROLE_BODY_MALE;
}
