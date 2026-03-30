#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR

using Sirenix.OdinInspector;

#endif

using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace DGame
{
    [System.Serializable]
    public class SetSpriteObject : ISetAssetObject
    {
        enum SetType
        {
            None,
            Image,
            SpriteRender,
        }

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector]
#endif
        private SetType m_setType;

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector]
#endif
        private Image m_image;

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector]
#endif
        private SpriteRenderer m_spriteRenderer;

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR && UNITY_EDITOR
        [ShowInInspector]
#endif
        private UnityEngine.Sprite m_sprite;

        public string Location { get; private set; }
        public UnityEngine.Object TargetObject { get; set; }

        private bool m_setNativeSize = false;
        Action<Image> m_imageCallback;
        Action<SpriteRenderer> m_spriteRendererCallback;
        private CancellationToken m_cancellationToken;

        public void OnRelease()
        {
            m_spriteRenderer = null;
            m_image = null;
            Location = null;
            m_setType = SetType.None;
            m_setNativeSize = false;
            m_sprite = null;
        }

        public void SetAsset(UnityEngine.Object asset)
        {
            m_sprite = asset as UnityEngine.Sprite;

            if (m_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (m_image != null)
            {
                m_image.sprite = m_sprite;

                if (m_setNativeSize)
                {
                    m_image.SetNativeSize();
                }
                m_imageCallback?.Invoke(m_image);
            }
            else if (m_spriteRenderer != null)
            {
                m_spriteRenderer.sprite = m_sprite;
                m_spriteRendererCallback?.Invoke(m_spriteRenderer);
            }
        }

        public bool IsCanRelease()
        {
            if (m_setType == SetType.Image)
            {
                return m_image == null || m_image.sprite == null || (m_sprite != null && m_image.sprite != m_sprite);
            }

            if(m_setType == SetType.SpriteRender)
            {
                return m_spriteRenderer == null || m_spriteRenderer.sprite == null || (m_sprite != null && m_spriteRenderer.sprite != m_sprite);
            }
            return true;
        }

        public static SetSpriteObject Create(Image image, string location,
            bool isNativeSize, Action<Image> imageCallback = null, CancellationToken ct = default)
        {
            SetSpriteObject setSpriteObject = MemoryPool.Spawn<SetSpriteObject>();
            setSpriteObject.Location = location;
            setSpriteObject.m_setNativeSize = isNativeSize;
            setSpriteObject.m_imageCallback = imageCallback;
            setSpriteObject.m_setType = SetType.Image;
            setSpriteObject.m_image = image;
            setSpriteObject.m_cancellationToken = ct;
            setSpriteObject.TargetObject = image;
            return setSpriteObject;
        }

        public static SetSpriteObject Create(SpriteRenderer spriteRenderer, string location,
            Action<SpriteRenderer> imageCallback = null, CancellationToken ct = default)
        {
            SetSpriteObject setSpriteObject = MemoryPool.Spawn<SetSpriteObject>();
            setSpriteObject.Location = location;
            setSpriteObject.m_spriteRendererCallback = imageCallback;
            setSpriteObject.m_setType = SetType.SpriteRender;
            setSpriteObject.m_spriteRenderer = spriteRenderer;
            setSpriteObject.m_cancellationToken = ct;
            setSpriteObject.TargetObject = spriteRenderer;
            return setSpriteObject;
        }
    }
}