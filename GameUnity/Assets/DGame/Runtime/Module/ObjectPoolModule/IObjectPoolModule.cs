using System;
using System.Collections.Generic;

namespace DGame
{
    /// <summary>
    /// 对象池管理模块接口
    /// </summary>
    public interface IObjectPoolModule
    {
        /// <summary>
        /// 对象池数量
        /// </summary>
        int Count { get; }

        #region Contains

        /// <summary>
        /// 检查是否存在对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns></returns>
        bool Contains<T>() where T : BasePoolObject;

        /// <summary>
        /// 检查是否存在对象池
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <returns></returns>
        bool Contains(Type type);

        /// <summary>
        /// 检查是否存在对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象名称</param>
        /// <returns></returns>
        bool Contains<T>(string name) where T : BasePoolObject;

        /// <summary>
        /// 检查是否存在对象池
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="name">对象名称</param>
        /// <returns></returns>
        bool Contains(Type type, string name);

        /// <summary>
        /// 检查是否存在对象池
        /// </summary>
        /// <param name="condition">检查条件</param>
        /// <returns></returns>
        bool Contains(Predicate<BaseObjectPool> condition);

        #endregion

        #region GetObjectPool

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns></returns>
        IObjectPool<T> GetObjectPool<T>() where T : BasePoolObject;

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <returns></returns>
        BaseObjectPool GetObjectPool(Type objType);

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象名称</param>
        /// <returns></returns>
        IObjectPool<T> GetObjectPool<T>(string name) where T : BasePoolObject;

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象名称</param>
        /// <returns></returns>
        BaseObjectPool GetObjectPool(Type objType, string name);

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <param name="condition">要检查的条件</param>
        /// <returns></returns>
        BaseObjectPool GetObjectPool(Predicate<BaseObjectPool> condition);

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <param name="condition">要检查的条件</param>
        /// <returns></returns>
        BaseObjectPool[] GetObjectPools(Predicate<BaseObjectPool> condition);

        /// <summary>
        /// 获取所有的对象池
        /// </summary>
        /// <returns></returns>
        BaseObjectPool[] GetAllObjectPools();

        /// <summary>
        /// 获取所有的对象池
        /// </summary>
        /// <param name="pools">所有的对象池</param>
        /// <returns></returns>
        void GetAllObjectPools(List<BaseObjectPool> pools);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序</param>
        /// <returns>所有对象池</returns>
        BaseObjectPool[] GetAllObjectPools(bool sort);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="pools">所有的对象池</param>
        /// <param name="sort">是否根据对象池的优先级排序</param>
        /// <returns>所有对象池</returns>
        void GetAllObjectPools(bool sort, List<BaseObjectPool> pools);

        #endregion

        #region CreateSingleSpawnObjectPool

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>() where T : BasePoolObject;

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name) where T : BasePoolObject;

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity) where T : BasePoolObject;

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(float expireTime) where T : BasePoolObject;

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, float expireTime);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float expireTime) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, float expireTime);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, float expireTime) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity, float expireTime);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity, int priority);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, float expireTime, int priority);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, float expireTime) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity, float expireTime);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity, int priority);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, float expireTime, int priority);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity, float expireTime, int priority);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity, float expireTime, int priority);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float autoReleaseInterval, int capacity, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, float autoReleaseInterval, int capacity, float expireTime, int priority);

        #endregion

        #region CreateMultiSpawnObjectPool

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>() where T : BasePoolObject;

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name) where T : BasePoolObject;

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity) where T : BasePoolObject;

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(float expireTime) where T : BasePoolObject;

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, float expireTime);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float expireTime) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, float expireTime);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, float expireTime) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity, float expireTime);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity, int priority);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, float expireTime, int priority);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, float expireTime) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity, float expireTime);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池的容量</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity, int priority);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, float expireTime, int priority);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity, float expireTime, int priority);

        /// <summary>
        /// 创建允许单次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity, float expireTime, int priority);

        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float autoReleaseInterval, int capacity, float expireTime, int priority) where T : BasePoolObject;


        /// <summary>
        /// 创建允许多次获取的对象池
        /// </summary>
        /// <param name="objType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数</param>
        /// <param name="capacity">对象池容量</param>
        /// <param name="expireTime">对象池对象过期秒数</param>
        /// <param name="priority">对象池的优先级</param>
        /// <returns></returns>
        BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, float autoReleaseInterval, int capacity, float expireTime, int priority);

        #endregion

        #region DestroyObjectPool

        /// <summary>
        /// 销毁对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>是否销毁对象池成功</returns>
        bool DestroyObjectPool<T>() where T : BasePoolObject;

        /// <summary>
        /// 销毁对象池
        /// </summary>
        /// <param name="objectType">对象类型</param>
        /// <returns>是否销毁对象池成功</returns>
        bool DestroyObjectPool(Type objectType);

        /// <summary>
        /// 销毁对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象池名称</param>
        /// <returns>是否销毁对象池成功</returns>
        bool DestroyObjectPool<T>(string name) where T : BasePoolObject;

        /// <summary>
        /// 销毁对象池
        /// </summary>
        /// <param name="objectType">对象类型</param>
        /// <param name="name">对象池名称</param>
        /// <returns>是否销毁对象池成功</returns>
        bool DestroyObjectPool(Type objectType, string name);

        /// <summary>
        /// 销毁对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="objectPools">要销毁的对象池</param>
        /// <returns>是否销毁对象池成功</returns>
        bool DestroyObjectPool<T>(IObjectPool<T> objectPools) where T : BasePoolObject;

        /// <summary>
        /// 销毁对象池
        /// </summary>
        /// <param name="objectPools">对象池名称</param>
        /// <returns>是否销毁对象池成功</returns>
        bool DestroyObjectPool(BaseObjectPool objectPools);

        #endregion

        #region Release

        void Release();

        void ReleaseAllUnused();

        #endregion
    }
}