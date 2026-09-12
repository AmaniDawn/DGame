using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// UI 按压缩放反馈组件。挂载到需要按压反馈的 UI 节点上，按下缩小，抬起或移出恢复。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIButtonScale : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Transform m_tfTween;
        [SerializeField] private Vector3 m_pressedScale = new Vector3(0.95f, 0.95f, 0.95f);
        [SerializeField, Min(0f)] private float m_duration = 0.1f;
        [SerializeField] private bool m_removeAllTween = true;

        private Vector3 m_cacheScale;
        private Tween m_scaleTween;
        private bool m_started;
        private bool m_pressed;

        private void Start()
        {
            Init();
        }

        private void OnEnable()
        {
            if (!m_started)
            {
                return;
            }

            m_pressed = false;
            KillTween();
            if (m_tfTween != null)
            {
                m_tfTween.localScale = m_cacheScale;
            }
        }

        private void OnDisable()
        {
            if (!m_started || m_tfTween == null)
            {
                return;
            }

            KillTween();
            m_tfTween.localScale = m_cacheScale;
        }

        private void OnDestroy()
        {
            KillTween();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_pressed = true;
            OnPress(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_pressed = false;
            OnPress(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPress(m_pressed);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPress(false);
        }

        /// <summary>
        /// 设置新的默认缩放，并停止当前反馈动画。
        /// </summary>
        public void SetDefaultScale(Vector3 scale)
        {
            Init();
            m_cacheScale = scale;
            KillTween();
            if (m_tfTween != null)
            {
                m_tfTween.localScale = scale;
            }
        }

        private void Init()
        {
            if (m_started)
            {
                return;
            }

            m_started = true;
            m_tfTween ??= transform;
            m_cacheScale = m_tfTween.localScale;
        }

        private void OnPress(bool isPressed)
        {
            Init();
            if (!enabled || m_tfTween == null)
            {
                return;
            }

            KillTween();
            Vector3 destinationScale = isPressed
                ? Vector3.Scale(m_cacheScale, m_pressedScale)
                : m_cacheScale;
            m_scaleTween = m_tfTween.DOScale(destinationScale, m_duration).SetUpdate(true);
        }

        private void KillTween()
        {
            if (m_tfTween == null)
            {
                return;
            }

            if (m_removeAllTween)
            {
                m_tfTween.DOKill();
            }
            else
            {
                m_scaleTween?.Kill();
            }

            m_scaleTween = null;
        }
    }
}
