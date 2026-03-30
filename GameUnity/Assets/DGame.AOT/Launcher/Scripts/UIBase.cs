using DG.Tweening;
using UnityEngine;

namespace Launcher
{
    public class UIBase
    {
        private const float NORMAL_TWEEN_POP_TIME = 0.3f;
        public GameObject gameObject;

        private Transform m_transform;
        public Transform transform => m_transform != null ? m_transform : m_transform = gameObject?.transform;
        public RectTransform rectTransform => transform as RectTransform;

        protected virtual bool NeedTween => true;
        protected virtual bool FullScreen => false;
        private bool m_isInTween = false;

        protected object m_param;

        protected virtual void ScriptGenerator(){}

        public void CallScriptGenerator()
        {
            ScriptGenerator();
        }

        public void TweenPop()
        {
            if (m_isInTween || !transform)
            {
                return;
            }

            if (!FullScreen && NeedTween)
            {
                m_isInTween = true;
                transform.localScale = Vector3.one * 0.8f;
                transform.DOScale(Vector3.one, NORMAL_TWEEN_POP_TIME).SetEase(Ease.OutBack).SetUpdate(true).SetAutoKill(true).onComplete += OnTweenPopComplete;
            }
        }

        private void OnTweenPopComplete()
        {
            m_isInTween = false;
        }

        public virtual void OnInit(object param)
        {
            m_param = param;
        }

        public void Show()
        {
            gameObject?.SetActive(true);
            TweenPop();
        }

        public void Hide()
        {
            transform?.DOKill();
            gameObject?.SetActive(false);
        }

        public void Close()
        {
            transform?.DOKill();
            LauncherMgr.CloseUI(this);
        }

        #region FindChildComponent

        public Transform FindChild(string path)
        {
            return FindChildImp(rectTransform, path);
        }

        public Transform FindChild(Transform trans, string path)
        {
            return FindChildImp(trans, path);
        }

        public T FindChildComponent<T>(string path) where T : Component
        {
            return FindChildComponentImp<T>(rectTransform, path);
        }

        public T FindChildComponent<T>(Transform trans, string path) where T : Component
        {
            return FindChildComponentImp<T>(trans, path);
        }

        private static Transform FindChildImp(Transform trans, string path)
        {
            var findTrans = trans.Find(path);
            return findTrans == null ? null : findTrans;
        }

        private static T FindChildComponentImp<T>(Transform trans, string path) where T : Component
        {
            var findTrans = trans.Find(path);
            return findTrans == null ? null : findTrans.gameObject.GetComponent<T>();
        }

        #endregion
    }
}