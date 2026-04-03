using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
    public class LevelConfigMgr : Singleton<LevelConfigMgr>
    {
        #region 地图配置表

        public MapConfig GetModelOrDefault(int modelID) => TbMapConfig.GetOrDefault(modelID);

        public bool TryGetModelCfg(int modelID, out MapConfig cfg) => TbMapConfig.TryGetValue(modelID, out cfg);

        public bool ContainsModelId(int modelID) => TbMapConfig.ContainsKey(modelID);

        #endregion

        #region 路径配置表

        public PathConfig GetPathModelOrDefault(int modelID) => TbPathConfig.GetOrDefault(modelID);

        public bool TryGetPathModelCfg(int modelID, out PathConfig cfg) => TbPathConfig.TryGetValue(modelID, out cfg);

        public bool ContainsPathModelId(int modelID) => TbPathConfig.ContainsKey(modelID);

        public List<PathConfig> GetListByGroupID(int groupID) => TbPathConfig.GetListByGroupID(groupID);

        #endregion
    }
}