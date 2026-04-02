using GameProto;

namespace GameBattle
{
    /// <summary>
    /// 战斗域模型配置总入口。
    /// </summary>
    public class ModelConfigMgr : BattleSingleton<ModelConfigMgr>
    {
        /// <summary>
        /// 获取模型配置。
        /// </summary>
        public ModelConfig GetModelOrDefault(int modelID) => TbModelConfig.GetOrDefault(modelID);

        /// <summary>
        /// 尝试获取模型配置。
        /// </summary>
        public bool TryGetModelCfg(int modelID, out ModelConfig cfg) => TbModelConfig.TryGetValue(modelID, out cfg);

        /// <summary>
        /// 检查模型配置是否存在。
        /// </summary>
        public bool ContainsModelId(int modelID) => TbModelConfig.ContainsKey(modelID);

        /// <summary>
        /// 获取武器模型配置。
        /// </summary>
        public WeaponModelConfig GetWeaponModelCfgOrDefault(int modelID) => TbWeaponModelConfig.GetOrDefault(modelID);

        /// <summary>
        /// 尝试获取武器模型配置。
        /// </summary>
        public bool TryGetWeaponModelCfg(int modelID, out WeaponModelConfig cfg) => TbWeaponModelConfig.TryGetValue(modelID, out cfg);

        /// <summary>
        /// 检查武器模型配置是否存在。
        /// </summary>
        public bool ContainsWeaponModelId(int modelID) => TbWeaponModelConfig.ContainsKey(modelID);
    }
}