using System;
using System.Collections.Generic;
using DGame;
using Fantasy;
using GameProto;

namespace GameLogic
{
    public class PlayerData : BasePlayerData
    {
        /// <summary>
        /// RoleID
        /// </summary>
        public ulong RoleID { get => m_roleID; private set => m_roleID = value; }

        /// <summary>
        /// 性别
        /// </summary>
        public RoleBodyType BodyType { get => m_bodyType; private set => m_bodyType = value; }

        /// <summary>
        /// RoleNo
        /// </summary>
        public ulong RoleNo { get; private set; }

        /// <summary>
        /// 所在主服
        /// </summary>
        public int WorldID { get; private set; }

        /// <summary>
        /// 创角时间
        /// </summary>
        public long CreateTime { get; private set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string RoleName { get; private set; }

        /// <summary>
        /// 等级
        /// </summary>
        public uint Level { get; private set; }

        /// <summary>
        /// 旧等级
        /// </summary>
        public uint OldLevel { get; private set; }

        /// <summary>
        /// 经验
        /// </summary>
        public uint Exp { get; private set; }

        /// <summary>
        /// 角色所有的货币和数量
        /// </summary>
        private Dictionary<int, uint> m_allCurrencyDict = new Dictionary<int, uint>();

        /// <summary>
        /// 角色体力
        /// </summary>
        public uint Stam { get; private set; }

        /// <summary>
        /// 上次登录时间
        /// </summary>
        public long LastLoginTime { get; private set; }

        /// <summary>
        /// 上次增加体力的时间
        /// </summary>
        public long LastAddStamTime { get; private set; }

        /// <summary>
        /// 每日购买体力次数
        /// </summary>
        public int DailyBuyStamCount { get; private set; }

        /// <summary>
        /// 角色战斗力
        /// </summary>
        public uint FightValue { get; private set; }

        /// <summary>
        /// 角色个性签名
        /// </summary>
        public string Sign { get; private set; }

        /// <summary>
        /// 是否完成新手引导
        /// </summary>
        public bool IsFinGuide { get; private set; }

        /// <summary>
        /// 累计充值金额
        /// </summary>
        public uint TotalRmb { get; private set; }

        /// <summary>
        /// 是否登录成功，完成数据初始化
        /// </summary>
        public bool IsInit { get; private set; }

        #region 服务器相关

        public void UpdatePlayerData(CSPlayerData playerData, bool isDirty)
        {
            RoleID = playerData.RoleID;
            RoleNo = playerData.RoleNo;
            WorldID = playerData.WorldID;
            TotalRmb = playerData.TotalRmb;
            CreateTime = playerData.CreateTime;
            LastLoginTime = playerData.LastLoginTime;
            IsFinGuide = playerData.IsFinGuide > 0;

            if(string.IsNullOrEmpty(RoleName) || !RoleName.Equals(playerData.RoleName))
            {
                RoleName = playerData.RoleName;
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerNameChange();
            }

            if(Level != playerData.Level)
            {
                OldLevel = isDirty ? Level : playerData.Level;
                Level = playerData.Level;
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerLevelChange();
            }

            if(FightValue != playerData.FightValue)
            {
                var oldValue = FightValue == 0 ? playerData.FightValue : FightValue;
                FightValue = playerData.FightValue;

                if (isDirty || oldValue != FightValue)
                {
                    if (UIModule.Instance.GetWindow<GameMainUI>() != null)
                    {
                        GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerFightValueChange(oldValue, FightValue);
                    }
                }
            }

            if(!string.IsNullOrWhiteSpace(playerData.Sign) && !Sign.Equals(playerData.Sign))
            {
                Sign = playerData.Sign;
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerSignDataChange();
            }

            if(BodyType != (RoleBodyType)playerData.Sex)
            {
                BodyType = (RoleBodyType)playerData.Sex;
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerSexDataChange();
            }

            var oldDiamond = GetCurrency(CurrencyType.CURRENCY_DIAMOND);
            if(oldDiamond != playerData.Diamond)
            {
                var newValue = playerData.Diamond;
                UpdateCurrencyData((int)CurrencyType.CURRENCY_DIAMOND, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerDiamondChange(oldDiamond, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerCurrencyChange(CurrencyType.CURRENCY_DIAMOND, oldDiamond, newValue);
            }

            var oldGold = GetCurrency(CurrencyType.CURRENCY_GOLD);
            if(oldGold != playerData.Gold)
            {
                var newValue = playerData.Gold;
                UpdateCurrencyData((int)CurrencyType.CURRENCY_GOLD, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerGoldChange(oldDiamond, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerCurrencyChange(CurrencyType.CURRENCY_GOLD, oldDiamond, newValue);
            }

            var oldStam = GetCurrency(CurrencyType.CURRENCY_STAM);
            if(oldStam != playerData.Stam)
            {
                var newValue = playerData.Stam;
                UpdateCurrencyData((int)CurrencyType.CURRENCY_STAM, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerStamChange(oldDiamond, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerCurrencyChange(CurrencyType.CURRENCY_STAM, oldDiamond, newValue);
            }

            var oldExp = GetCurrency(CurrencyType.CURRENCY_EXP);
            if(oldExp != playerData.Exp)
            {
                var newValue = playerData.Exp;
                UpdateCurrencyData((int)CurrencyType.CURRENCY_EXP, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerExpChange(oldDiamond, newValue);
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerCurrencyChange(CurrencyType.CURRENCY_EXP, oldDiamond, newValue);
            }

            if (LastAddStamTime != playerData.LastAddStamTime)
            {
                LastAddStamTime = playerData.LastAddStamTime;
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerLastAddStamTimeChange();
            }

            if (DailyBuyStamCount != playerData.DailyBuyStamCount)
            {
                DailyBuyStamCount = playerData.DailyBuyStamCount;
                GameEvent.Get<IPlayerLogicEvent>().OnMainPlayerBuyStamCntChange();
            }

            IsInit = true;
        }

        private void UpdateCurrencyData(int currencyType, uint value)
        {
            if (currencyType is > 0 and < (int)CurrencyType.CURRENCY_MAX)
            {
                m_allCurrencyDict[currencyType] = value;
            }
        }

        #endregion

        #region 货币

        public bool CheckCurrencyEnough(CurrencyType currencyType, int needCnt, bool showTips = false)
        {
            uint currencyCnt = GetCurrency(currencyType);

            if (currencyCnt < needCnt)
            {
                switch (currencyType)
                {
                    case CurrencyType.CURRENCY_NONE:
                        break;

                    case CurrencyType.CURRENCY_GOLD:
                        break;

                    case CurrencyType.CURRENCY_DIAMOND:
                        break;

                    case CurrencyType.CURRENCY_STAM:
                        break;

                    case CurrencyType.CURRENCY_EXP:
                        break;

                    default:
                        ShowNotEnoughCurrencyTips(currencyType);
                        break;
                }
            }
            return true;
        }

        private void ShowNotEnoughCurrencyTips(CurrencyType currencyType)
        {
            if (CurrencyConfigMgr.Instance.TryGetValue(currencyType, out var currencyCfg))
            {
                UIModule.Instance.ShowTipsUI(TextConfigMgr.Instance.GetText(currencyCfg.NotEnoughTextID));
            }
        }

        public uint GetCurrency(int currencyType)
            => m_allCurrencyDict.TryGetValue(currencyType, out var count) ? count : 0;

        public uint GetCurrency(CurrencyType currencyType) => GetCurrency((int)currencyType);

        public string GetCurrencyStr(int currencyType)
        {
            return TextConfigMgr.Instance.FormatNum(GetCurrency(currencyType));
        }

        public string GetCurrencyStr(CurrencyType currencyType)
        {
            return TextConfigMgr.Instance.FormatNum(GetCurrency(currencyType));
        }

        #endregion
    }
}