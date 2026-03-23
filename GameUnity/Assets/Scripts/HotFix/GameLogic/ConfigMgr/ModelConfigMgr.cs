using GameProto;

namespace GameLogic
{
    public class ModelConfigMgr : Singleton<ModelConfigMgr>
    {
        public ModelConfig GetOrDefault(int modelID) => TbModelConfig.GetOrDefault(modelID);

        public bool TryGetValue(int modelID, out ModelConfig cfg) => TbModelConfig.TryGetValue(modelID, out cfg);

        public bool ContainsKey(int modelID) => TbModelConfig.ContainsKey(modelID);
    }
}