using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonLongPressExtend
    {
        [SerializeField] private bool m_isUseLongPress;
        [SerializeField, Range(0, 10)] private float m_pressDuration;
        [SerializeField] private bool m_isLoopLongPress;
        [SerializeField, Range(0, 10)] private float m_interval;
        [SerializeField] private UnityEvent m_onLongPressEvent;
        private float m_pointerDownTime;
        private bool m_isTriggered;

        public void OnPointerDown()
        {
            m_pointerDownTime = Time.realtimeSinceStartup;
        }

        public void OnPointerUp()
        {
            if (!m_isUseLongPress)
            {
                return;
            }

            m_isTriggered = false;
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void OnUpdateSelected()
        {
            if (!m_isUseLongPress)
            {
                return;
            }
            if (m_isLoopLongPress)
            {
                if (Time.realtimeSinceStartup - m_pointerDownTime >= m_interval)
                {
                    m_onLongPressEvent?.Invoke();
                    m_pointerDownTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                if (!m_isTriggered && m_pressDuration >= 0 &&
                    Time.realtimeSinceStartup - m_pointerDownTime >= m_pressDuration)
                {
                    m_onLongPressEvent?.Invoke();

                    if (!m_isLoopLongPress)
                    {
                        EventSystem.current.SetSelectedGameObject(null);
                    }
                    m_pointerDownTime = Time.realtimeSinceStartup;
                    m_isTriggered = true;
                }
            }
        }

        public void AddLongPressListener(UnityAction callback, float duration)
        {
            m_pressDuration = duration;
            m_isUseLongPress = true;
            m_onLongPressEvent.AddListener(callback);
        }

        public void AddLoopLongPressListener(UnityAction callback, float interval)
        {
            m_interval = interval;
            m_isLoopLongPress = true;
            m_isUseLongPress = true;
            m_onLongPressEvent.AddListener(callback);
        }
    }
}