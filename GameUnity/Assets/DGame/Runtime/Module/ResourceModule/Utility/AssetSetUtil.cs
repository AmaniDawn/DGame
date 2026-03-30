using UnityEngine;
using UnityEngine.UI;

namespace DGame
{
    public static class AssetSetUtil
    {
        private static IResourceModule m_resourceModule;

        private static void CheckResourceModule()
        {
            if (m_resourceModule != null)
            {
                return;
            }
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
        }

        #region SetMaterial

        public static void SetMaterial(this Image image, string location, bool isAsync = false, string packageName = "")
        {
            if (image == null)
            {
                throw new DGameException("设置资源材质失败，Image为空");
            }

            CheckResourceModule();

            if (isAsync)
            {
                m_resourceModule.LoadAssetAsync<Material>(location, material =>
                {
                    if (image == null || image.gameObject == null)
                    {
                        m_resourceModule.UnloadAsset(material);
                        return;
                    }
                    image.material = material;
                    AssetReference.Ref(material, image.gameObject);
                }, packageName);
            }
            else
            {
                Material material = m_resourceModule.LoadAsset<Material>(location, packageName);
                image.material = material;
                AssetReference.Ref(material, image.gameObject);
            }
        }

        public static void SetMaterial(this SpriteRenderer spriteRenderer, string location,
            bool isAsync = false, string packageName = "")
        {
            if (spriteRenderer == null)
            {
                throw new DGameException("设置资源材质失败，spriteRenderer为空");
            }

            CheckResourceModule();

            if (isAsync)
            {
                m_resourceModule.LoadAssetAsync<Material>(location, material =>
                {
                    if (spriteRenderer == null || spriteRenderer.gameObject == null)
                    {
                        m_resourceModule.UnloadAsset(material);
                        return;
                    }
                    spriteRenderer.material = material;
                    AssetReference.Ref(material, spriteRenderer.gameObject);
                }, packageName);
            }
            else
            {
                Material material = m_resourceModule.LoadAsset<Material>(location, packageName);
                spriteRenderer.material = material;
                AssetReference.Ref(material, spriteRenderer.gameObject);
            }
        }

        public static void SetSharedMaterial(this MeshRenderer meshRenderer, string location,
            bool isAsync = false, string packageName = "")
        {
            if (meshRenderer == null)
            {
                throw new DGameException("设置资源材质失败，meshRenderer为空");
            }

            CheckResourceModule();

            if (isAsync)
            {
                m_resourceModule.LoadAssetAsync<Material>(location, material =>
                {
                    if (meshRenderer == null || meshRenderer.gameObject == null)
                    {
                        m_resourceModule.UnloadAsset(material);
                        return;
                    }
                    meshRenderer.sharedMaterial = material;
                    AssetReference.Ref(material, meshRenderer.gameObject);
                }, packageName);
            }
            else
            {
                Material material = m_resourceModule.LoadAsset<Material>(location, packageName);
                meshRenderer.sharedMaterial = material;
                AssetReference.Ref(material, meshRenderer.gameObject);
            }
        }


        public static void SetMaterial(this MeshRenderer meshRenderer, string location,
            bool needInstance = true, bool isAsync = false, string packageName = "")
        {
            if (meshRenderer == null)
            {
                throw new DGameException("设置资源材质失败，meshRenderer为空");
            }

            CheckResourceModule();

            if (isAsync)
            {
                m_resourceModule.LoadAssetAsync<Material>(location, material =>
                {
                    if (meshRenderer == null || meshRenderer.gameObject == null)
                    {
                        m_resourceModule.UnloadAsset(material);
                        return;
                    }
                    meshRenderer.material = needInstance ? Object.Instantiate(material) : material;
                    AssetReference.Ref(material, meshRenderer.gameObject);
                }, packageName);
            }
            else
            {
                Material material = m_resourceModule.LoadAsset<Material>(location, packageName);
                meshRenderer.material = needInstance ? Object.Instantiate(material) : material;
                AssetReference.Ref(material, meshRenderer.gameObject);
            }
        }

        #endregion
    }
}