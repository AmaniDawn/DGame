using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Network;
using GameProto;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

// ReSharper disable InconsistentNaming

namespace Hotfix;

public sealed class PlayerDataDestroySystem : DestroySystem<PlayerData>
{
    protected override void Destroy(PlayerData self)
    {
        self.SessionRuntimeId = 0;
        self.AccountID = 0;
        self.ServerID = 0;
        self.RoleName = string.Empty;
        self.HeadID = 0;
        self.Sex = 0;
        self.Level = 0;
        self.Exp = 0;
        self.FightValue = 0;
        self.Diamond = 0;
        self.Gold = 0;
        self.Stam = 0;
        self.IsFinGuide = 0;
        self.Sign = string.Empty;
        self.WorldID = 0;
        self.TotalRmb = 0;
        self.LastAddStamTime = 0;
        self.DailyBuyStamCount = 0;
        self.CreateTime = 0;
        self.LastLoginTime = 0;
    }
}

public static class PlayerDataSystem
{
    /// <summary>
    /// 记录客户端的Session
    /// </summary>
    /// <param name="self"></param>
    /// <param name="sessionRuntimeId"></param>
    public static void RecordSession(this PlayerData self, long sessionRuntimeId)
    {
        self.SessionRuntimeId = sessionRuntimeId;
    }
    
    /// <summary>
    /// 账号上线逻辑
    /// </summary>
    /// <param name="self"></param>
    public static async FTask Online(this PlayerData self)
    {
        await FTask.CompletedTask;
    }
    
    /// <summary>
    /// 账号下线逻辑
    /// </summary>
    /// <param name="self"></param>
    /// <param name="timeOut">延迟下线时间</param>
    public static async FTask Offline(this PlayerData self, int timeOut = 0)
    {
        var scene = self.Scene;
        var playerManagerComponent = scene.GetComponent<PlayerManagerComponent>();
        if (!playerManagerComponent.TryGet(self.AccountID, self.ServerID, out var playerData))
        {
            // 如果缓存中没有 表示已经下线或根本不存在账号
            Log.Warning($"PlayerDataSystem Offline fail accountID: {self.AccountID} serverID: {self.ServerID} not found");
            return;
        }

        if (!scene.TryGetEntity<Session>(self.SessionRuntimeId, out _))
        {
            Log.Warning($"PlayerDataSystem Offline fail Session: {self.SessionRuntimeId} not found");
            return;
        }

        if (timeOut <= 0)
        {
            // 直接执行下线操作
            await self.InternalOffline();
        }
        // 延迟下线
        playerData.SetDestroyTimeout(timeOut, self.InternalOffline);
        await FTask.CompletedTask;
    }
    
    /// <summary>
    /// 内部下线方法
    /// </summary>
    /// <param name="self"></param>
    private static async FTask InternalOffline(this PlayerData self)
    {
        // 保存当前账号数据到数据库
        await self.Scene.World.Database.Save(self);
        // 在缓存中移除自己 并执行自己的Dispose销毁方法
        self.Scene.GetComponent<PlayerManagerComponent>().Remove(self);
    }

    /// <summary>
    /// PlayerData 转换成 CSPlayerData
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static CSPlayerData ToCSPlayerData(this PlayerData self)
        => new ()
        {
            RoleName = self.RoleName,
            HeadID = self.HeadID,
            RoleID = (ulong)self.Id,
            Sex = self.Sex,
            Level = self.Level,
            Exp = self.Exp,
            FightValue = self.FightValue,
            Diamond = self.Diamond,
            Gold = self.Gold,
            Stam = self.Stam,
            IsFinGuide = self.IsFinGuide,
            Sign = self.Sign,
            WorldID = self.WorldID,
            TotalRmb = self.TotalRmb,
            LastAddStamTime = self.LastAddStamTime,
            DailyBuyStamCount = self.DailyBuyStamCount,
            CreateTime = self.CreateTime,
            LastLoginTime = self.LastLoginTime,
        };

    /// <summary>
    /// 第一次创建角色 初始数据
    /// </summary>
    /// <param name="self"></param>
    public static void Initialize(this PlayerData self)
    {
        var cfg = TbPlayerInitConfig.GetOrDefault(TbFuncParamConfig.PlayerInitConfigID);
        if (cfg == null)
        {
            return;
        }
        self.Sex = cfg.Sex;
        self.Diamond = (uint)cfg.Diamond;
        self.Level = (uint)cfg.Level;
        self.Exp = (uint)cfg.Exp;
        self.FightValue = (uint)cfg.FightValue;
        self.Gold = (uint)cfg.Gold;
        self.Stam = (uint)cfg.Stam;
        self.IsFinGuide = cfg.IsFinGuide;
        self.Sign = cfg.Sign;
        self.HeadID = cfg.HeadID;
        self.CreateTime = TimeHelper.Now;
        self.TotalRmb = 0;
        self.LastAddStamTime = 0;
        self.DailyBuyStamCount = 0;
    }

