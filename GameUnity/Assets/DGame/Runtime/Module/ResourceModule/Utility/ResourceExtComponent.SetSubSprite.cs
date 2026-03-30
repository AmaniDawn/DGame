using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace DGame
{
    internal partial class ResourceExtComponent
    {
        private readonly Dictionary<string, SubAssetsHandle> m_subAssetsHandles =
            new Dictionary<string, SubAssetsHandle>();

        private readonly Dictionary<string, int> m_subSpriteReferences = new Dictionary<string, int>();

        public async UniTask SetSubSprite(Image image, string location, string spriteName, bool setNativeSize = false,
            CancellationToken cancellationToken = default)
        {
            var subSprite = await GetSubSpriteImp(location, spriteName, cancellationToken);

            if (image == null)
            {
                DLogger.Warning("[SetSubAssets] Image is null");
                return;
            }

            image.sprite = subSprite;

            if (setNativeSize)
            {
                image.SetNativeSize();
            }

            AddReference(image.gameObject, location);
        }

        public async UniTask SetSubSprite(SpriteRenderer spriteRenderer, string location, string spriteName,
            CancellationToken cancellationToken = default)
        {
            var subSprite = await GetSubSpriteImp(location, spriteName, cancellationToken);

            if (spriteRenderer == null)
            {
                DLogger.Warning("[SetSubAssets] Image is null");
                return;
            }

            spriteRenderer.sprite = subSprite;
            AddReference(spriteRenderer.gameObject, location);
        }

        private async UniTask<UnityEngine.Sprite> GetSubSpriteImp(string location, string spriteName,
            CancellationToken cancellationToken = default)
        {
            // var assetInfo = YooAssets.GetAssetInfo(location);
            var assetInfo = m_resourceModule.GetAssetInfo(location);

            if (assetInfo.IsInvalid)
            {
                throw new DGameException($"[GetSubSpriteImp] Invalid location: {location}");
            }

            await TryWaitingLoading(location);

            if (!m_subAssetsHandles.TryGetValue(location, out var subAssetsHandle))
            {
                subAssetsHandle = YooAssets.LoadSubAssetsAsync<UnityEngine.Sprite>(location);
                await subAssetsHandle.ToUniTask(cancellationToken: cancellationToken);
                m_subAssetsHandles[location] = subAssetsHandle;
            }

            var subSprite = subAssetsHandle.GetSubAssetObject<UnityEngine.Sprite>(spriteName);

            if (subSprite == null)
            {
                throw new DGameException($"[GetSubSpriteImp] Invalid sprite name: {spriteName}");
            }

            return subSprite;
        }

        private void AddReference(GameObject target, string location)
        {
            if (!target.TryGetComponent<SubSpriteReference>(out var subSpriteReference))
            {
                subSpriteReference = target.AddComponent<SubSpriteReference>();
            }

            m_subSpriteReferences[location] =
                m_subSpriteReferences.TryGetValue(location, out var count) ? count + 1 : 1;
            subSpriteReference.Reference(location);
        }

        internal void DeleteReference(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return;
            }

            m_subSpriteReferences[location] =
                m_subSpriteReferences.TryGetValue(location, out var count) ? count - 1 : 0;

            if (m_subSpriteReferences[location] <= 0)
            {
                var subAssetsHandle = m_subAssetsHandles[location];
                subAssetsHandle.Dispose();
                m_subAssetsHandles.Remove(location);
                m_subSpriteReferences.Remove(location);
            }
        }
    }

    [DisallowMultipleComponent]
    public class SubSpriteReference : MonoBehaviour
    {
        private string m_location;

        public void Reference(string location)
        {
            if (!string.IsNullOrEmpty(m_location) && m_location.Equals(location))
            {
                ResourceExtComponent.Instance?.DeleteReference(m_location);
            }
            m_location = location;
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(m_location))
            {
                ResourceExtComponent.Instance?.DeleteReference(m_location);
            }
        }
    }
}