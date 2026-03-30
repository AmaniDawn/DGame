#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR

using Sirenix.OdinInspector;

#endif

namespace DGame
{
    [System.Serializable]
    public class LoadedAssetObject
    {

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector]
#endif
        public ISetAssetObject assetObject { get; }

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector]
#endif
        public UnityEngine.Object assetTarget { get; }

#if UNITY_EDITOR
        public bool isSelect { get; }
#endif

        public LoadedAssetObject(ISetAssetObject assetObject, UnityEngine.Object assetTarget)
        {
            this.assetObject = assetObject;
            this.assetTarget = assetTarget;
        }
    }
}