using System;
using System.Runtime.InteropServices;

namespace DGame
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct PoolObjectInfo
    {
        /// <summary>
        /// 初始化对象信息的新实例
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="locked">对象是否被加锁</param>
        /// <param name="customCanReleaseFlag">对象自定义释放检查标记</param>
        /// <param name="priority">对象的优先级</param>
        /// <param name="lastUseTime">对象是否正在使用</param>
        /// <param name="spawnCount">对象的正在被使用计数</param>
        public PoolObjectInfo(string name, bool locked, bool customCanReleaseFlag, int priority, DateTime lastUseTime, int spawnCount)
        {
            Name = name;
            Locked = locked;
            CustomCanReleaseFlag = customCanReleaseFlag;
            Priority = priority;
            LastUseTime = lastUseTime;
            SpawnCount = spawnCount;
        }

        /// <summary>
        /// 对象名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 对象是否被加锁
        /// </summary>
        public bool Locked { get; }

        /// <summary>
        /// 自定义释放检查标记
        /// </summary>
        public bool CustomCanReleaseFlag { get; }

        /// <summary>
        /// 获取对象的优先级
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 对象最后一次使用时间
        /// </summary>
        public DateTime LastUseTime { get; }

        /// <summary>
        /// 对象是否正在使用。
        /// </summary>
        public bool IsUsing => SpawnCount > 0;

        /// <summary>
        /// 对象的正在被使用计数。
        /// </summary>
        public int SpawnCount  { get; }
    }
}