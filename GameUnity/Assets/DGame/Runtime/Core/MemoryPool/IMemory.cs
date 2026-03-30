namespace DGame
{
    public interface IMemory
    {
        /// <summary>
        /// 释放内存 返回内存池
        /// </summary>
        void OnRelease();
    }
}