using System.Collections.Generic;

namespace DGame
{
    public static class StringId
    {
        private static Dictionary<string, int> m_eventIdMaps = new Dictionary<string, int>();
        private static Dictionary<int, string> m_eventStringMaps = new Dictionary<int, string>();

        private static int m_currentEventId = 0;

        /// <summary>
        /// 字符串转HashID
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int StringToHash(string str)
        {
            if (m_eventIdMaps.TryGetValue(str, out var hash))
            {
                return hash;
            }

            hash = ++m_currentEventId;
            m_eventIdMaps.Add(str, hash);
            m_eventStringMaps.Add(hash, str);
            return hash;
        }

        /// <summary>
        /// HashID转字符串
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static string HashToString(int hash)
        {
            return m_eventStringMaps.TryGetValue(hash, out var str) ? str : string.Empty;
        }
    }
}