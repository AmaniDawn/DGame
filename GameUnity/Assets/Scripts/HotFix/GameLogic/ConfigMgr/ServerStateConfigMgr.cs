using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
    public class ServerStateConfigMgr : Singleton<ServerStateConfigMgr>
    {
        public List<ServerStateConfig> DataList => TbServerStateConfig.DataList;

        public ServerStateConfig GetOrDefault(int state) => TbServerStateConfig.GetOrDefault(state);

        public bool TryGetValue(int state, out ServerStateConfig cfg) => TbServerStateConfig.TryGetValue(state, out cfg);

        public bool ContainsKey(int state) => TbServerStateConfig.ContainsKey(state);
    }
}