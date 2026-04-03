using GameProto;

namespace GameLogic
{
    public class ModelConfigMgr : Singleton<ModelConfigMgr>
    {
        #region 模型配置表

        public ModelConfig GetModelOrDefault(int modelID) => TbModelConfig.GetOrDefault(modelID);

        public bool TryGetModelCfg(int modelID, out ModelConfig cfg) => TbModelConfig.TryGetValue(modelID, out cfg);

        public bool ContainsModelId(int modelID) => TbModelConfig.ContainsKey(modelID);

        #endregion

        #region 武器模型配置表

        public WeaponModelConfig GetWeaponModelCfgOrDefault(int modelID) => TbWeaponModelConfig.GetOrDefault(modelID);

        public bool TryGetWeaponModelCfg(int modelID, out WeaponModelConfig cfg) => TbWeaponModelConfig.TryGetValue(modelID, out cfg);

        public bool ContainsWeaponModelId(int modelID) => TbWeaponModelConfig.ContainsKey(modelID);

        #endregion

        #region 角色模型配对表

        public PlayerModelPairConfig GetPlayerModelPairCfgOrDefault(RoleBodyType bodyType, int clothID)
            => TbPlayerModelPairConfig.Get((int)bodyType, clothID);

        #endregion
    }
}