using DGame;
using GameProto;
using UnityEngine;
using UnityEngine.UI;

#if TextMeshPro
using TMPro;
#endif

namespace GameLogic
{
    public enum UITextIDBinderResultType
    {
        /// <summary>
        /// 更新成功
        /// </summary>
        Success,

        /// <summary>
        /// 没有找到 Text 组件
        /// </summary>
        NoTextCom,

        /// <summary>
        /// 没有找到 TextID 的配置文件
        /// </summary>
        NoTextID,

        /// <summary>
        /// TextID = 0
        /// </summary>
        TextIDZero
    }

    [DisallowMultipleComponent]
    public class UITextIDBinder : MonoBehaviour
    {
        public int m_textID;

        [Header("预览语言(仅编辑器)")]
        public LocalAreaType m_previewLanguage = LocalAreaType.CN;

        [SerializeField]
        [Header("预览文本(仅编辑器,只读)")]
        public string m_previewText;

        private int m_curSelectLanguage = 0;

        /// <summary>
        /// 文本配置ID
        /// </summary>
        public int TextID
        {
            get => m_textID;
            set
            {
                if (m_textID != value)
                {
                    m_textID = value;
                    UpdateTextContent();
                }
            }
        }

        /// <summary>
        /// 预览语言
        /// </summary>
        public LocalAreaType PreviewLanguage
        {
            get => m_previewLanguage;
            set
            {
                if (m_previewLanguage != value)
                {
                    m_previewLanguage = value;
                    UpdateTextContent();
                }
            }
        }

        private Text m_textBinder;

        public Text TextBinder => m_textBinder == null ? m_textBinder = GetComponent<Text>() : m_textBinder;

#if TextMeshPro
        private TextMeshProUGUI m_textProBinder;

        public TextMeshProUGUI TextProBinder => m_textProBinder == null ? m_textProBinder = GetComponent<TextMeshProUGUI>() : m_textProBinder;
#endif

        private void Awake()
        {
            UpdateTextContent();
        }

        private void OnEnable()
        {
            GameEvent.AddEventListener<int>(ILocalization_Event.OnLanguageChanged, OnLanguageChanged);
        }

        private void OnDisable()
        {
            GameEvent.RemoveEventListener<int>(ILocalization_Event.OnLanguageChanged, OnLanguageChanged);
        }

        private void OnLanguageChanged(int language)
        {
            m_curSelectLanguage = language;
            UpdateTextContent();
        }

        public UITextIDBinderResultType UpdateTextContent()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string previewText = EditorConfigLoader.GetTextContent(m_textID, PreviewLanguage);

#if TextMeshPro
                // 更新 TextMeshPro 组件显示
                if (TextProBinder != null)
                {
                    TextProBinder.text = previewText;
                }
                else
#endif
                    // 更新 Unity Text 组件显示
                if (TextBinder != null)
                {
                    TextBinder.text = previewText;
                }

                // 更新预览字段（编辑模式下不直接修改序列化字段，避免触发Inspector刷新）
                m_previewText = previewText;
                return UITextIDBinderResultType.Success;
            }
#endif

            bool hasText = TextBinder != null;
#if TextMeshPro
            hasText = hasText || TextProBinder != null;
#endif

            if (!hasText)
            {
                return UITextIDBinderResultType.NoTextCom;
            }

            if (m_textID == 0)
            {
                m_previewText = string.Empty;
                return UITextIDBinderResultType.TextIDZero;
            }

            var textConfig = TextConfigMgr.Instance.GetTextConfig(m_textID);
            if (textConfig == null)
            {
                var textIDStr = TextID.ToString();
                m_previewText = $"LabelID{textIDStr}";

#if TextMeshPro
                if (TextProBinder != null)
                {
                    TextProBinder.text = textIDStr;
                }
                else
#endif
                if (TextBinder != null)
                {
                    TextBinder.text = textIDStr;
                }
                return UITextIDBinderResultType.NoTextID;
            }

            int langIndex = m_curSelectLanguage;
            if (langIndex < 0 || langIndex >= textConfig.Content.Length)
            {
                langIndex = 0;
            }

            m_previewText = textConfig.Content[langIndex];

#if TextMeshPro
            if (TextProBinder != null)
            {
                TextProBinder.text = m_previewText;
            }
            else
#endif
            if (TextBinder != null)
            {
                TextBinder.text = m_previewText;
            }

            return UITextIDBinderResultType.Success;
        }

        /// <summary>
        /// 运行时更新文本（支持参数）
        /// </summary>
        public void SetText(params object[] args)
        {
            if (m_textID == 0)
            {
                return;
            }

            string text = TextConfigMgr.Instance.GetText(m_textID, args);

#if TextMeshPro
            if (TextProBinder != null)
            {
                TextProBinder.text = text;
            }
            else
#endif
            if (TextBinder != null)
            {
                TextBinder.text = text;
            }
        }

        /// <summary>
        /// 运行时更新文本（无参数）
        /// </summary>
        public void SetText()
        {
            SetText((object[])null);
        }
    }
}