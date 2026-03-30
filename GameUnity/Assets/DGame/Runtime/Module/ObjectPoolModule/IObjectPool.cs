using System;
using System.Collections.Generic;

namespace DGame
{
    /// <summary>
    /// 释放对象筛选函数
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="candidateObjects">要筛选的对象集合</param>
    /// <param name="toReleaseCount">需要释放的对象数量</param>
    /// <param name="expireTime">对象过期参考时间</param>
    /// <returns>经筛选需要释放的对象集合</returns>
    public delegate List<T> ReleaseObjectFilterCallback<T>(List<T> candidateObjects, int toReleaseCount, DateTime expireTime) where T : BasePoolObject;

    /// <summary>
    /// 对象池接口
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IObjectPool<T> where T : BasePoolObject
    {
        /// <summary>
        /// 对象池名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 对象池完整名称
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// 对象池对象类型
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// 对象池对象的数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 是否允许对象被多次获取
        /// <remarks>true=>可以同时获取多个对象 false=>同一时间只能有一个对象被获取</remarks>
        /// </summary>
        bool AllowMultiSpawn { get; }

        /// <summary>
        /// 自动释放可释放对象的间隔（秒）
        /// </summary>
        float AutoReleaseInterval { get; set; }

        /// <summary>
        /// 对象池容量
        /// </summary>
        int Capacity { get; set; }

        /// <summary>
        /// 对象池对象过期回收时间（秒）
        /// </summary>
        float ExpireTime { get; set; }

        /// <summary>
        /// 对象池优先级
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// 注册对象（对象外部创建，标记到对象池管理）
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="spawned">对象是否已Spawn</param>
        void Register(T obj, bool spawned);

        /// <summary>
        /// 检查对象是否存在
        /// </summary>
        /// <returns>检查对象是否存在</returns>
        bool CanSpawn();

        /// <summary>
        /// 检查对象是否存在
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <returns>检查对象是否存在</returns>
        bool CanSpawn(string name);

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns></returns>
        T Spawn();

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <returns></returns>
        T Spawn(string name);

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="obj">被回收的对象</param>
        void Recycle(T obj);

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="obj">被回收的对象</param>
        void Recycle(object obj);

        /// <summary>
        /// 设置对象加锁
        /// </summary>
        /// <param name="obj">需要加锁的对象</param>
        /// <param name="locked">锁定状态</param>
        void SetLocked(T obj, bool locked);

        /// <summary>
        /// 设置对象加锁
        /// </summary>
        /// <param name="obj">需要加锁的对象</param>
        /// <param name="locked">锁定状态</param>
        void SetLocked(object obj, bool locked);

        /// <summary>
        /// 设置对象优先级
        /// </summary>
        /// <param name="obj">需要设置的对象</param>
        /// <param name="priority">对象优先级</param>
        void SetPriority(T obj, int priority);

        /// <summary>
        /// 设置对象优先级
        /// </summary>
        /// <param name="obj">需要设置的对象</param>
        /// <param name="priority">对象优先级</param>
        void SetPriority(object obj, int priority);

        /// <summary>
        /// 回收对象到内存池（从对象池中释放）
        /// </summary>
        /// <param name="obj">需要释放的对象</param>
        bool ReleaseObject(T obj);

        /// <summary>
        /// 回收对象到内存池 （从对象池中释放）
        /// </summary>
        /// <param name="obj">需要释放的对象</param>
        bool ReleaseObject(object obj);

        /// <summary>
        /// 释放对象池中可以回收到内存池的对象（从对象池中释放）
        /// </summary>
        void Release();

        /// <summary>
        /// 释放对象池中可以回收到内存池的对象（从对象池中释放）
        /// </summary>
        /// <param name="releaseCnt">尝试释放的数量</param>
        void Release(int releaseCnt);

        /// <summary>
        /// 释放对象池中可以回收到内存池的对象（从对象池中释放）
        /// </summary>
        /// <param name="releaseObjectFilterCallback">释放对象筛选函数</param>
        void Release(ReleaseObjectFilterCallback<T> releaseObjectFilterCallback);

        /// <summary>
        /// 释放对象池中可以回收到内存池的对象（从对象池中释放）
        /// </summary>
        /// <param name="releaseCnt">尝试释放对象数量</param>
        /// <param name="releaseObjectFilterCallback">释放对象筛选函数</param>
        void Release(int releaseCnt, ReleaseObjectFilterCallback<T> releaseObjectFilterCallback);

        /// <summary>
        /// 释放对象池中未使用的对象到内存池（从对象池中释放）
        /// </summary>
        void ReleaseAllUnused();
    }
}