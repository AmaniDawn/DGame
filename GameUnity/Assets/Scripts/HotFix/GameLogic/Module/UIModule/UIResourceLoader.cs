using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public interface IUIResourceLoader
    {
        /// <summary>
        /// 同步加载游戏物体并实例化
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="packageName">资源包名</param>
        /// <returns></returns>
        public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "");

        /// <summary>
        /// 异步加载游戏物体并实例化
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="cancellationToken">取消操作Token</param>
        /// <param name="packageName">资源包名</param>
        /// <returns></returns>
        public UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null, CancellationToken cancellationToken = default, string packageName = "");
    }

    public class UIResourceLoader : IUIResourceLoader
    {
        public readonly IResourceModule m_resourceLoaderImp = GameModule.ResourceModule;

        public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "")
        {
            return m_resourceLoaderImp.LoadGameObject(location, parent, packageName);
        }

        public async UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null, CancellationToken cancellationToken = default,
            string packageName = "")
        {
            return await m_resourceLoaderImp.LoadGameObjectAsync(location, parent, cancellationToken, packageName);
        }
    }
}