    /// <summary>
    /// 保存账号数据到数据库
    /// </summary>
    /// <param name="self"></param>
    public static async FTask SaveDatabase(this PlayerData self)
        => await self.Scene.World.Database.Save(self);
    
    /// <summary>
    /// 校验角色名是否合法。
    /// 规则：
    ///   1. 不能为空或纯空白。
    ///   2. 只允许汉字（\u4e00-\u9fa5）、英文字母（a-z / A-Z）、数字（0-9），其余字符一律拒绝。
    ///   3. 根据字符组成分为四种模式，各自套用独立的长度配置：
    ///      - 纯中文    ：汉字数在 [RoleNameChineseMinCount, RoleNameChineseMaxCount] 范围内
    ///      - 纯英文    ：字母数在 [RoleNameLatinMinLength,   RoleNameLatinMaxLength]  范围内
    ///      - 纯数字    ：数字数在 [RoleNameDigitMinLength,   RoleNameDigitMaxLength]  范围内
    ///      - 混排（任意两种及以上组合）
    ///                  ：UTF-8 字节数（汉字 3 字节，字母/数字各 1 字节）
    ///                    在 [RoleNameMixedMinBytes, RoleNameMixedMaxBytes] 范围内
    /// </summary>
    /// <param name="roleName">待校验的角色名</param>
    /// <returns>
    ///   Success              — 校验通过
    ///   Role_NameInvalid     — 名字为空或纯空白
    ///   Role_NameCharInvalid — 含非法字符
    ///   Role_NameLengthOutOfRange — 长度不在对应模式的配置范围内
    /// </returns>
    private static uint ValidateRoleName(string roleName)
    {
        // 1. 空值校验
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return ErrorCode.ROLE_NAME_INVALID;
        }

        // 2. 单次遍历：同时完成字符合法性校验与三类字符计数
        var chineseCount = 0; // 汉字数
        var latinCount   = 0; // 英文字母数
        var digitCount   = 0; // 数字数
        foreach (var c in roleName)
        {
            if (c >= '\u4e00' && c <= '\u9fa5')
            {
                chineseCount++;
            }
            else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                latinCount++;
            }
            else if (c >= '0' && c <= '9')
            {
                digitCount++;
            }
            else
            {
                // 含非法字符，立即返回，无需继续遍历
                return ErrorCode.ROLE_NAME_CHAR_INVALID;
            }
        }

        // 3. 统计活跃字符类型数，>= 2 即为混排
        var typeCount = (chineseCount > 0 ? 1 : 0)
                      + (latinCount   > 0 ? 1 : 0)
                      + (digitCount   > 0 ? 1 : 0);

        bool lengthValid;
        if (typeCount >= 2)
        {
            // 混排（英文+数字 / 中文+英文 / 中文+数字 / 三者皆有）
            // UTF-8 字节数：汉字 3 字节，字母/数字各 1 字节
            var byteLen = chineseCount * 3 + latinCount + digitCount;
            lengthValid = byteLen >= TbFuncParamConfig.RoleNameMixedMinCount
                       && byteLen <= TbFuncParamConfig.RoleNameMixedMaxCount;
        }
        else if (chineseCount > 0)
        {
            // 纯中文
            lengthValid = chineseCount >= TbFuncParamConfig.RoleNameChineseMinCount
                       && chineseCount <= TbFuncParamConfig.RoleNameChineseMaxCount;
        }
        else if (latinCount > 0)
        {
            // 纯英文
            lengthValid = latinCount >= TbFuncParamConfig.RoleNameLatinMinCount
                       && latinCount <= TbFuncParamConfig.RoleNameLatinMaxCount;
        }
        else
        {
            // 纯数字
            lengthValid = digitCount >= TbFuncParamConfig.RoleNameDigitMinCount
                       && digitCount <= TbFuncParamConfig.RoleNameDigitMaxCount;
        }

        return lengthValid ? ErrorCode.SUCCESS : ErrorCode.ROLE_NAME_LENGTH_OUT_RANGE;
    }
}
