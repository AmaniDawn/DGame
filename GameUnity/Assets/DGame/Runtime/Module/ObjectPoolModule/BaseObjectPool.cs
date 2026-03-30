using System;

namespace DGame
{
    /// <summary>
    /// 对象池抽象基类
    /// </summary>
    public abstract class BaseObjectPool
    {
        /// <summary>
        /// 对象池名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 对象池完整名称((对象类型.FullName).对象池名称)
        /// </summary>
        public string FullName => new TypeNamePair(ObjectType, Name).ToString();

        /// <summary>
        /// 对象池对象类型
        /// </summary>
        public abstract Type ObjectType { get; }

        /// <summary>
        /// 对象池对象的数量
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// 可以释放的对象数量 （回收到内存池）
        /// </summary>
        public abstract int CanReleaseCount { get; }

        /// <summary>
        /// 是否允许对象被多次获取。
        /// <remarks>true=>可以同时获取多个对象 false=>同一时间只能有一个对象被获取</remarks>
        /// </summary>
        public abstract bool AllowMultiSpawn { get; }

        /// <summary>
        /// 自动释放可释放对象的间隔（秒）
        /// </summary>
        public abstract float AutoReleaseInterval { get; set; }

        /// <summary>
        /// 对象池容量
        /// </summary>
        public abstract int Capacity { get; set; }

        /// <summary>
        /// 对象池对象过期回收时间（秒）
        /// </summary>
        public abstract float ExpireTime { get; set; }

        /// <summary>
        /// 对象池优先级
        /// </summary>
        public abstract int Priority { get; set; }

        protected BaseObjectPool() : this(null) { }

        /// <summary>
        /// 初始化对象池
        /// </summary>
        /// <param name="name">对象池名称</param>
        protected BaseObjectPool(string name)
        {
            Name = string.IsNullOrEmpty(name) ? string.Empty : name;
        }

        /// <summary>
        /// 释放对象池中可以回收到内存池的对象（从对象池中释放）
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// 释放对象池中可以回收到内存池的对象（从对象池中释放）
        /// </summary>
        /// <param name="releaseCnt">尝试释放的数量</param>
        public abstract void Release(int releaseCnt);

        /// <summary>
        /// 释放对象池中未使用的对象到内存池（从对象池中释放）
        /// </summary>
        public abstract void ReleaseAllUnused();

        /// <summary>
        /// 对象池更新函数
        /// </summary>
        /// <param name="elapsedSeconds">逻辑流逝时间（秒）</param>
        /// <param name="realElapseSeconds">真实流逝时间（秒）</param>
        internal abstract void Update(float elapsedSeconds, float realElapseSeconds);

        /// <summary>
        /// 获取所有对象池对象信息
        /// </summary>
        /// <returns></returns>
        public abstract PoolObjectInfo[] GetAllPoolObjectInfos();

        /// <summary>
        /// 对象池销毁
        /// </summary>
        internal abstract void Destroy();
    }
}