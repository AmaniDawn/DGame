using GameProto;

namespace GameLogic
{
    public class ModelConfigMgr : Singleton<ModelConfigMgr>
    {
        public ModelConfig GetModelOrDefault(int modelID) => TbModelConfig.GetOrDefault(modelID);

        public bool TryGetModelCfg(int modelID, out ModelConfig cfg) => TbModelConfig.TryGetValue(modelID, out cfg);

        public bool ContainsModelId(int modelID) => TbModelConfig.ContainsKey(modelID);
        
        public WeaponModelConfig GetWeaponModelCfgOrDefault(int modelID) => TbWeaponModelConfig.GetOrDefault(modelID);

        public bool TryGetWeaponModelCfg(int modelID, out WeaponModelConfig cfg) => TbWeaponModelConfig.TryGetValue(modelID, out cfg);

        public bool ContainsWeaponModelId(int modelID) => TbWeaponModelConfig.ContainsKey(modelID);
    }
}