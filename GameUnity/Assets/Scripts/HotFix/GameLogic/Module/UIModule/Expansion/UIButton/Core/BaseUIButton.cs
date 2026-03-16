using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    [DisallowMultipleComponent]
    [System.Serializable]
    public abstract class BaseUIButton : Button, IUpdateSelectedHandler
    {
        #region Properties

        [SerializeField] private UnityEvent m_buttonClickEvent = new UnityEvent(); // 按钮可点击时候触发
        [SerializeField] private UIButtonClickProtectExtend m_uiButtonClickProtect = new UIButtonClickProtectExtend();
        [SerializeField] private UIButtonClickScaleExtend m_uiButtonClickScale = new UIButtonClickScaleExtend();
        [SerializeField] private UIButtonLongPressExtend m_uiButtonLongPress = new UIButtonLongPressExtend();
        [SerializeField] private UIButtonDoubleClickExtend m_uiButtonDoubleClick = new UIButtonDoubleClickExtend();
        [SerializeField] private UIButtonClickSoundExtend m_uiButtonClickSound = new UIButtonClickSoundExtend();

        private Vector2 m_pressPos; // 按下的坐标
        private bool m_isPress; // 是否按下
        private bool m_isClickDown;
        private PointerEventData m_pointerEventData;
        public Action OnPointerUpEvent; // 按钮不可点击也触发

        public UIButtonClickScaleExtend ClickScaleExtend => m_uiButtonClickScale;

        #endregion

        protected override void Awake()
        {
            base.Awake();
            m_uiButtonClickProtect?.Awake();
        }

        protected override void OnEnable()
        {
            m_uiButtonClickProtect?.OnEnable();
            m_uiButtonClickScale?.OnEnable(transform);
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            m_uiButtonLongPress?.OnUpdateSelected();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (m_uiButtonClickProtect.IsUseClickProtect && !m_uiButtonClickProtect.CanClick)
            {
                return;
            }

            if (interactable)
            {
                m_uiButtonClickSound?.OnPointerClick();
                base.OnPointerClick(eventData);
                // onClick?.Invoke();
            }
            // 连点保护在这里触发抬起事件 才算完成一次点击 开始倒计时
            m_uiButtonClickProtect?.OnPointerClick();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!m_uiButtonClickProtect.CanClick)
            {
                return;
            }
            base.OnPointerDown(eventData);
            m_pressPos = eventData.position;
            m_isPress = true;
            m_pointerEventData = eventData;
            m_isClickDown = true;
            m_uiButtonClickProtect?.OnPointerDown();
            m_uiButtonLongPress?.OnPointerDown();
            m_uiButtonDoubleClick?.OnPointerDown();
            m_uiButtonClickScale?.OnPointerDown(transform, interactable);

            if (interactable)
            {
                m_uiButtonClickSound?.OnPointerDown();
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!m_isClickDown || !m_uiButtonClickProtect.CanClick)
            {
                return;
            }

            if (m_isClickDown)
            {
                m_isClickDown = false;
            }
            base.OnPointerUp(eventData);
            m_isPress = false;
            m_pointerEventData = null;

            if (interactable && Mathf.Abs(Vector2.Distance(m_pressPos, eventData.position)) < 10f)
            {
                m_buttonClickEvent?.Invoke();
            }
            OnPointerUpEvent?.Invoke();
            m_uiButtonClickProtect?.OnPointerUp();
            m_uiButtonLongPress?.OnPointerUp();
            m_uiButtonClickScale?.OnPointerUp(transform, interactable);
            if (interactable)
            {
                m_uiButtonClickSound?.OnPointerUp();
            }
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Update()
        {
            m_uiButtonClickProtect?.OnUpdate();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                if (m_isPress && m_pointerEventData != null)
                {
                    OnPointerUp(m_pointerEventData);
                }
            }
        }

        protected override void OnDisable()
        {
            m_uiButtonClickScale?.OnDestroy(transform);
        }

        protected override void OnDestroy()
        {
            m_uiButtonClickScale?.OnDestroy(transform);
        }

        /// <summary>
        /// 添加按钮长按时间
        /// </summary>
        /// <param name="callback">长按后回调</param>
        /// <param name="duration">长按持续时间</param>
        public void AddButtonLongPressListener(UnityAction callback, float duration)
        {
            m_uiButtonLongPress?.AddLongPressListener(callback, duration);
        }

        /// <summary>
        /// 添加按钮长按持续触发时间
        /// </summary>
        /// <param name="callback">长按后回调</param>
        /// <param name="interval">长按持续触发间隔</param>
        public void AddButtonLoopLongPressListener(UnityAction callback, float interval)
        {
            m_uiButtonLongPress?.AddLoopLongPressListener(callback, interval);
        }

        /// <summary>
        /// 添加按钮双击触发事件
        /// </summary>
        /// <param name="callback">双击触发回调</param>
        /// <param name="interval">双击时间间隔</param>
        public void AddButtonDoubleClickListener(UnityAction callback, float interval)
        {
            m_uiButtonDoubleClick?.AddDoubleClickListener(callback, interval);
        }

        /// <summary>
        /// 设置点击音效ID
        /// </summary>
        /// <param name="soundID">音效ID</param>
        public void SetClickSoundID(int soundID)
        {
            m_uiButtonClickSound?.SetClickSoundID(soundID);
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            // if (m_buttonClickScaleExtend.UseClickScale)
            // {
            //     transition = Transition.None;
            // }
            //Navigation tempNavigation = navigation;
            //tempNavigation.mode = Navigation.Mode.None;
            //navigation = tempNavigation;
        }

#endif
    }
}