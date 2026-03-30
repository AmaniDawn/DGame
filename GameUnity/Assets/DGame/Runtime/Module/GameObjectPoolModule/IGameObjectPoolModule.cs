using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    public interface IGameObjectPoolModule
    {
        GameObject PoolRoot { get; }

        /// <summary>
        /// 异步预创建GameObjectPool
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="initCapacity">初始容量</param>
        /// <param name="maxCapacity">最大容量</param>
        /// <param name="autoDestroyTime">自动销毁时间</param>
        /// <param name="dontDestroy">持久化</param>
        /// <param name="allowMultiSpawn">是否允许重复获取</param>
        /// <returns></returns>
        UniTask<GameObjectPool> CreateGameObjectPoolAsync(string location, int initCapacity = 0,
            int maxCapacity = int.MaxValue, float autoDestroyTime = -1f, bool dontDestroy = false,
            bool allowMultiSpawn = false);

        /// <summary>
        /// 预创建GameObjectPool
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="initCapacity">初始容量</param>
        /// <param name="maxCapacity">最大容量</param>
        /// <param name="autoDestroyTime">自动销毁时间</param>
        /// <param name="dontDestroy">持久化</param>
        /// <param name="allowMultiSpawn">是否允许重复获取</param>
        /// <returns></returns>
        GameObjectPool CreateGameObjectPool(string location, int initCapacity = 0,
            int maxCapacity = int.MaxValue, float autoDestroyTime = -1f, bool dontDestroy = false,
            bool allowMultiSpawn = false);

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        UniTask<GameObject> SpawnAsync(string location, bool forceClone = false);

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        UniTask<GameObject> SpawnAsync(string location, Transform parent, bool forceClone = false);

        /// <summary>
        /// 异步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">世界角度</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        UniTask<GameObject> SpawnAsync(string location, Transform parent, Vector3 position,
            Quaternion rotation, bool forceClone = false);

        /// <summary>
        /// 同步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        GameObject SpawnSync(string location, bool forceClone = false);

        /// <summary>
        /// 同步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        GameObject SpawnSync(string location, Transform parent, bool forceClone = false);

        /// <summary>
        /// 同步实例化一个游戏对象
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="position">世界坐标</param>
        /// <param name="rotation">世界角度</param>
        /// <param name="forceClone">强制克隆游戏对象，忽略缓存池里的对象</param>
        GameObject SpawnSync(string location, Transform parent, Vector3 position,
            Quaternion rotation, bool forceClone = false);

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="gameObject"></param>
        void Recycle(GameObject gameObject);

        /// <summary>
        /// 丢弃对象
        /// </summary>
        /// <param name="gameObject"></param>
        void Remove(GameObject gameObject);

        GameObjectPool GetGameObjectPool(string location);

        bool TryGetGameObjectPool(string location, out GameObjectPool pool);

        /// <summary>
        /// 销毁指定对象池
        /// </summary>
        /// <param name="location">资源定位地址</param>
        void DestroyPool(string location);

        /// <summary>
        /// 销毁所有对象池
        /// </summary>
        /// <param name="includeAll">是否包括常驻对象池</param>
        void DestroyAllPool(bool includeAll);
    }
}