namespace DGame
{
    /// <summary>
    /// 内存池对象抽象基类
    /// </summary>
    public abstract class MemoryObject : IMemory
    {
        /// <summary>
        /// 清理内存 返回内存池
        /// </summary>
        public abstract void OnRelease();

        public static void Release(MemoryObject memoryObject)
        {
            MemoryPool.Release(memoryObject);
        }

        public static T Spawn<T>() where T : MemoryObject, new()
        {
            return MemoryPool.Spawn<T>();
        }
    }
}