using YooAsset;

namespace DGame
{
    internal partial class ResourceModule
    {
        /// <summary>
        /// 资源对象
        /// <remarks>对象池对象</remarks>
        /// </summary>
        private sealed class AssetObject : BasePoolObject
        {
            /// <summary>
            /// 资源句柄
            /// </summary>
            private AssetHandle m_assetHandle = null;
            private ResourceModule m_resourceModule;

            public static AssetObject Create(string name, object target,
                object assetHandle, ResourceModule resourceModule)
            {
                if (assetHandle == null)
                {
                    throw new DGameException("资源句柄无效");
                }

                if (resourceModule == null)
                {
                    throw new DGameException("资源管理器无效");
                }

                AssetObject assetObject = MemoryPool.Spawn<AssetObject>();
                assetObject.Initialize(name, target);
                assetObject.m_assetHandle = assetHandle as AssetHandle;
                assetObject.m_resourceModule = resourceModule;
                return assetObject;
            }

            public override void OnRelease()
            {
                m_assetHandle = null;
                base.OnRelease();
            }

            protected internal override void ReleaseObj(bool isDestroy)
            {
                if (!isDestroy)
                {
                    // 释放资源句柄
                    if (m_assetHandle != null && m_assetHandle.IsValid)
                    {
                        m_assetHandle.Dispose();
                    }
                    m_assetHandle = null;
                }
            }
        }
    }
}