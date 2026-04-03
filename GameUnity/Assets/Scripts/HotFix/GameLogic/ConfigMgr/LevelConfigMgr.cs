using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
    public class LevelConfigMgr : Singleton<LevelConfigMgr>
    {
        #region 地图配置表

        public MapConfig GetMapConfigOrDefault(int mapID) => TbMapConfig.GetOrDefault(mapID);

        public bool TryGetMapConfig(int mapID, out MapConfig cfg) => TbMapConfig.TryGetValue(mapID, out cfg);

        public bool ContainsMapId(int mapID) => TbMapConfig.ContainsKey(mapID);

        #endregion

        #region 路径配置表

        public PathConfig GetPathConfigOrDefault(int pathID) => TbPathConfig.GetOrDefault(pathID);

        public bool TryGetPathConfig(int pathID, out PathConfig cfg) => TbPathConfig.TryGetValue(pathID, out cfg);

        public bool ContainsPathId(int pathID) => TbPathConfig.ContainsKey(pathID);

        public List<PathConfig> GetPathCfgListByGroupID(int groupID) => TbPathConfig.GetListByGroupID(groupID);

        #endregion
    }
}