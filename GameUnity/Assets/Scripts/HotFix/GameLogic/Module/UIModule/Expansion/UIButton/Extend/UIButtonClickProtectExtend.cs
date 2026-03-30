using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonClickProtectExtend
    {
        [SerializeField] private bool m_isUseClickProtect;
        [SerializeField, Range(0, 5)] private float m_protectTime = 0.2f;
        [SerializeField] private bool m_isShowProtectText;
        [SerializeField] private Text m_protectText;

        private string m_originalText;
        private float m_lastClickTime;
        private float m_clickRunTimer;
        private bool m_canClick = true;
        public bool CanClick => m_canClick;
        public bool IsUseClickProtect => m_isUseClickProtect;
        public bool IsShowProtectText => m_isShowProtectText;

        public void Awake()
        {
            GetProtectOriginalText();
        }

        public void OnEnable()
        {
            m_canClick = true;
            SetProtectText(0);
        }

        public void OnPointerDown()
        {

        }

        public void OnPointerUp()
        {

        }

        public void OnPointerClick()
        {
            GetProtectOriginalText();
            if (m_isUseClickProtect)
            {
                if (m_canClick)
                {
                    m_lastClickTime = Time.realtimeSinceStartup;
                    m_canClick = false;
                }
            }
        }

        public void OnUpdate()
        {
            ClickProtect();
        }

        private void ClickProtect()
        {
            if (!m_canClick)
            {
                m_clickRunTimer = Time.realtimeSinceStartup - m_lastClickTime;
                if (m_clickRunTimer >= m_protectTime)
                {
                    m_canClick = true;
                }

                SetProtectText(m_protectTime - m_clickRunTimer);
            }
        }

        private void SetProtectText(float protectTime)
        {
            if (m_isShowProtectText && m_protectText != null)
            {
                if (protectTime > 0)
                {
                    m_protectText.text = $"{m_originalText}({(int)(protectTime + 1)})" ;
                }
                else
                {
                    m_protectText.text = m_originalText;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_protectText.rectTransform);
                }
            }
        }

        private void GetProtectOriginalText()
        {
            if (IsShowProtectText && m_protectText != null)
            {
                m_originalText = m_protectText.text;
            }
        }
    }
}