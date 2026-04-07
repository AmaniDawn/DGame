using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    [System.Serializable]
    public struct AssetRefInfo
    {
        public readonly int instanceID;
        public readonly Object refAsset;

        public AssetRefInfo(Object asset)
        {
            refAsset = asset;
            instanceID = refAsset.GetInstanceID();
        }
    }

    [DisallowMultipleComponent]
    public class AssetReference : MonoBehaviour
    {
        private static IResourceModule s_resourceModule;
        private static Dictionary<GameObject, AssetReference> s_originalRefs = new Dictionary<GameObject, AssetReference>();

        [SerializeField] private GameObject sourceGameObject;
        [SerializeField] private List<AssetRefInfo> refInfos = new List<AssetRefInfo>();

        private void CheckInit()
        {
            if (s_resourceModule != null)
            {
                return;
            }

            s_resourceModule = ModuleSystem.GetModule<IResourceModule>();

            if (s_resourceModule == null)
            {
                throw new DGameException("资源管理器无效");
            }
        }

        private void CheckRelease()
        {
            if (sourceGameObject != null)
            {
                s_resourceModule?.UnloadAsset(sourceGameObject);
            }
            else
            {
                DLogger.Warning("游戏对象无效");
            }
        }

        private void Awake()
        {
            // 如果不是原始实例（即克隆体）
            if (!IsOriginalInstance())
            {
                // 清理克隆体的引用
                ClearCloneReferences();
            }
        }

        /// <summary>
        /// 判断当前实例是否为原始实例
        /// </summary>
        /// <returns></returns>
        private bool IsOriginalInstance()
        {
            // 检查字典中是否包含当前GameObject，且映射的AssetReference就是当前组件
            return s_originalRefs.TryGetValue(gameObject, out AssetReference reference) && reference == this;
        }

        private void ClearCloneReferences()
        {
            sourceGameObject = null;
            refInfos?.Clear();
        }

        private void OnDestroy()
        {
            if (s_originalRefs.TryGetValue(gameObject, out var reference) && reference == this)
            {
                s_originalRefs.Remove(gameObject);
            }

            CheckInit();

            if (sourceGameObject != null)
            {
                CheckRelease();
            }

            ReleaseRefAssetInfos();
        }

        private void ReleaseRefAssetInfos()
        {
            if (refInfos != null)
            {
                for (int i = 0; i < refInfos.Count; i++)
                {
                    s_resourceModule?.UnloadAsset(refInfos[i].refAsset);
                }
                refInfos.Clear();
            }
        }

        public AssetReference Ref(GameObject source, IResourceModule resourceModule = null)
        {
            if (source == null)
            {
                throw new DGameException("游戏对象是无效的");
            }

            if (source.scene.name != null)
            {
                throw new DGameException("游戏对象已经存在此场景中");
            }

            if (resourceModule != null)
            {
                s_resourceModule = resourceModule;
            }
            sourceGameObject = source;
            s_originalRefs[gameObject] = this;
            return this;
        }

        /// <summary>
        /// 引用一个泛型资源（如Texture、AudioClip等）
        /// </summary>
        /// <param name="source">资源</param>
        /// <param name="resourceModule"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public AssetReference Ref<T>(T source, IResourceModule resourceModule = null) where T : Object
        {
            if (source == null)
            {
                throw new DGameException("资源是无效的");
            }

            if (resourceModule != null)
            {
                s_resourceModule = resourceModule;
            }

            if (refInfos == null)
            {
                refInfos = new List<AssetRefInfo>();
            }

            refInfos.Add(new AssetRefInfo(source));
            return this;
        }

        internal static AssetReference Instantiate(GameObject source, Transform parent = null,
            IResourceModule resourceModule = null)
        {
            if (source == null)
            {
                throw new DGameException("游戏对象是无效的");
            }

            if (source.scene.name != null)
            {
                throw new DGameException("游戏对象已经存在此场景中");
            }

            return Object.Instantiate(source, parent)?.AddComponent<AssetReference>()?.Ref(source, resourceModule);
        }

        /// <summary>
        /// 为已存在的GameObject引用一个源GameObject
        /// </summary>
        /// <param name="source"></param>
        /// <param name="instance"></param>
        /// <param name="resourceModule"></param>
        /// <returns></returns>
        public static AssetReference Ref(GameObject source, GameObject instance, IResourceModule resourceModule = null)
        {
            if (source == null)
            {
                throw new DGameException("游戏对象是无效的");
            }

            if (source.scene.name != null)
            {
                throw new DGameException("游戏对象已经存在此场景中");
            }

            if (!instance.TryGetComponent<AssetReference>(out var assetRef))
            {
                assetRef = instance.AddComponent<AssetReference>();
            }

            return assetRef?.Ref(source, resourceModule);
        }

        /// <summary>
        /// 为已存在的GameObject引用一个泛型资源
        /// </summary>
        /// <param name="source"></param>
        /// <param name="instance"></param>
        /// <param name="resourceModule"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AssetReference Ref<T>(T source, GameObject instance,
            IResourceModule resourceModule = null) where T : Object
        {
            if (source == null)
            {
                throw new DGameException("资源对象是无效的");
            }

            if (!instance.TryGetComponent<AssetReference>(out var assetRef))
            {
                assetRef = instance.AddComponent<AssetReference>();
            }

            return assetRef?.Ref(source, resourceModule);
        }
    }
}