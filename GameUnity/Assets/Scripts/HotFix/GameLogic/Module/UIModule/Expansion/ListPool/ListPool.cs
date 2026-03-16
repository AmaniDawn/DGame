using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 列表对象池
    /// </summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    internal static class ListPool<T>
    {
        private static readonly Pool<List<T>> m_listPool
            = new Pool<List<T>>(null, Clear);

        /// <summary>
        /// 从池中获取一个列表
        /// </summary>
        /// <returns>列表实例</returns>
        public static List<T> Get()
            => m_listPool.Get();

        /// <summary>
        /// 回收列表到池中
        /// </summary>
        /// <param name="element">要回收的列表</param>
        public static void Recycle(List<T> element)
            => m_listPool.Recycle(element);

        private static void Clear(List<T> list)
        {
            list?.Clear();
        }
    }
}