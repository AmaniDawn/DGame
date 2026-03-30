namespace DGame
{
    public class AssetItemObject : BasePoolObject
    {
        public static AssetItemObject Create(string location, UnityEngine.Object target)
        {
            AssetItemObject assetItemObject = MemoryPool.Spawn<AssetItemObject>();
            assetItemObject.Initialize(location, target);
            return assetItemObject;
        }

        protected internal override void ReleaseObj(bool isDestroy)
        {
            if (Target == null)
            {
                return;
            }
            ResourceExtComponent.ResourceModule?.UnloadAsset(Target);
        }
    }
}