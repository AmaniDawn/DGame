using GameProto;

namespace GameLogic
{
    public class FuncOpenConfigMgr : Singleton<FuncOpenConfigMgr>
    {
        public FuncOpenConfig GetOrDefault(FuncType funcType) => TbFuncOpenConfig.GetOrDefault((int)funcType);

        public bool TryGetValue(FuncType funcType, out FuncOpenConfig cfg)
            => TbFuncOpenConfig.TryGetValue((int)funcType, out cfg);

        public bool ContainsKey(FuncType funcType) => TbFuncOpenConfig.ContainsKey((int)funcType);

        public FuncOpenConfig GetOrDefault(int funcType) => TbFuncOpenConfig.GetOrDefault(funcType);

        public bool TryGetValue(int funcType, out FuncOpenConfig cfg) => TbFuncOpenConfig.TryGetValue(funcType, out cfg);

        public bool ContainsKey(int funcType) => TbFuncOpenConfig.ContainsKey(funcType);
    }
}