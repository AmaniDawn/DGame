using UnityEngine;
using UnityEngine.Events;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonDoubleClickExtend
    {
        [SerializeField] private bool m_isUseDoubleClick;
        [SerializeField] private float m_clickInterval;
        [SerializeField] private UnityEvent m_onDoubleClickEvent;
        [SerializeField] private float m_lastPointDownTime;


        public void OnPointerDown()
        {
            if (!m_isUseDoubleClick)
            {
                return;
            }

            m_lastPointDownTime = Time.realtimeSinceStartup - m_lastPointDownTime < m_clickInterval ? 0 : Time.realtimeSinceStartup;
            if (m_lastPointDownTime == 0)
            {
                m_onDoubleClickEvent?.Invoke();
            }
        }

        public void AddDoubleClickListener(UnityAction callback, float clickInterval)
        {
            m_isUseDoubleClick = true;
            m_clickInterval = clickInterval;
            m_onDoubleClickEvent.AddListener(callback);
        }
    }
}