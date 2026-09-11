using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonClickScaleExtend
    {
        [SerializeField] private bool m_isUseClickScale = true;
        [SerializeField] private float m_clickScaleTime = 0.1f;
        [SerializeField] private float m_clickRecoverTime = 0.18f;

        [SerializeField] private Vector3 m_normalScale = Vector3.one;
        [SerializeField] private Vector3 m_clickScale = new Vector3(0.95f, 0.95f, 0.95f);

        [SerializeField] private List<Transform> m_childList;

        [SerializeField] private bool m_isUseDoTween = true; // 开启缩放动画效果
        [SerializeField] private bool m_reboundEffect = true; // 回弹效果开启

        public bool IsUseClickScale
        {
            get => m_isUseClickScale;
            set => m_isUseClickScale = value;
        }

        public Vector3 NormalScale
        {
            get => m_normalScale;
            set => m_normalScale = value;
        }

        public Vector3 ClickScale
        {
            get => m_clickScale;
            set => m_clickScale = value;
        }

        public void OnEnable(Transform transf)
        {
            KillTween(transf);
            // 恢复正常缩放，防止上次禁用时缩放动画未完成
            if (transf != null)
            {
                transf.localScale = m_normalScale;
            }
            if (m_childList != null && m_childList.Count > 0)
            {
                foreach (var child in m_childList)
                {
                    if (child != null)
                    {
                        child.localScale = m_normalScale;
                    }
                }
            }
        }

        /// <summary>
        /// 确认短按后播放一次缩小并恢复的点击反馈。
        /// </summary>
        /// <param name="transf">按钮根节点。</param>
        /// <param name="interactable">按钮是否允许交互。</param>
        public void PlayClickFeedback(Transform transf, bool interactable)
        {
            if (!m_isUseClickScale || !interactable)
            {
                return;
            }

            if (!m_isUseDoTween)
            {
                OnEnable(transf);
                return;
            }

            KillTween(transf);
            PlayClickFeedbackTween(transf);
            if (m_childList != null && m_childList.Count > 0)
            {
                foreach (var child in m_childList)
                {
                    PlayClickFeedbackTween(child);
                }
            }
        }

        public void OnPointerDown(Transform transf, bool interactable)
        {
            if (m_isUseClickScale && interactable)
            {
                if (m_isUseDoTween)
                {
                    transf?.DOKill();
                    transf.DOScale(m_clickScale, m_clickScaleTime).SetUpdate(true).SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete += () =>
                    {
                        transf?.DOKill();
                    };

                    if (m_childList != null && m_childList.Count > 0)
                    {
                        foreach (var child in m_childList)
                        {
                            child?.DOKill();
                            child.DOScale(m_clickScale, m_clickScaleTime).SetUpdate(true).SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete += () =>
                            {
                                child?.DOKill();
                            };
                        }
                    }
                }
                else
                {
                    transf.localScale = m_clickScale;

                    if (m_childList != null && m_childList.Count > 0)
                    {
                        for (var index = 0; index < m_childList.Count; index++)
                        {
                            var child = m_childList[index];
                            child.localScale = m_clickScale;
                        }
                    }
                }
            }
        }

        public void OnPointerUp(Transform transf, bool interactable)
        {
            if (transf == null)
            {
                return;
            }
            if (m_isUseClickScale && interactable)
            {
                if (m_isUseDoTween)
                {
                    transf.DOKill();
                    transf.DOScale(m_normalScale, m_clickRecoverTime).SetUpdate(true).SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete += () =>
                    {
                        transf.DOKill();
                    };

                    if (m_childList != null && m_childList.Count > 0)
                    {
                        for (var index = 0; index < m_childList.Count; index++)
                        {
                            var child = m_childList[index];
                            child?.DOKill();
                            child.DOScale(m_normalScale, m_clickRecoverTime).SetUpdate(true)
                                    .SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete +=
                                () => { child?.DOKill(); };
                        }
                    }
                }
                else
                {
                    transf.localScale = m_normalScale;
                    foreach (var child in m_childList)
                    {
                        child.localScale = m_normalScale;
                    }
                }
            }
        }

        /// <summary>
        /// 创建绑定节点的完整点击动画，保证节点清理时可停止整个序列。
        /// </summary>
        private void PlayClickFeedbackTween(Transform transf)
        {
            if (transf == null)
            {
                return;
            }

            var ease = m_reboundEffect ? Ease.OutBack : Ease.Unset;
            DOTween.Sequence()
                .Append(transf.DOScale(m_clickScale, m_clickScaleTime).SetEase(ease))
                .Append(transf.DOScale(m_normalScale, m_clickRecoverTime).SetEase(ease))
                .SetUpdate(true)
                .SetTarget(transf);
        }

        private void KillTween(Transform transf)
        {
            transf?.DOKill();

            if (m_childList != null && m_childList.Count > 0)
            {
                for (int i = 0; i < m_childList.Count; i++)
                {
                    m_childList[i]?.DOKill();
                }
            }
        }

        public void OnDisable(Transform transf)
        {
            KillTween(transf);
        }

        public void OnDestroy(Transform transf)
        {
            KillTween(transf);
        }
    }
}
