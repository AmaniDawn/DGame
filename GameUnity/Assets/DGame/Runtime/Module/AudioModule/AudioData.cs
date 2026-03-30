using YooAsset;

namespace DGame
{
    /// <summary>
    /// 音频资源数据
    /// </summary>
    public class AudioData : MemoryObject
    {
        public AssetHandle AssetHandle { get; private set; }

        public bool InPool { get; private set; } = false;

        public override void OnRelease()
        {
            if (!InPool)
            {
                AssetHandle.Dispose();
            }
            InPool = false;
            AssetHandle = null;
        }

        internal static AudioData Spawn(AssetHandle assetHandle, bool inPool)
        {
            AudioData audioData = MemoryPool.Spawn<AudioData>();
            audioData.AssetHandle = assetHandle;
            audioData.InPool = inPool;
            return audioData;
        }

        internal static void Release(AudioData audioData)
        {
            if (audioData != null)
            {
                MemoryPool.Release(audioData);
            }
        }
    }
}