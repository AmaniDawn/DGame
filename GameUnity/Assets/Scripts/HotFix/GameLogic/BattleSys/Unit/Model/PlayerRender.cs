using System.Threading;
using Cysharp.Threading.Tasks;
using GameBattle;
using GameProto;

namespace GameLogic
{
    /// <summary>
    /// 玩家渲染单位。
    /// </summary>
    public sealed class PlayerRender : RenderUnit
    {
        /// <summary>
        /// 主显示初始化完成后刷新武器模型。
        /// </summary>
        /// <param name="ct">初始化取消令牌。</param>
        protected override async UniTask OnDisplayReadyAsync(CancellationToken ct)
        {
            await UnitDisplay.RefreshWeaponModelAsync(GetWeaponModelID(), ct);
        }

        /// <summary>
        /// 获取玩家当前使用的模型配置 ID。
        /// </summary>
        /// <returns>模型配置 ID；当前默认返回 0。</returns>
        public override int GetModelID()
        {
            if (LogicUnit != null && LogicUnit.CreateData != null && LogicUnit.CreateData.TryGetPayload<PlayerUnitCreatePayload>(out var playerPayload))
            {
                RoleBodyType bodyType = (RoleBodyType)playerPayload.RoleBodyType;
                uint fashionId = playerPayload.ClothingConfigId;

                if (fashionId == 0)
                {
                    int defaultModelId = 0;
                    switch (bodyType)
                    {
                        case RoleBodyType.ROLE_BODY_NONE:
                        case RoleBodyType.ROLE_BODY_MALE:
                        default:
                            defaultModelId = TbFuncParamConfig.DefaultMaleModelID;
                            break;
                        case RoleBodyType.ROLE_BODY_FEMALE:
                            defaultModelId = TbFuncParamConfig.DefaultFemaleModelID;
                            break;
                    }

                    return defaultModelId;
                }
                var cfg = ModelConfigMgr.Instance.GetPlayerModelPairCfgOrDefault(bodyType, (int)fashionId);
                return cfg?.ModelID ?? 0;
            }

            return 0;
        }

        private int GetWeaponModelID()
        {
            if (LogicUnit == null || LogicUnit.CreateData == null || LogicUnit.CreateData.TryGetPayload<PlayerUnitCreatePayload>(out var playerPayload) == false)
            {
                return 0;
            }
            var createParam = playerPayload;

            RoleBodyType bodyType = (RoleBodyType)createParam.RoleBodyType;

            if (bodyType == RoleBodyType.ROLE_BODY_NONE)
            {
                bodyType = RoleBodyType.ROLE_BODY_MALE;
            }

            uint weaponFashionID = createParam.WeaponConfigId;
            if (weaponFashionID == 0)
            {
                weaponFashionID = (uint)TbFuncParamConfig.DefaultGunFashionID;
            }

            var weaponModelID = ModelConfigMgr.Instance.GetWeaponModelCfgOrDefault((int)weaponFashionID)?.ModelID ?? 0;

            return weaponModelID;
        }
    }
}