using System.Collections.Generic;
using Fantasy.Helper;

namespace GameLogic
{
    public static class AuthenticationHelper
    {
        private static readonly List<string> AuthenticationList = new List<string>()
        {
            "127.0.0.1:20001", "127.0.0.1:20002", "127.0.0.1:20003"
        };

        /// <summary>
        /// 一致性哈希算法 MurmurHash3 获得分配的服务器地址
        /// <remarks>在客户端实现 减轻服务器压力</remarks>
        /// </summary>
        /// <param name="userName">通过用户名进行MurmurHash3</param>
        /// <returns></returns>
        public static string Select(string userName)
        {
            var userNameHashCode = HashCodeHelper.MurmurHash3(userName);
            // 按这个情况 模出的值只会是 0 - 3
            var authenticationListIndex = userNameHashCode % AuthenticationList.Count;
            return AuthenticationList[(int)authenticationListIndex];
        }
    }
}