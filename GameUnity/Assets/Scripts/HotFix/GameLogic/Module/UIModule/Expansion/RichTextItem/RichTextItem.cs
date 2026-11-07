using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;
#if TextMeshPro
using TMPro;
using RichTextLabel = TMPro.TMP_Text;
#else
using RichTextLabel = GameLogic.UIText;
#endif

namespace GameLogic
{
    /// <summary>
    /// 高性能富文本组件，支持文本、图标、表情和链接
    /// 与项目的资源加载模块集成，用于加载图片
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UGUIPro/RichTextItem")]
    public class RichTextItem : MonoBehaviour, ILayoutElement
    {
        #region 序列化字段

        [Header("Text Settings")] [SerializeField]
        private Font m_font;

#if TextMeshPro
        [SerializeField] private TMP_FontAsset m_tmpFont;
#endif

        [SerializeField] private int m_fontSize = 24;
        [SerializeField] private Color m_fontColor = Color.white;
        [SerializeField] private bool m_supportRichText = true;

        [Header("Icon Settings")] [SerializeField]
        private Vector2 m_iconSize = new Vector2(24, 24);

        [SerializeField] private Vector2 m_iconOffset = Vector2.zero;
        [SerializeField] private RichTextIconAlignment m_iconAlignment = RichTextIconAlignment.Center;

        [Header("Layout Settings")] [SerializeField]
        private RichTextAlignment m_alignment = RichTextAlignment.Left;

        [SerializeField] private float m_characterSpacing = 0;
        [SerializeField] private float m_lineSpacing = 0;
        [SerializeField] private HorizontalWrapMode m_horizontalOverflow = HorizontalWrapMode.Wrap;
        [SerializeField] private VerticalWrapMode m_verticalOverflow = VerticalWrapMode.Overflow;

        [Header("Shadow Settings")] [SerializeField]
        private bool m_enableShadow = false;

        [SerializeField] private Vector2 m_shadowEffectDistance = new Vector2(1f, -1f);
        [SerializeField] private Color m_shadowTopLeftColor = Color.black;
        [SerializeField] private Color m_shadowTopRightColor = Color.black;
        [SerializeField] private Color m_shadowBottomLeftColor = Color.black;
        [SerializeField] private Color m_shadowBottomRightColor = Color.black;

        [Header("Outline Settings")] [SerializeField]
        private bool m_enableOutline = false;

        [SerializeField] private Color m_outlineColor = Color.black;
        [SerializeField, Range(1, 10)] private int m_outlineWidth = 1;

        [Header("Animation")] [SerializeField] private int m_emojiFPS = 4;

        [Header("Link Settings")] [SerializeField]
        private Color m_defaultLinkColor = new Color(0f, 0.75f, 1f); // #00BFFF

        [SerializeField] private float m_underlineHeight = 1f;

        [Header("Async Rendering")] [SerializeField]
        private bool m_enableAsyncRendering = false;

        [SerializeField] private int m_elementsPerFrame = 10;
        [SerializeField] private int m_asyncThreshold = 20;

        #endregion

        #region 私有字段

        private RectTransform m_rectTransform;
        private RichTextParams m_currentParams;
        private CancellationTokenSource m_cts;
        private string m_content;

        // 静态缓存 - 减少 GC 分配
        [ThreadStatic] private static StringBuilder s_textBuilder;
        [ThreadStatic] private static StringBuilder s_colorBuilder;

        private static StringBuilder TextBuilder => s_textBuilder ??= new StringBuilder(256);
        private static StringBuilder ColorBuilder => s_colorBuilder ??= new StringBuilder(128);

        // 缓存的参数对象，避免每次创建新实例
        private RichTextParams m_cachedParams;

#if TextMeshPro
        /// <summary>跨组件共享的 TMP 特效材质，仅在有文本持有时保留。</summary>
        private static readonly Dictionary<string, TmpEffectMaterialEntry> s_tmpEffectMaterials = new Dictionary<string, TmpEffectMaterialEntry>();

        /// <summary>当前组件中每个文本持有的材质键，避免重复计数并支持逐项释放。</summary>
        private readonly Dictionary<RichTextLabel, string> m_tmpEffectMaterialKeys = new Dictionary<RichTextLabel, string>();

        /// <summary>TMP 特效材质及其文本引用数。</summary>
        private sealed class TmpEffectMaterialEntry
        {
            public Material Material;
            public int ReferenceCount;
        }
#endif

        // 对象池
        private readonly List<RichTextLabel> m_textPool = new List<RichTextLabel>();
        private readonly List<UIImage> m_imagePool = new List<UIImage>();
        private readonly List<RichTextLabel> m_activeTexts = new List<RichTextLabel>();
        private readonly List<UIImage> m_activeImages = new List<UIImage>();
        private readonly List<UIImage> m_underlinePool = new List<UIImage>();
        private readonly List<UIImage> m_activeUnderlines = new List<UIImage>();

#if TextMeshPro
        private readonly List<MergedIconAnchor> m_mergedIconAnchors = new List<MergedIconAnchor>();
        private RichTextLabel m_mergedText;
#endif

        // 表情动画
        private readonly List<EmojiAnimationInstance> m_emojiInstances = new List<EmojiAnimationInstance>();
        private float m_emojiTimer = 0f;
        private bool m_hasEmojis = false;

        // 布局数据
        private readonly List<RichTextRow> m_rows = new List<RichTextRow>();

        // 非 TMP 特效依赖布局后的顶点，TMP 材质特效在文本排版前设置。
        private bool m_pendingEffects = false;

        private bool NeedsDeferredTextEffects
        {
            get
            {
#if TextMeshPro
                return false;
#else
                return m_currentParams.EnableShadow || m_currentParams.EnableOutline;
#endif
            }
        }

        // 链接元素 - 只存储 text，LinkData 单独管理避免重复 Dispose
        private readonly List<RichTextLabel> m_linkTexts = new List<RichTextLabel>();
        // 唯一的 LinkData 列表，避免重复 Dispose
        private readonly List<LinkData> m_linkDataList = new List<LinkData>();

        // 异步渲染状态
        private bool m_isRendering = false;

        // 图标加载上下文（避免 Lambda 闭包）
        private readonly Dictionary<Image, string> m_pendingNativeSizeIcons = new Dictionary<Image, string>();
        private readonly Dictionary<string, Vector2> m_nativeIconSizes = new Dictionary<string, Vector2>();
        private bool m_pendingIconLayoutRefresh;

#if TextMeshPro
        private struct MergedIconAnchor
        {
            public RichTextElement Element;
            public UIImage Image;
            public int TextIndex;
            public Vector2 ReservedSize;
        }
#endif

        #endregion

        #region 属性

        public RectTransform RectTransform
        {
            get
            {
                if (m_rectTransform == null)
                {
                    m_rectTransform = GetComponent<RectTransform>();
                }

                return m_rectTransform;
            }
        }

        public Vector2 Size => RectTransform.sizeDelta;
        public float Width => RectTransform.sizeDelta.x;
        public float Height => RectTransform.sizeDelta.y;
        /// <summary>已完成布局的行数；隐藏状态首次创建且尚未排版时返回 0。</summary>
        public int RowCount
        {
            get
            {
                if (m_rows.Count > 0)
                {
                    return m_rows.Count;
                }

#if TextMeshPro
                if (m_mergedText != null)
                {
                    if (m_mergedText.gameObject.activeInHierarchy && m_mergedText.havePropertiesChanged)
                    {
                        m_mergedText.ForceMeshUpdate(true);
                    }
                    return m_mergedText.textInfo?.lineCount ?? 0;
                }
#else
                if (m_nativeMergedText != null && m_nativeMergedText.gameObject.activeInHierarchy)
                {
                    return m_nativeMergedRowCount;
                }
#endif

                return 0;
            }
        }

        public float minWidth => m_layoutMinWidth;
        public float preferredWidth => m_layoutPreferredWidth;
        public float flexibleWidth => -1f;
        public float minHeight => m_layoutMinHeight;
        public float preferredHeight => m_layoutPreferredHeight;
        public float flexibleHeight => -1f;
        public int layoutPriority => 1;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public Font Font
        {
            get => m_font;
            set => m_font = value;
        }

        public int FontSize
        {
            get => m_fontSize;
            set => m_fontSize = value;
        }

        public Color FontColor
        {
            get => m_fontColor;
            set => m_fontColor = value;
        }

        public Vector2 IconSize
        {
            get => m_iconSize;
            set => m_iconSize = value;
        }

        public RichTextAlignment Alignment
        {
            get => m_alignment;
            set => m_alignment = value;
        }

        /// <summary>
        /// 链接点击回调
        /// </summary>
        public Action<LinkData> OnLinkClicked { get; set; }

        /// <summary>
        /// 渲染进度回调 (0~1)，分帧模式在每批元素完成排版后触发。
        /// </summary>
        public Action<float> OnRenderProgress { get; set; }

        /// <summary>
        /// 渲染完成回调
        /// </summary>
        public Action OnRenderComplete { get; set; }

        /// <summary>
        /// 是否正在异步渲染中
        /// </summary>
        public bool IsRendering => m_isRendering;

        #endregion

        #region 公共方法

        /// <summary>
        /// 使用 Inspector 设置更新文本内容
        /// </summary>
        public void SetText(string content)
        {
            var param = CreateParamsFromInspector();
            SetText(content, param);
        }

        /// <summary>
        /// 使用自定义参数更新文本内容
        /// 当启用异步渲染且元素数量超过阈值时，自动使用分帧渲染
        /// </summary>
        public void SetText(string content, RichTextParams param)
        {
            if (param == null)
            {
                param = CreateParamsFromInspector();
            }

            m_currentParams = param;
            m_content = content;
            m_nativeIconSizes.Clear();

            // 在布局之前保存原始尺寸（用于 Wrap/Truncate 模式）
            m_originalWidth = RectTransform.sizeDelta.x;
            m_originalHeight = RectTransform.sizeDelta.y;

            // 取消任何待处理的操作
            CancelPendingOperations();
            m_isRendering = false;
            m_cts = new CancellationTokenSource();

            // 解析元素
            var elements = RichTextParser.Parse(content, RichTextConfig.IconPrefix);

            // 检查是否应该使用异步分帧渲染
            if (m_enableAsyncRendering && elements.Count >= m_asyncThreshold)
            {
                // 捕获当前的 CancellationToken，防止后续被替换
                var token = m_cts.Token;

                // 立即标记为渲染中，防止 LateUpdate 操作正在被回收的资源
                m_isRendering = true;

                // 使用异步分帧渲染 (fire-and-forget) - 使用私有方法避免 Lambda 闭包
                RenderAsyncInternal(elements, token).Forget();
                return;
            }

            try
            {
                if (CanUseMergedText(elements))
                {
                    RenderMergedText(elements);
                }
                else
                {
                    BuildLayout(elements);
                    ApplyLayout();
                    MarkLayoutDirty();
                }

                m_pendingEffects = NeedsDeferredTextEffects;
                m_emojiTimer = 0f;
                if (m_hasEmojis)
                {
                    UpdateEmojiFrames();
                }
            }
            finally
            {
                RichTextParser.Recycle(elements);
            }
        }

        /// <summary>
        /// 设置表情动画帧率 (FPS)
        /// </summary>
        public void SetEmojiSpeed(int fps)
        {
            m_emojiFPS = Mathf.Max(1, fps);
        }

        /// <summary>
        /// 异步设置文本 (分帧渲染，适合长文本)
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="forceAsync">强制使用异步模式</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask SetTextAsync(string content, bool forceAsync = false,
            CancellationToken cancellationToken = default)
        {
            var param = CreateParamsFromInspector();
            await SetTextAsync(content, param, forceAsync, cancellationToken);
        }

        /// <summary>
        /// 异步设置文本 (分帧渲染，适合长文本)
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="param">自定义参数</param>
        /// <param name="forceAsync">强制使用异步模式</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask SetTextAsync(string content, RichTextParams param,
            bool forceAsync = false, CancellationToken cancellationToken = default)
        {
            // 取消之前的操作
            CancelPendingOperations();

            if (param == null) param = CreateParamsFromInspector();
            m_currentParams = param;
            m_content = content;
            m_nativeIconSizes.Clear();

            m_originalWidth = RectTransform.sizeDelta.x;
            m_originalHeight = RectTransform.sizeDelta.y;

            // 创建链接的 CancellationToken
            m_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // 捕获 token，防止后续被替换
            var token = m_cts.Token;

            // 解析元素
            var elements = RichTextParser.Parse(content, RichTextConfig.IconPrefix);

            // 判断是否需要分帧
            bool useAsync = forceAsync ||
                            (m_enableAsyncRendering && elements.Count >= m_asyncThreshold);

            // 标记为渲染中，防止 LateUpdate 操作正在被回收的资源
            m_isRendering = true;

            try
            {
                token.ThrowIfCancellationRequested();
                if (useAsync)
                {
                    await RenderLayoutAsync(elements, token);
                }
                else if (CanUseMergedText(elements))
                {
                    RenderMergedText(elements);
                }
                else
                {
                    // 同步渲染
                    BuildLayout(elements);
                }

                // 检查取消
                token.ThrowIfCancellationRequested();

                ApplyLayout();
                MarkLayoutDirty();
                FinalizeRendering(token);
            }
            finally
            {
                RichTextParser.Recycle(elements);
                if (m_cts != null && m_cts.Token == token)
                {
                    m_isRendering = false;
                }
            }
        }

        /// <summary>
        /// 取消当前渲染
        /// </summary>
        public void CancelRendering()
        {
            CancelPendingOperations();
            m_isRendering = false;
        }

        /// <summary>
        /// 清除所有内容
        /// </summary>
        public void Clear()
        {
            CancelPendingOperations();
            m_content = null;
            m_nativeIconSizes.Clear();
            m_isRendering = false;
            ClearLinkElements();
            RecycleAllElements();
            ClearRows();
            RecycleEmojiInstances();
            m_hasEmojis = false;
            m_pendingEffects = false;
            UpdateLayoutMetrics(0f, 0f);
            MarkLayoutDirty();
        }

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
            UpdateLayoutMetrics(m_rectTransform.sizeDelta.x, m_rectTransform.sizeDelta.y);
        }

        private void LateUpdate()
        {
            if (m_pendingIconLayoutRefresh && !m_isRendering)
            {
                m_pendingIconLayoutRefresh = false;
                RefreshIconLayout();
            }

            // 在渲染完成后应用延迟的文本特效（阴影/描边）
            if (m_pendingEffects)
            {
                m_pendingEffects = false;
                ApplyAllTextEffects();
            }

            // 渲染中不更新表情动画，防止操作正在被回收的资源
            if (m_isRendering)
                return;

            // 表情动画 (使用组件的 destroyCancellationToken，不依赖 m_cts)
            if (!m_hasEmojis || m_emojiInstances.Count == 0)
                return;

            float frameInterval = 1f / m_emojiFPS;
            m_emojiTimer += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime，不受时间缩放影响

            if (m_emojiTimer >= frameInterval)
            {
                m_emojiTimer -= frameInterval;
                UpdateEmojiFramesForAnimation();
            }
        }

        private void OnDestroy()
        {
            CancelPendingOperations();

#if TextMeshPro
            ReleaseAllTmpEffectMaterials();
#endif

            // 清理 Action 回调，防止外部对象通过委托持有引用
            OnLinkClicked = null;
            OnRenderProgress = null;
            OnRenderComplete = null;

            // 清理链接事件
            foreach (var text in m_linkTexts)
            {
                if (text != null)
                {
                    var button = text.GetComponent<UIButton>();
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                    }

                    // 清理 LinkClickHandler
                    var clickHandler = text.GetComponent<LinkClickHandler>();
                    if (clickHandler != null)
                    {
                        clickHandler.Clear();
                    }
                }
            }

            m_linkTexts.Clear();

            // 回收 LinkData（每个只 Dispose 一次）
            foreach (var data in m_linkDataList)
            {
                data?.Dispose();
            }

            m_linkDataList.Clear();

            // 回收表情实例到对象池（不涉及 GameObject 操作）
            foreach (var instance in m_emojiInstances)
            {
                instance.Dispose();
            }

            m_emojiInstances.Clear();

            // 清理行数据
            foreach (var row in m_rows)
            {
                row.Dispose();
            }

            m_rows.Clear();

            // 销毁所有活动元素（OnDestroy 时不需要回收，直接清理列表即可，GameObject 会随父对象销毁）
            m_activeTexts.Clear();
            m_activeImages.Clear();
            m_activeUnderlines.Clear();

            // 销毁对象池中的对象（这些是隐藏的，需要手动销毁）
            foreach (var text in m_textPool)
            {
                if (text != null) Destroy(text.gameObject);
            }

            foreach (var image in m_imagePool)
            {
                if (image != null) Destroy(image.gameObject);
            }

            foreach (var underline in m_underlinePool)
            {
                if (underline != null) Destroy(underline.gameObject);
            }

            m_textPool.Clear();
            m_imagePool.Clear();
            m_underlinePool.Clear();

            m_hasEmojis = false;
            m_pendingEffects = false;
        }

        #endregion

        #region 布局构建

        private float m_layoutWidth; // Wrap 模式下的固定布局宽度
        private float m_layoutHeight; // Truncate 模式下的固定布局高度
        private float m_originalWidth; // 布局前的原始 RectTransform 宽度
        private float m_originalHeight; // 布局前的原始 RectTransform 高度

        private float m_layoutMinWidth;
        private float m_layoutPreferredWidth;
        private float m_layoutMinHeight;
        private float m_layoutPreferredHeight;

#if TextMeshPro
        /// <summary>
        /// 判断是否可以使用单 TMP 渲染。
        /// 静态图标使用 TMP 文本占位并保留 UIImage 覆盖层；表情和链接继续使用旧路径。
        /// </summary>
        private bool CanUseMergedText(List<RichTextElement> elements)
        {
            if (elements == null || elements.Count == 0)
            {
                return true;
            }

            // 单图标、单行文本原本就只有一个 TMP，沿用原布局可省去占位解析和强制网格更新。
            // 需要换行或截断时仍交给合并路径，保持换行边界与可见行语义。
            if (elements.Count == 2 && elements[0].Type == RichTextElementType.Icon &&
                elements[1].Type == RichTextElementType.Text &&
                m_currentParams.VerticalOverflow == VerticalWrapMode.Overflow)
            {
                var text = elements[1];
                var length = text.Content != null ? text.Content.Length : text.TextBuilder.Length;
                var isSingleLine = length > 0;
                var width = m_currentParams.IconSize.x;
                for (var i = 0; i < length; i++)
                {
                    var character = text.Content != null ? text.Content[i] : text.TextBuilder[i];
                    // TMP 标签和代理对由 TMP 解释，不能使用逐 char 的旧宽度估算。
                    if (character == '\n' || character == '\r' || character == '<' || char.IsSurrogate(character))
                    {
                        isSingleLine = false;
                        break;
                    }

                    width += GetCharacterAdvance(character) + m_currentParams.CharacterSpacing;
                }

                if (isSingleLine && (m_currentParams.HorizontalOverflow == HorizontalWrapMode.Overflow ||
                                    m_originalWidth <= 0 || width <= m_originalWidth))
                {
                    return false;
                }
            }

            var iconLineHeight = 0f;
            var hasText = false;
            var hasIcon = false;
            for (var index = 0; index < elements.Count; index++)
            {
                var type = elements[index].Type;
                if (type == RichTextElementType.Emoji || type == RichTextElementType.Link)
                {
                    return false;
                }

                if (type == RichTextElementType.Icon)
                {
                    hasIcon = true;
                    // 原图尺寸图标需要等待资源后重新参与换行，继续使用旧布局路径。
                    if (m_currentParams.IconSize == Vector2.zero || !m_currentParams.SupportRichText)
                    {
                        return false;
                    }

                    if (iconLineHeight == 0f)
                    {
                        var font = m_tmpFont != null ? m_tmpFont : TMP_Settings.defaultFontAsset;
                        var face = font.faceInfo;
                        var scale = (float)m_currentParams.FontSize / face.pointSize * face.scale;
                        var textHeight = (face.ascentLine - face.descentLine) * scale;
                        var lineAdvance = face.lineHeight * scale +
                                          m_currentParams.LineSpacing * m_currentParams.FontSize * 0.01f;
                        iconLineHeight = Mathf.Min(textHeight, lineAdvance);
                    }

                    // space 不提供垂直度量；大图标由原行布局计算高度、行间距和截断预算。
                    if (m_currentParams.IconSize.y > iconLineHeight)
                    {
                        return false;
                    }
                }
                else if (type == RichTextElementType.Text && !hasText)
                {
                    var text = elements[index];
                    var length = text.Content != null ? text.Content.Length : text.TextBuilder.Length;
                    for (var i = 0; i < length; i++)
                    {
                        if (!char.IsWhiteSpace(text.Content != null ? text.Content[i] : text.TextBuilder[i]))
                        {
                            hasText = true;
                            break;
                        }
                    }
                }
            }

            // 纯图标内容无需创建 TMP，沿用图标自身的行高。
            return !hasIcon || hasText;
        }

        /// <summary>
        /// 使用一个 TMP 合并渲染普通文本和颜色片段。
        /// </summary>
        private void RenderMergedText(List<RichTextElement> elements)
        {
            if (!PrepareMergedText(elements)) return;

            for (var index = 0; index < m_mergedIconAnchors.Count; index++)
            {
                CreateMergedIcon(index);
            }

            RefreshMergedTextLayout();
        }

        private async UniTask RenderMergedTextAsync(List<RichTextElement> elements, CancellationToken token)
        {
            if (elements.Count == 0)
            {
                RenderMergedText(elements);
                return;
            }

            PrepareMergedText(null, true);
            // 每次渲染独占拼接器，进度回调和跨帧期间不使用共享 StringBuilder。
            var builder = new StringBuilder(256);
            string iconFontSize = null;
            var batchSize = Mathf.Max(1, m_elementsPerFrame);
            for (var index = 0; index < elements.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                AppendMergedElement(builder, elements[index], ref iconFontSize);
                if (elements[index].Type == RichTextElementType.Icon)
                {
                    CreateMergedIcon(m_mergedIconAnchors.Count - 1);
                }

                var processed = index + 1;
                var endOfBatch = processed % batchSize == 0 || processed == elements.Count;
                if (endOfBatch)
                {
                    m_mergedText.text = builder.ToString();
                    RefreshMergedTextLayout();
                    OnRenderProgress?.Invoke((float)processed / elements.Count);
                    token.ThrowIfCancellationRequested();
                }

                if (endOfBatch && processed < elements.Count)
                {
                    await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, token);
                }
            }
        }

        private bool PrepareMergedText(List<RichTextElement> elements, bool allowEmpty = false)
        {
            ClearLinkElements();
            RecycleAllElements();
            ClearRows();
            RecycleEmojiInstances();
            m_hasEmojis = false;
            m_mergedIconAnchors.Clear();
            m_mergedText = null;

            var isWrapMode = m_currentParams.HorizontalOverflow == HorizontalWrapMode.Wrap;
            var isTruncateMode = m_currentParams.VerticalOverflow == VerticalWrapMode.Truncate;
            m_layoutWidth = isWrapMode && m_originalWidth > 0 ? m_originalWidth : 0f;
            m_layoutHeight = isTruncateMode && m_originalHeight > 0 ? m_originalHeight : 0f;

            var content = BuildMergedText(elements);
            if (string.IsNullOrEmpty(content) && !allowEmpty)
            {
                UpdateLayoutMetrics(m_layoutWidth > 0 ? m_layoutWidth : 0f, 0f);
                MarkLayoutDirty();
                return false;
            }

            var label = GetOrCreateText();
            SetupTextCommon(label);

            label.alignment = m_currentParams.Alignment switch
            {
                RichTextAlignment.Center => TextAlignmentOptions.Top,
                RichTextAlignment.Right => TextAlignmentOptions.TopRight,
                _ => TextAlignmentOptions.TopLeft
            };
            label.enableWordWrapping = m_layoutWidth > 0;
            // 按实际行信息统一截断文字与图标，避免 TMP 在高度不足一行时裁空整个文本。
            label.overflowMode = TextOverflowModes.Overflow;
            label.text = content;

            SetupRectTransform(label.rectTransform);
            label.rectTransform.anchoredPosition = Vector2.zero;
            m_activeTexts.Add(label);
            m_mergedText = label;

            return true;
        }

        private void CreateMergedIcon(int index)
        {
            var anchor = m_mergedIconAnchors[index];
            var image = GetOrCreateImage();
            SetupImageCommon(image, anchor.Element.RaycastEnabled);
            image.rectTransform.sizeDelta = anchor.ReservedSize;
            anchor.Image = image;
            m_mergedIconAnchors[index] = anchor;
            m_activeImages.Add(image);
            RichTextConfig.SetSprite(
                image,
                anchor.Element.FormatData,
                false,
                null,
                m_cts?.Token ?? default);
        }

        private void RefreshMergedTextLayout()
        {
            var label = m_mergedText;
            if (!label.gameObject.activeInHierarchy)
            {
                // 首次隐藏创建时 TMP 可能尚未 Awake，尺寸、截断和图标都等启用后的 LateUpdate 更新。
                m_pendingIconLayoutRefresh = true;
                return;
            }

            label.maxVisibleLines = int.MaxValue;
            var preferred = m_layoutWidth > 0
                ? label.GetPreferredValues(label.text, m_layoutWidth, 0f)
                : label.GetPreferredValues(label.text);
            var finalWidth = m_layoutWidth > 0
                ? m_layoutWidth
                : preferred.x;
            var finalHeight = Mathf.Max(
                m_currentParams.FontSize + m_currentParams.LineSpacing,
                preferred.y);
            label.rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);

            // GetPreferredValues 的预测换行可能与实际 TMP 排版不同（例如彩色中文和图标混排）。
            // 用实际行度量更新布局尺寸，确保 ScrollRect 能滚到最后一行 / 最右端。
            label.ForceMeshUpdate(true);
            var textInfo = label.textInfo;
            if (textInfo.lineCount > 0)
            {
                var lines = textInfo.lineInfo;
                var firstLineTop = lines[0].ascender;
                var actualWidth = 0f;
                var actualHeight = 0f;
                for (var index = 0; index < textInfo.lineCount; index++)
                {
                    var line = lines[index];
                    if (line.characterCount > 0)
                    {
                        // lineInfo.maxAdvance 会混入水平对齐偏移，使用同一行字符的相对推进量。
                        var firstCharacter = textInfo.characterInfo[line.firstCharacterIndex];
                        var lastCharacter = textInfo.characterInfo[line.lastCharacterIndex];
                        actualWidth = Mathf.Max(actualWidth, lastCharacter.xAdvance - firstCharacter.origin);
                    }
                    actualHeight = Mathf.Max(actualHeight, firstLineTop - lines[index].descender);
                }

                if (m_layoutWidth <= 0)
                {
                    finalWidth = Mathf.Ceil(actualWidth * 100f) / 100f;
                }
                finalHeight = Mathf.Max(m_currentParams.FontSize + m_currentParams.LineSpacing,
                    Mathf.Ceil(actualHeight * 100f) / 100f);

                if (m_layoutHeight > 0)
                {
                    var heightLimit = Mathf.Max(m_layoutHeight, firstLineTop - lines[0].descender);
                    var visibleLineCount = 1;
                    while (visibleLineCount < textInfo.lineCount &&
                           firstLineTop - lines[visibleLineCount].descender <= heightLimit + 0.001f)
                    {
                        visibleLineCount++;
                    }

                    label.maxVisibleLines = visibleLineCount;
                    finalHeight = Mathf.Min(heightLimit, finalHeight);
                }
            }

            var widthChanged = !Mathf.Approximately(label.rectTransform.sizeDelta.x, finalWidth);
            label.rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);
            if (m_mergedIconAnchors.Count > 0)
            {
                // 无约束宽度发生修正时，居中 / 右对齐的图标要使用新宽度下的字符位置。
                if (widthChanged)
                {
                    label.ForceMeshUpdate(true);
                }
                ApplyMergedIconLayout();
            }
            UpdateLayoutMetrics(finalWidth, finalHeight);
            MarkLayoutDirty();
        }

        private string BuildMergedText(List<RichTextElement> elements)
        {
            if (elements != null && elements.Count == 1 &&
                elements[0] != null && elements[0].Type == RichTextElementType.Text &&
                string.IsNullOrEmpty(elements[0].FormatData))
            {
                return elements[0].GetText() ?? string.Empty;
            }

            var builder = TextBuilder;
            builder.Clear();

            if (elements == null)
            {
                return string.Empty;
            }

            string iconFontSize = null;
            for (var index = 0; index < elements.Count; index++)
            {
                AppendMergedElement(builder, elements[index], ref iconFontSize);
            }

            return builder.ToString();
        }

        private void AppendMergedElement(StringBuilder builder, RichTextElement element, ref string iconFontSize)
        {
            if (element.Type == RichTextElementType.Icon)
            {
                var reservedSize = m_currentParams.IconSize;
                if (iconFontSize == null)
                {
                    var font = m_tmpFont != null ? m_tmpFont : TMP_Settings.defaultFontAsset;
                    var face = font.faceInfo;
                    iconFontSize = (reservedSize.y * face.pointSize /
                                   ((face.ascentLine - face.descentLine) * face.scale))
                        .ToString("R", CultureInfo.InvariantCulture);
                }

                // 图标占位同时提供水平推进量、断行点和真实宽度。
                builder.Append("<size=");
                builder.Append(iconFontSize);
                builder.Append('>');
                builder.Append('\u200B');
                builder.Append("<space=");
                builder.Append(Mathf.Max(1, Mathf.CeilToInt(reservedSize.x)));
                builder.Append('>');
                m_mergedIconAnchors.Add(new MergedIconAnchor
                {
                    Element = element,
                    TextIndex = builder.Length,
                    ReservedSize = reservedSize
                });
                builder.Append('\u2060');
                builder.Append('\u200B');
                builder.Append("</size>");
                return;
            }

            var text = element.GetText();
            if (string.IsNullOrEmpty(text)) return;

            if (!string.IsNullOrEmpty(element.FormatData))
            {
                builder.Append("<color=");
                builder.Append(element.FormatData);
                builder.Append('>');
                builder.Append(text);
                builder.Append("</color>");
            }
            else
            {
                builder.Append(text);
            }
        }

        private void ApplyMergedIconLayout()
        {
            var textInfo = m_mergedText.textInfo;
            for (var index = 0; index < m_mergedIconAnchors.Count; index++)
            {
                var image = m_mergedIconAnchors[index].Image;
                if (image != null)
                {
                    image.gameObject.SetActive(false);
                    image.rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            var anchorIndex = 0;
            for (var characterIndex = 0;
                 characterIndex < textInfo.characterCount && anchorIndex < m_mergedIconAnchors.Count;
                 characterIndex++)
            {
                var character = textInfo.characterInfo[characterIndex];
                while (anchorIndex < m_mergedIconAnchors.Count &&
                       m_mergedIconAnchors[anchorIndex].TextIndex < character.index)
                {
                    anchorIndex++;
                }

                if (anchorIndex >= m_mergedIconAnchors.Count ||
                    m_mergedIconAnchors[anchorIndex].TextIndex != character.index)
                {
                    continue;
                }

                var anchor = m_mergedIconAnchors[anchorIndex++];
                if (anchor.Image == null)
                {
                    continue;
                }

                var lineIndex = character.lineNumber;
                var lineVisible = lineIndex >= 0 && lineIndex < textInfo.lineCount &&
                                  lineIndex < m_mergedText.maxVisibleLines;

                if (!lineVisible)
                {
                    continue;
                }

                var iconSize = anchor.Image.rectTransform.sizeDelta;
                // 零宽占位字符自身的字形高度接近 0，使用它的 topLeft/bottomLeft
                // 会让图标漂到基线附近。使用 TMP 当前行的 ascender/descender 才能得到真实行高。
                var lineTop = textInfo.lineInfo[lineIndex].ascender;
                var lineBottom = textInfo.lineInfo[lineIndex].descender;
                var lineHeight = Mathf.Abs(lineTop - lineBottom);
                if (lineHeight <= 0f)
                {
                    lineTop = character.topLeft.y;
                    lineBottom = character.bottomLeft.y;
                    lineHeight = Mathf.Max(Mathf.Abs(lineTop - lineBottom), m_currentParams.FontSize);
                }

                var y = lineTop;
                switch (m_currentParams.IconAlignment)
                {
                    case RichTextIconAlignment.Center:
                        y -= (lineHeight - iconSize.y) * 0.5f;
                        break;
                    case RichTextIconAlignment.Bottom:
                        y = lineBottom + iconSize.y;
                        break;
                }

                anchor.Image.rectTransform.anchoredPosition =
                    new Vector2(character.origin - Mathf.Max(1, Mathf.CeilToInt(anchor.ReservedSize.x)), y) + m_iconOffset;
                anchor.Image.gameObject.SetActive(true);
            }
        }

#else
        private RichTextLabel m_nativeMergedText;
        private int m_nativeMergedRowCount;

        /// <summary>
        /// 原生 Text 合并普通文本和颜色片段。
        /// 图标、表情、链接和非零字间距继续使用元素布局。
        /// </summary>
        private bool CanUseMergedText(List<RichTextElement> elements)
        {
            if (elements == null || elements.Count == 0)
            {
                return true;
            }

            if (m_currentParams.CharacterSpacing != 0)
            {
                return false;
            }

            for (var index = 0; index < elements.Count; index++)
            {
                if (elements[index] == null || elements[index].Type != RichTextElementType.Text)
                {
                    return false;
                }
            }

            return true;
        }

        private void RenderMergedText(List<RichTextElement> elements)
        {
            if (!PrepareNativeMergedText(elements)) return;
            RefreshNativeMergedTextLayout();
        }

        private async UniTask RenderMergedTextAsync(List<RichTextElement> elements, CancellationToken token)
        {
            if (elements == null || elements.Count == 0)
            {
                RenderMergedText(elements);
                return;
            }

            PrepareNativeMergedText(null, true);
            var builder = new StringBuilder(256);
            var batchSize = Mathf.Max(1, m_elementsPerFrame);
            for (var index = 0; index < elements.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                AppendNativeMergedElement(builder, elements[index]);

                var processed = index + 1;
                var endOfBatch = processed % batchSize == 0 || processed == elements.Count;
                if (endOfBatch)
                {
                    m_nativeMergedText.text = builder.ToString();
                    RefreshNativeMergedTextLayout();
                    OnRenderProgress?.Invoke((float)processed / elements.Count);
                    token.ThrowIfCancellationRequested();
                }

                if (endOfBatch && processed < elements.Count)
                {
                    await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, token);
                }
            }
        }

        private bool PrepareNativeMergedText(List<RichTextElement> elements, bool allowEmpty = false)
        {
            ClearLinkElements();
            RecycleAllElements();
            ClearRows();
            RecycleEmojiInstances();
            m_hasEmojis = false;
            m_nativeMergedText = null;
            m_nativeMergedRowCount = 0;

            var isWrapMode = m_currentParams.HorizontalOverflow == HorizontalWrapMode.Wrap;
            var isTruncateMode = m_currentParams.VerticalOverflow == VerticalWrapMode.Truncate;
            m_layoutWidth = isWrapMode && m_originalWidth > 0 ? m_originalWidth : 0f;
            m_layoutHeight = isTruncateMode && m_originalHeight > 0 ? m_originalHeight : 0f;

            var content = BuildNativeMergedText(elements);
            if (string.IsNullOrEmpty(content) && !allowEmpty)
            {
                UpdateLayoutMetrics(m_layoutWidth > 0 ? m_layoutWidth : 0f, 0f);
                MarkLayoutDirty();
                return false;
            }

            var label = GetOrCreateText();
            SetupTextCommon(label);
            label.alignment = m_currentParams.Alignment switch
            {
                RichTextAlignment.Center => TextAnchor.UpperCenter,
                RichTextAlignment.Right => TextAnchor.UpperRight,
                _ => TextAnchor.UpperLeft
            };
            label.horizontalOverflow = m_layoutWidth > 0
                ? HorizontalWrapMode.Wrap
                : HorizontalWrapMode.Overflow;
            label.verticalOverflow = m_layoutHeight > 0
                ? VerticalWrapMode.Truncate
                : VerticalWrapMode.Overflow;
            // Text.lineSpacing 是倍率，RichTextParams.LineSpacing 是额外像素间隔。
            var lineHeight = label.font != null && label.font.fontSize > 0
                ? (float)label.font.lineHeight * m_currentParams.FontSize / label.font.fontSize
                : m_currentParams.FontSize;
            label.lineSpacing = lineHeight > 0 ? 1f + m_currentParams.LineSpacing / lineHeight : 1f;
            label.text = content;
            SetupRectTransform(label.rectTransform);
            label.rectTransform.anchoredPosition = Vector2.zero;
            m_activeTexts.Add(label);
            m_nativeMergedText = label;
            return true;
        }

        private string BuildNativeMergedText(List<RichTextElement> elements)
        {
            var builder = TextBuilder;
            builder.Clear();
            if (elements == null) return string.Empty;

            for (var index = 0; index < elements.Count; index++)
            {
                AppendNativeMergedElement(builder, elements[index]);
            }
            return builder.ToString();
        }

        private void AppendNativeMergedElement(StringBuilder builder, RichTextElement element)
        {
            if (element == null || element.Type != RichTextElementType.Text) return;
            var text = element.GetText();
            if (string.IsNullOrEmpty(text)) return;

            if (string.IsNullOrEmpty(element.FormatData))
            {
                builder.Append(text);
            }
            else
            {
                builder.Append("<color=");
                builder.Append(element.FormatData);
                builder.Append('>');
                builder.Append(text);
                builder.Append("</color>");
            }
        }

        private void RefreshNativeMergedTextLayout()
        {
            var label = m_nativeMergedText;
            if (label == null) return;
            if (!label.gameObject.activeInHierarchy)
            {
                m_pendingIconLayoutRefresh = true;
                return;
            }

            var finalWidth = m_layoutWidth > 0 ? m_layoutWidth : label.preferredWidth;
            label.rectTransform.sizeDelta = new Vector2(finalWidth, m_currentParams.FontSize);
            var finalHeight = Mathf.Max(m_currentParams.FontSize + m_currentParams.LineSpacing,
                Mathf.Ceil(label.preferredHeight));
            m_nativeMergedRowCount = Mathf.Max(1, CountNativeTextRows(label.text, finalWidth));

            if (m_layoutHeight > 0)
            {
                finalHeight = Mathf.Min(
                    Mathf.Max(m_layoutHeight, m_currentParams.FontSize + m_currentParams.LineSpacing),
                    finalHeight);
            }

            label.rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);
            UpdateLayoutMetrics(finalWidth, finalHeight);
            if (NeedsDeferredTextEffects) ApplyTextEffects(label);
            MarkLayoutDirty();
        }

        private int CountNativeTextRows(string text, float width)
        {
            // 交给 TextGenerator 处理颜色标签、字号、单词换行和代理对，不逐字符估算。
            var settings = m_nativeMergedText.GetGenerationSettings(new Vector2(width, 0f));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var generator = m_nativeMergedText.cachedTextGeneratorForLayout;
            generator.Populate(text, settings);
            return generator.lineCount;
        }

#endif

        private UniTask RenderLayoutAsync(List<RichTextElement> elements, CancellationToken token)
        {
            if (CanUseMergedText(elements))
            {
                return RenderMergedTextAsync(elements, token);
            }
            return BuildLayoutAsync(elements, token);
        }

        private void BuildLayout(List<RichTextElement> elements)
        {
            ClearLinkElements();
            RecycleAllElements();
            ClearRows();
            RecycleEmojiInstances();
            m_hasEmojis = false;

            // 根据溢出设置使用原始尺寸
            bool isWrapMode = m_currentParams.HorizontalOverflow == HorizontalWrapMode.Wrap;
            bool isTruncateMode = m_currentParams.VerticalOverflow == VerticalWrapMode.Truncate;

            // HorizontalOverflow.Wrap: 使用原始宽度作为约束
            // HorizontalOverflow.Overflow: 无宽度约束，随内容扩展
            m_layoutWidth = isWrapMode && m_originalWidth > 0 ? m_originalWidth : 0;

            // VerticalOverflow.Truncate: 使用原始高度作为约束
            // VerticalOverflow.Overflow: 无高度约束，随内容扩展
            m_layoutHeight = isTruncateMode && m_originalHeight > 0 ? m_originalHeight : 0;

            if (elements == null || elements.Count == 0)
            {
                UpdateLayoutMetrics(m_layoutWidth > 0 ? m_layoutWidth : 0f, 0f);
                return;
            }

            RichTextRow currentRow = RichTextRow.Create();
            m_rows.Add(currentRow);

            foreach (var element in elements)
            {
                switch (element.Type)
                {
                    case RichTextElementType.Text:
                        ProcessTextElement(element, ref currentRow);
                        break;

                    case RichTextElementType.Icon:
                        ProcessIconElement(element, ref currentRow);
                        break;

                    case RichTextElementType.Emoji:
                        ProcessEmojiElement(element, ref currentRow);
                        break;

                    case RichTextElementType.Link:
                        ProcessLinkElement(element, ref currentRow);
                        break;
                }
            }

            CalculateTotalSize();
        }

        /// <summary>
        /// 异步分帧构建布局
        /// 注意：调用方需要在调用前设置 m_isRendering = true，并在完成后设置为 false
        /// </summary>
        private async UniTask BuildLayoutAsync(List<RichTextElement> elements, CancellationToken token)
        {
            // 初始化
            ClearLinkElements();
            RecycleAllElements();
            ClearRows();
            RecycleEmojiInstances();
            m_hasEmojis = false;

            bool isWrapMode = m_currentParams.HorizontalOverflow == HorizontalWrapMode.Wrap;
            bool isTruncateMode = m_currentParams.VerticalOverflow == VerticalWrapMode.Truncate;
            m_layoutWidth = isWrapMode && m_originalWidth > 0 ? m_originalWidth : 0;
            m_layoutHeight = isTruncateMode && m_originalHeight > 0 ? m_originalHeight : 0;

            if (elements == null || elements.Count == 0)
            {
                UpdateLayoutMetrics(m_layoutWidth > 0 ? m_layoutWidth : 0f, 0f);
                return;
            }

            RichTextRow currentRow = RichTextRow.Create();
            m_rows.Add(currentRow);

            int processedCount = 0;
            int totalCount = elements.Count;
            var batchSize = Mathf.Max(1, m_elementsPerFrame);

            // 分帧处理元素
            foreach (var element in elements)
            {
                token.ThrowIfCancellationRequested();

                switch (element.Type)
                {
                    case RichTextElementType.Text:
                        ProcessTextElement(element, ref currentRow);
                        break;

                    case RichTextElementType.Icon:
                        ProcessIconElement(element, ref currentRow);
                        break;

                    case RichTextElementType.Emoji:
                        ProcessEmojiElement(element, ref currentRow);
                        break;

                    case RichTextElementType.Link:
                        ProcessLinkElement(element, ref currentRow);
                        break;
                }

                processedCount++;
                var endOfBatch = processedCount % batchSize == 0 || processedCount == totalCount;
                if (endOfBatch)
                {
                    // 已创建的节点在交出本帧前排好位置，取消后保留的部分内容也不会堆叠。
                    CalculateTotalSize();
                    ApplyLayout();
                    MarkLayoutDirty();
                    if (NeedsDeferredTextEffects) ApplyAllTextEffects();
                    OnRenderProgress?.Invoke((float)processedCount / totalCount);
                    token.ThrowIfCancellationRequested();
                }

                // 每处理 N 个元素暂停一帧 (使用 PreLateUpdate 不受 timeScale 影响)
                if (endOfBatch && processedCount < totalCount)
                {
                    await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, token);
                }
            }
        }

        /// <summary>
        /// 完成渲染的收尾工作 (带取消令牌)
        /// </summary>
        private void FinalizeRendering(CancellationToken token)
        {
            m_pendingEffects = NeedsDeferredTextEffects;

            // 如果有表情且未取消则启动动画
            m_emojiTimer = 0f;

            if (m_hasEmojis && !token.IsCancellationRequested)
            {
                UpdateEmojiFrames(token);
            }

            // 回调可能开始新一轮渲染，取消后不再发送旧任务的完成事件。
            OnRenderProgress?.Invoke(1f);
            token.ThrowIfCancellationRequested();
            OnRenderComplete?.Invoke();
        }

        /// <summary>
        /// 内部异步渲染方法（避免 Lambda 闭包）
        /// </summary>
        private async UniTaskVoid RenderAsyncInternal(List<RichTextElement> elements, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await RenderLayoutAsync(elements, token);

                token.ThrowIfCancellationRequested();
                ApplyLayout();
                MarkLayoutDirty();
                FinalizeRendering(token);
            }
            finally
            {
                RichTextParser.Recycle(elements);
                if (m_cts != null && m_cts.Token == token)
                {
                    m_isRendering = false;
                }
            }
        }

        private float GetCharacterAdvance(char character)
        {
#if TextMeshPro
            var font = m_tmpFont != null ? m_tmpFont : TMP_Settings.defaultFontAsset;
            if (font != null && font.characterLookupTable.TryGetValue(character, out var tmpCharacter)
                && tmpCharacter.glyph != null && font.faceInfo.pointSize > 0)
            {
                return tmpCharacter.glyph.metrics.horizontalAdvance
                    * m_currentParams.FontSize / font.faceInfo.pointSize;
            }
#endif

            if (m_font != null && m_font.GetCharacterInfo(character, out CharacterInfo info, m_currentParams.FontSize))
            {
                return info.advance;
            }

            return m_currentParams.FontSize;
        }

        private void ProcessTextElement(RichTextElement element, ref RichTextRow currentRow)
        {
            string text = element.GetText();
            if (string.IsNullOrEmpty(text)) return;

            m_font?.RequestCharactersInTexture(text, m_currentParams.FontSize);

            // 使用静态缓存的 StringBuilder，避免每次调用都分配
            var sb = TextBuilder;
            sb.Clear();

            float currentWidth = 0;
            bool shouldWrap = m_layoutWidth > 0; // 使用 m_layoutWidth 决定是否换行
            float charSpacing = m_currentParams.CharacterSpacing;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // 处理换行符
                if (c == '\n')
                {
                    if (sb.Length > 0)
                    {
                        CreateTextLabel(sb.ToString(), element.FormatData, currentWidth, ref currentRow);
                        sb.Clear();
                        currentWidth = 0;
                    }

                    currentRow = RichTextRow.Create();
                    m_rows.Add(currentRow);
                    continue;
                }

                // 获取字符宽度（包含字间距）
                float charWidth = GetCharacterAdvance(c) + charSpacing;

                // 检查是否需要换行（仅当 m_layoutWidth > 0 时）
                if (shouldWrap && currentRow.Width + currentWidth + charWidth > m_layoutWidth)
                {
                    if (sb.Length > 0)
                    {
                        CreateTextLabel(sb.ToString(), element.FormatData, currentWidth, ref currentRow);
                        sb.Clear();
                        currentWidth = 0;
                    }

                    currentRow = RichTextRow.Create();
                    m_rows.Add(currentRow);
                }

                sb.Append(c);
                currentWidth += charWidth;
            }

            // 刷新剩余文本
            if (sb.Length > 0)
            {
                CreateTextLabel(sb.ToString(), element.FormatData, currentWidth, ref currentRow);
            }
        }

        private void ProcessIconElement(RichTextElement element, ref RichTextRow currentRow)
        {
            var image = GetOrCreateImage();
            SetupImageCommon(image, element.RaycastEnabled);

            Vector2 size = m_currentParams.IconSize;
            bool useNativeSize = size == Vector2.zero;
            if (useNativeSize) m_nativeIconSizes.TryGetValue(element.FormatData, out size);
            image.rectTransform.sizeDelta = size;

            // 检查是否需要换行（仅当 m_layoutWidth > 0 时）
            if (m_layoutWidth > 0 && currentRow.Width + image.rectTransform.sizeDelta.x > m_layoutWidth)
            {
                currentRow = RichTextRow.Create();
                m_rows.Add(currentRow);
            }

            var layoutElement = RichTextLayoutElement.Create(RichTextElementType.Icon, image.rectTransform);
            currentRow.AddElement(layoutElement);
            m_activeImages.Add(image);

            if (useNativeSize)
            {
                m_pendingNativeSizeIcons[image] = element.FormatData;
            }

            RichTextConfig.SetSprite(image, element.FormatData, useNativeSize,
                useNativeSize ? OnIconSpriteLoaded : null, m_cts?.Token ?? default);
        }

        /// <summary>
        /// 图标精灵加载完成回调（避免 Lambda 闭包）
        /// </summary>
        private void OnIconSpriteLoaded(Image img)
        {
            if (img == null || img.sprite == null) return;

            if (m_pendingNativeSizeIcons.TryGetValue(img, out var spriteName))
            {
                m_pendingNativeSizeIcons.Remove(img);
                var size = img.rectTransform.sizeDelta;
                if (!m_nativeIconSizes.TryGetValue(spriteName, out var previous) || previous != size)
                {
                    m_nativeIconSizes[spriteName] = size;
                    m_pendingIconLayoutRefresh = true;
                }
            }
        }

        private void ProcessEmojiElement(RichTextElement element, ref RichTextRow currentRow)
        {
            var emojiData = RichTextConfig.GetEmojiData(element.FormatData);
            if (emojiData == null) return;

            var image = GetOrCreateImage();
            SetupImageCommon(image, emojiData.RaycastEnabled);

            Vector2 size = m_currentParams.IconSize;
            if (size == Vector2.zero)
            {
                var firstFrame = emojiData.GetFrame(0);
                if (!string.IsNullOrEmpty(firstFrame))
                {
                    m_nativeIconSizes.TryGetValue(firstFrame, out size);
                    if (size == Vector2.zero) m_pendingNativeSizeIcons[image] = firstFrame;
                }
            }
            image.rectTransform.sizeDelta = size;

            // 使用对象池创建表情动画实例
            var animInstance = EmojiAnimationInstance.Create(image, emojiData);
            m_emojiInstances.Add(animInstance);
            m_hasEmojis = true;

            // 检查是否需要换行（仅当 m_layoutWidth > 0 时）
            if (m_layoutWidth > 0 && currentRow.Width + size.x > m_layoutWidth)
            {
                currentRow = RichTextRow.Create();
                m_rows.Add(currentRow);
            }

            var layoutElement = RichTextLayoutElement.Create(RichTextElementType.Emoji, image.rectTransform);
            currentRow.AddElement(layoutElement);
            m_activeImages.Add(image);
        }

        private void ProcessLinkElement(RichTextElement element, ref RichTextRow currentRow)
        {
            var linkData = RichTextParser.ParseLink(element.FormatData);

            if (linkData == null)
            {
                // 解析失败，当作普通文本处理
                ProcessTextElement(element, ref currentRow);
                return;
            }

            // 将 LinkData 添加到列表中（只添加一次，避免重复 Dispose）
            m_linkDataList.Add(linkData);

            // 确定链接颜色
            Color linkColor = m_defaultLinkColor;

            if (!string.IsNullOrEmpty(linkData.LinkColor))
            {
                ColorUtility.TryParseHtmlString(linkData.LinkColor, out linkColor);
            }

            string linkText = linkData.LinkText;
            m_font.RequestCharactersInTexture(linkText, m_currentParams.FontSize);

            bool shouldWrap = m_layoutWidth > 0 && m_currentParams.HorizontalOverflow == HorizontalWrapMode.Wrap;
            float charSpacing = m_currentParams.CharacterSpacing;

            if (!shouldWrap)
            {
                // 不换行模式：整个链接作为一个元素
                float totalWidth = 0;

                foreach (char c in linkText)
                {
                    totalWidth += GetCharacterAdvance(c) + charSpacing;
                }

                CreateLinkLabel(linkText, linkData, linkColor, totalWidth, ref currentRow);
            }
            else
            {
                // 换行模式：逐字符处理，必要时拆分链接
                // 使用静态缓存的 StringBuilder
                var sb = TextBuilder;
                sb.Clear();
                float currentWidth = 0;

                for (int i = 0; i < linkText.Length; i++)
                {
                    char c = linkText[i];
                    float charWidth = GetCharacterAdvance(c) + charSpacing;

                    // 检查是否需要换行
                    if (currentRow.Width + currentWidth + charWidth > m_layoutWidth && sb.Length > 0)
                    {
                        // 创建当前片段的链接
                        CreateLinkLabel(sb.ToString(), linkData, linkColor, currentWidth, ref currentRow);
                        sb.Clear();
                        currentWidth = 0;

                        // 换行
                        currentRow = RichTextRow.Create();
                        m_rows.Add(currentRow);
                    }

                    sb.Append(c);
                    currentWidth += charWidth;
                }

                // 创建剩余文本的链接
                if (sb.Length > 0)
                {
                    CreateLinkLabel(sb.ToString(), linkData, linkColor, currentWidth, ref currentRow);
                }
            }
        }

        /// <summary>
        /// 创建链接文本标签
        /// </summary>
        private void CreateLinkLabel(string text, LinkData linkData, Color linkColor, float width,
            ref RichTextRow currentRow)
        {
            var label = GetOrCreateText();
            SetupTextCommon(label);

            // 使用缓存的 StringBuilder 构建显示文本，避免字符串分配
            var sb = ColorBuilder;
            sb.Clear();
            sb.Append("<color=#");
            sb.Append(ColorUtility.ToHtmlStringRGB(linkColor));
            sb.Append('>');
            sb.Append(text);
            sb.Append("</color>");
            label.text = sb.ToString();

            // 启用射线检测以支持点击
            label.raycastTarget = true;

            // 添加点击事件 - 使用 LinkClickHandler 避免 Lambda 闭包
            var button = label.gameObject.GetComponent<UIButton>();

            if (button == null)
            {
                button = label.gameObject.AddComponent<UIButton>();
                button.transition = Selectable.Transition.None;
            }

            // 关闭按钮点击缩放功能
            button.ClickScaleExtend.IsUseClickScale = false;
            button.onClick.RemoveAllListeners();

            // 获取或添加 LinkClickHandler 组件
            var clickHandler = label.gameObject.GetComponent<LinkClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = label.gameObject.AddComponent<LinkClickHandler>();
            }

            clickHandler.Data = linkData;

            if (OnLinkClicked == null)
            {
                OnLinkClicked = DefaultLinkTextClickHandler;
            }
            clickHandler.Callback = OnLinkClicked;
            button.onClick.AddListener(clickHandler.OnClick);

            float height = GetTextLabelHeight(label, width);
            label.rectTransform.sizeDelta = new Vector2(width, height);

            // 创建下划线（如果需要）
            if (linkData.Style == RichTextLinkStyle.Underline)
            {
                CreateUnderline(label.rectTransform, width, linkColor);
            }

            // 检查换行（仅当 m_layoutWidth > 0 时）
            if (m_layoutWidth > 0 && currentRow.Width + width > m_layoutWidth)
            {
                currentRow = RichTextRow.Create();
                m_rows.Add(currentRow);
            }

            var layoutElement = RichTextLayoutElement.Create(RichTextElementType.Link, label.rectTransform);
            layoutElement.Width = width;
            layoutElement.Height = height;
            currentRow.AddElement(layoutElement);
            m_activeTexts.Add(label);
            m_linkTexts.Add(label);
        }

        /// <summary>
        /// 为链接创建下划线（使用对象池）
        /// </summary>
        private void CreateUnderline(RectTransform parent, float width, Color color)
        {
            UIImage underlineImage;

            if (m_underlinePool.Count > 0)
            {
                underlineImage = m_underlinePool[m_underlinePool.Count - 1];
                m_underlinePool.RemoveAt(m_underlinePool.Count - 1);
                underlineImage.gameObject.SetActive(true);
                underlineImage.transform.SetParent(parent, false);
            }
            else
            {
                var underlineGo = new GameObject("Underline", typeof(RectTransform));
                underlineGo.layer = gameObject.layer;
                underlineGo.transform.SetParent(parent, false);
                underlineImage = underlineGo.AddComponent<UIImage>();

                var underlineRect = underlineImage.rectTransform;
                underlineRect.anchorMin = new Vector2(0, 0);
                underlineRect.anchorMax = new Vector2(0, 0);
                underlineRect.pivot = new Vector2(0, 0);
            }

            underlineImage.color = color;
            underlineImage.raycastTarget = false;
            underlineImage.rectTransform.anchoredPosition = new Vector2(0, 0);
            underlineImage.rectTransform.sizeDelta = new Vector2(width, m_underlineHeight);

            m_activeUnderlines.Add(underlineImage);
        }

        private void CreateTextLabel(string text, string colorValue, float width, ref RichTextRow currentRow)
        {
            var label = GetOrCreateText();
            SetupTextCommon(label);

            // 应用颜色格式
            if (!string.IsNullOrEmpty(colorValue))
            {
                // 使用缓存的 StringBuilder，避免字符串分配
                var sb = ColorBuilder;
                sb.Clear();
                sb.Append("<color=");
                sb.Append(colorValue);
                sb.Append('>');
                sb.Append(text);
                sb.Append("</color>");
                label.text = sb.ToString();
            }
            else
            {
                label.text = text;
            }

            float height = GetTextLabelHeight(label, width);
            label.rectTransform.sizeDelta = new Vector2(width, height);

            var layoutElement = RichTextLayoutElement.Create(RichTextElementType.Text, label.rectTransform);
            layoutElement.Width = width;
            layoutElement.Height = height;
            currentRow.AddElement(layoutElement);
            m_activeTexts.Add(label);
        }

        #endregion

        #region 布局应用

        private void ApplyLayout()
        {
            if (m_rows.Count == 0) return;

            float totalHeight = 0;

            foreach (var row in m_rows)
            {
                totalHeight += row.Height;
            }

            float currentY = 0;

            for (int rowIdx = 0; rowIdx < m_rows.Count; rowIdx++)
            {
                var row = m_rows[rowIdx];
                float xOffset = CalculateRowXOffset(row);
                float currentX = xOffset;

                foreach (var element in row.Elements)
                {
                    Vector2 position = new Vector2(currentX, currentY);

                    // 为图标/表情应用垂直对齐
                    if (element.Type == RichTextElementType.Icon || element.Type == RichTextElementType.Emoji)
                    {
                        switch (m_currentParams.IconAlignment)
                        {
                            case RichTextIconAlignment.Center:
                                position.y -= (row.Height - element.Height) / 2f;
                                break;

                            case RichTextIconAlignment.Bottom:
                                position.y -= (row.Height - element.Height);
                                break;

                            case RichTextIconAlignment.Top:
                                // 顶部对齐: position.y 保持在 0（行顶部）
                                break;
                        }

                        position += m_iconOffset;
                    }

                    element.RectTransform.anchoredPosition = position;
                    currentX += element.Width;
                }

                currentY -= row.Height;
            }
        }

        private float CalculateRowXOffset(RichTextRow row)
        {
            float totalWidth = RectTransform.sizeDelta.x;

            switch (m_currentParams.Alignment)
            {
                case RichTextAlignment.Center:
                    return (totalWidth - row.Width) / 2f;

                case RichTextAlignment.Right:
                    return totalWidth - row.Width;

                case RichTextAlignment.Left:
                default:
                    return 0;
            }
        }

        private void CalculateTotalSize()
        {
            float contentWidth = 0;
            float totalHeight = 0;

            // 计算内容尺寸并处理垂直截断
            for (int i = 0; i < m_rows.Count; i++)
            {
                var row = m_rows[i];
                if (row.Width > contentWidth) contentWidth = row.Width;

                float newHeight = totalHeight + row.Height;

                // 检查是否超过 m_layoutHeight（Truncate 模式）
                if (m_layoutHeight > 0 && newHeight > m_layoutHeight)
                {
                    // 隐藏被截断行中的元素
                    for (int j = i; j < m_rows.Count; j++)
                    {
                        foreach (var element in m_rows[j].Elements)
                        {
                            if (element.RectTransform != null)
                            {
                                element.RectTransform.gameObject.SetActive(false);
                            }
                        }
                    }

                    break;
                }

                totalHeight = newHeight;
            }

            // 确定最终宽度
            float finalWidth;

            if (m_layoutWidth > 0)
            {
                // Wrap 模式: 使用固定布局宽度
                finalWidth = m_layoutWidth;
            }
            else
            {
                // Overflow 模式: 使用内容宽度
                finalWidth = contentWidth;
            }

            // 确定最终高度
            float finalHeight;

            if (m_layoutHeight > 0)
            {
                // Truncate 模式: 使用固定布局高度（如果内容更小则使用内容高度）
                finalHeight = Mathf.Min(m_layoutHeight, totalHeight);
            }
            else
            {
                // Overflow 模式: 使用内容高度
                finalHeight = totalHeight;
            }

            UpdateLayoutMetrics(finalWidth, finalHeight);
        }

        private void UpdateLayoutMetrics(float width, float height)
        {
            RectTransform.sizeDelta = new Vector2(width, height);
            m_layoutMinWidth = width;
            m_layoutPreferredWidth = width;
            m_layoutMinHeight = height;
            m_layoutPreferredHeight = height;
        }

        private void MarkLayoutDirty()
        {
            var parent = transform.parent as RectTransform;
            if (parent != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parent);
            }
        }

        private void RefreshIconLayout()
        {
#if TextMeshPro
            if (m_mergedText != null)
            {
                RefreshMergedTextLayout();
                return;
            }
#else
            if (m_nativeMergedText != null)
            {
                RefreshNativeMergedTextLayout();
                return;
            }
#endif

            if (m_currentParams.IconSize == Vector2.zero && m_content != null)
            {
                // 原图尺寸就绪后重新分行，而不是只调整旧行的宽高。
                // 保存表情正在显示的帧，避免重排使动画回到首帧。
                var frames = ListPool<int>.Get();
                var elements = RichTextParser.Parse(m_content, RichTextConfig.IconPrefix);
                try
                {
                    foreach (var instance in m_emojiInstances) frames.Add(instance.CurrentFrame);
                    BuildLayout(elements);
                    for (var index = 0; index < m_emojiInstances.Count; index++)
                    {
                        var instance = m_emojiInstances[index];
                        var frameCount = instance.AnimationData.FrameCount;
                        if (index < frames.Count && frameCount > 0)
                        {
                            instance.CurrentFrame = (frames[index] + frameCount - 1) % frameCount;
                        }
                    }
                    UpdateEmojiFrames();
                    ApplyLayout();
                    MarkLayoutDirty();
                    m_pendingEffects = NeedsDeferredTextEffects;
                }
                finally
                {
                    RichTextParser.Recycle(elements);
                    ListPool<int>.Recycle(frames);
                }
                return;
            }

            foreach (var row in m_rows)
            {
                row.Width = 0;
                row.Height = 0;

                foreach (var element in row.Elements)
                {
                    if (element.RectTransform == null)
                    {
                        continue;
                    }

                    Vector2 size = element.RectTransform.sizeDelta;
                    element.Width = size.x;
                    element.Height = size.y;
                    row.Width += element.Width;
                    row.Height = Mathf.Max(row.Height, element.Height);
                }
            }

            CalculateTotalSize();
            ApplyLayout();
            MarkLayoutDirty();
        }

        #endregion

        #region 对象池

        private RichTextLabel GetOrCreateText()
        {
            RichTextLabel text;

            if (m_textPool.Count > 0)
            {
                text = m_textPool[m_textPool.Count - 1];
                m_textPool.RemoveAt(m_textPool.Count - 1);
                text.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("RichText_Label", typeof(RectTransform));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
#if TextMeshPro
                text = go.AddComponent<TextMeshProUGUI>();
#else
                text = go.AddComponent<UIText>();
#endif
                SetupRectTransform(text.rectTransform);
            }

            return text;
        }

        private UIImage GetOrCreateImage()
        {
            UIImage image;

            if (m_imagePool.Count > 0)
            {
                image = m_imagePool[^1];
                m_imagePool.RemoveAt(m_imagePool.Count - 1);
                image.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("RichText_Icon", typeof(RectTransform));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                image = go.AddComponent<UIImage>();
                SetupRectTransform(image.rectTransform);
            }

            return image;
        }

        private void SetupRectTransform(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void SetupTextCommon(RichTextLabel text)
        {
#if TextMeshPro
            text.font = m_tmpFont != null ? m_tmpFont : TMP_Settings.defaultFontAsset;
            text.fontSize = m_currentParams.FontSize;
            text.color = m_currentParams.TextColor;
            text.alignment = m_currentParams.Alignment switch
            {
                RichTextAlignment.Center => TextAlignmentOptions.Midline,
                RichTextAlignment.Right => TextAlignmentOptions.MidlineRight,
                _ => TextAlignmentOptions.MidlineLeft
            };
            // RichTextItem performs wrapping at the element level so TMP labels must not wrap again.
            text.enableWordWrapping = false;
            text.overflowMode = m_currentParams.VerticalOverflow == VerticalWrapMode.Truncate
                ? TextOverflowModes.Truncate
                : TextOverflowModes.Overflow;
            text.maxVisibleLines = int.MaxValue;
            text.richText = m_currentParams.SupportRichText;
            text.characterSpacing = m_currentParams.CharacterSpacing;
            text.lineSpacing = m_currentParams.LineSpacing;
            text.raycastTarget = false;
            // 材质和 padding 必须先于 ForceMeshUpdate，避免图标定位后再次重建文字网格。
            ApplyTextEffects(text);
#else
            text.font = m_font;
            text.fontSize = m_currentParams.FontSize;
            text.color = m_currentParams.TextColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1f;
            text.supportRichText = m_currentParams.SupportRichText;
            text.raycastTarget = false;

            // 应用字间距
            var spacingExtend = text.UITextSpacingExtend;

            if (spacingExtend != null)
            {
                bool useSpacing = m_currentParams.CharacterSpacing != 0;
                spacingExtend.IsUseTextSpacing = useSpacing;

                if (useSpacing)
                {
                    spacingExtend.TextSpacing = m_currentParams.CharacterSpacing;
                }
            }

            // 关闭特效不依赖布局，先清除池内文本遗留的阴影和描边。
            if (!NeedsDeferredTextEffects) ApplyTextEffects(text);

            // 启用阴影/描边仍在布局完成后通过 ApplyAllTextEffects() 应用。
#endif
        }

        private float GetTextLabelHeight(RichTextLabel text, float width)
        {
            float minimumHeight = m_currentParams.FontSize + m_currentParams.LineSpacing;
#if TextMeshPro
            if (width > 0)
            {
                return Mathf.Max(minimumHeight, text.GetPreferredValues(text.text, width, 0).y);
            }
#endif
            return minimumHeight;
        }

        private void SetupImageCommon(UIImage image, bool raycastEnabled)
        {
            image.raycastTarget = raycastEnabled;
            image.sprite = null;
        }

        /// <summary>
        /// 在布局完成后为所有活动的文本元素应用特效
        /// </summary>
        private void ApplyAllTextEffects()
        {
            foreach (var text in m_activeTexts)
            {
                if (text != null)
                {
                    ApplyTextEffects(text);
                }
            }
        }

        private void ApplyTextEffects(RichTextLabel text)
        {
#if TextMeshPro
            bool useOutline = m_currentParams.EnableOutline;
            bool useUnderlay = m_currentParams.EnableShadow;
            if (!useOutline && !useUnderlay)
            {
                ReleaseTmpEffectMaterial(text);
                var defaultMaterial = text.font != null ? text.font.material : TMP_Settings.defaultFontAsset?.material;
                if (text.fontSharedMaterial == defaultMaterial)
                {
                    return;
                }

                text.fontSharedMaterial = defaultMaterial;
                text.UpdateMeshPadding();
                text.SetMaterialDirty();
                return;
            }

            var sourceMaterial = text.font != null ? text.font.material : TMP_Settings.defaultFontAsset?.material;
            if (sourceMaterial == null)
            {
                ReleaseTmpEffectMaterial(text);
                return;
            }

            var outlineColor = useOutline ? m_currentParams.OutlineColor : Color.clear;
            // 旧 1-10 档统一到与字体预设一致的 1-4 档。
            var outlineWidth = useOutline
                ? Mathf.Clamp(Mathf.RoundToInt(m_currentParams.OutlineWidth * 0.4f), 1, 4) * 0.1f
                : 0f;
            // TMP 沿用原生单色阴影，使用左上颜色；四色渐变由非 TMP 的 UIText 实现。
            var underlayColor = useUnderlay ? m_currentParams.ShadowTopLeftColor : Color.clear;
            var underlayOffsetX = useUnderlay ? m_currentParams.ShadowEffectDistance.x : 0f;
            var underlayOffsetY = useUnderlay ? m_currentParams.ShadowEffectDistance.y : 0f;
            var materialKey = string.Concat(
                sourceMaterial.GetInstanceID(), ":",
                useOutline, ":", outlineWidth, ":",
                outlineColor.r, ":", outlineColor.g, ":", outlineColor.b, ":", outlineColor.a, ":",
                useUnderlay, ":",
                underlayColor.r, ":", underlayColor.g, ":", underlayColor.b, ":", underlayColor.a, ":",
                underlayOffsetX, ":", underlayOffsetY);

            bool alreadyHeld = m_tmpEffectMaterialKeys.TryGetValue(text, out var currentKey) && currentKey == materialKey;
            if (!alreadyHeld)
            {
                ReleaseTmpEffectMaterial(text);
            }

            if (!s_tmpEffectMaterials.TryGetValue(materialKey, out var entry) || entry.Material == null)
            {
                var effectMaterial = new Material(sourceMaterial)
                {
                    name = sourceMaterial.name + " (RichText Effects)"
                };

                // 描边与阴影独立设置，可同时启用，并保留各自颜色的透明度。
                if (useOutline)
                    effectMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                else
                    effectMaterial.DisableKeyword(ShaderUtilities.Keyword_Outline);

                effectMaterial.DisableKeyword("UNDERLAY_INNER");
                if (useUnderlay)
                    effectMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                else
                    effectMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);

                effectMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
                effectMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
                effectMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, underlayColor);
                effectMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
                effectMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                effectMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, underlayOffsetX);
                effectMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, underlayOffsetY);
                entry ??= new TmpEffectMaterialEntry();
                entry.Material = effectMaterial;
                s_tmpEffectMaterials[materialKey] = entry;
            }

            if (!alreadyHeld)
            {
                entry.ReferenceCount++;
                m_tmpEffectMaterialKeys[text] = materialKey;
            }

            if (text.fontSharedMaterial == entry.Material)
            {
                return;
            }

            text.fontSharedMaterial = entry.Material;
            text.UpdateMeshPadding();
            text.SetMaterialDirty();
#else
            // 阴影 - 使用 UIText 内置的阴影功能
            var shadowExtend = text.UITextShadowExtend;

            if (shadowExtend != null)
            {
                shadowExtend.UseShadow = m_currentParams.EnableShadow;

                if (m_currentParams.EnableShadow)
                {
                    shadowExtend.EffectDistance = m_currentParams.ShadowEffectDistance;
                    shadowExtend.SetShadowColor(m_currentParams.ShadowTopLeftColor, m_currentParams.ShadowTopRightColor,
                        m_currentParams.ShadowBottomLeftColor, m_currentParams.ShadowBottomRightColor);
                }
            }

            // 描边 - 使用 UIText 内置的描边功能
            var outlineExtend = text.UITextOutlineExtend;

            if (outlineExtend != null)
            {
                if (m_currentParams.EnableOutline)
                {
                    text.SetOutLineColor(m_currentParams.OutlineColor, m_currentParams.OutlineWidth);
                }

                outlineExtend.SetUseTextOutline(m_currentParams.EnableOutline);
            }
#endif
        }

#if TextMeshPro
        /// <summary>释放指定文本持有的特效材质，重复释放不会改变引用计数。</summary>
        private void ReleaseTmpEffectMaterial(RichTextLabel text)
        {
            if (m_tmpEffectMaterialKeys.Remove(text, out var materialKey))
            {
                ReleaseTmpEffectMaterial(text, materialKey);
            }
        }

        /// <summary>解除文本的材质引用，最后一个使用者释放时销毁动态材质。</summary>
        private static void ReleaseTmpEffectMaterial(RichTextLabel text, string materialKey)
        {
            if (!s_tmpEffectMaterials.TryGetValue(materialKey, out var entry))
            {
                return;
            }

            // 池内文本也必须解除引用，字体资源自带的原始材质不由此缓存销毁。
            if (text != null && text.fontSharedMaterial == entry.Material)
            {
                text.fontSharedMaterial = text.font != null ? text.font.material : TMP_Settings.defaultFontAsset?.material;
                text.UpdateMeshPadding();
                text.SetMaterialDirty();
            }

            entry.ReferenceCount--;
            if (entry.ReferenceCount > 0)
            {
                return;
            }

            s_tmpEffectMaterials.Remove(materialKey);
            if (entry.Material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(entry.Material);
                }
                else
                {
                    DestroyImmediate(entry.Material);
                }
            }
        }

        /// <summary>组件销毁时释放全部持有项，包括已被外部销毁的文本。</summary>
        private void ReleaseAllTmpEffectMaterials()
        {
            foreach (var pair in m_tmpEffectMaterialKeys)
            {
                ReleaseTmpEffectMaterial(pair.Key, pair.Value);
            }

            m_tmpEffectMaterialKeys.Clear();
        }
#endif

        private void RecycleAllElements()
        {
            foreach (var text in m_activeTexts)
            {
#if TextMeshPro
                ReleaseTmpEffectMaterial(text);
#endif
                if (text != null)
                {
                    text.gameObject.SetActive(false);
                    m_textPool.Add(text);
                }
            }

            m_activeTexts.Clear();

            foreach (var image in m_activeImages)
            {
                if (image != null)
                {
                    image.sprite = null;
                    image.gameObject.SetActive(false);
                    m_imagePool.Add(image);
                }
            }

            m_activeImages.Clear();

            // 清理待处理的图标尺寸
            m_pendingNativeSizeIcons.Clear();
            m_pendingIconLayoutRefresh = false;
#if TextMeshPro
            m_mergedIconAnchors.Clear();
            m_mergedText = null;
#else
            m_nativeMergedText = null;
            m_nativeMergedRowCount = 0;
#endif

            // 回收下划线
            RecycleUnderlines();
        }

        private void RecycleUnderlines()
        {
            foreach (var underline in m_activeUnderlines)
            {
                if (underline != null)
                {
                    underline.gameObject.SetActive(false);
                    underline.transform.SetParent(transform, false);
                    m_underlinePool.Add(underline);
                }
            }

            m_activeUnderlines.Clear();
        }

        private void ClearRows()
        {
            foreach (var row in m_rows)
            {
                row.Dispose();
            }

            m_rows.Clear();
        }

        private void ClearLinkElements()
        {
            foreach (var text in m_linkTexts)
            {
                if (text != null)
                {
                    var button = text.GetComponent<UIButton>();

                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                    }

                    // 清理 LinkClickHandler
                    var clickHandler = text.GetComponent<LinkClickHandler>();
                    if (clickHandler != null)
                    {
                        clickHandler.Clear();
                    }
                }
            }

            m_linkTexts.Clear();

            // 回收 LinkData 到对象池（每个只 Dispose 一次）
            foreach (var data in m_linkDataList)
            {
                data?.Dispose();
            }

            m_linkDataList.Clear();
        }

        private void RecycleEmojiInstances()
        {
            foreach (var instance in m_emojiInstances)
            {
                instance.Dispose();
            }

            m_emojiInstances.Clear();
        }

        #endregion

        #region 表情动画

        /// <summary>
        /// 用于 LateUpdate 动画循环的表情帧更新（无闭包版本）
        /// </summary>
        private void UpdateEmojiFramesForAnimation()
        {
            foreach (var instance in m_emojiInstances)
            {
                if (instance.TryGetNextFrame(out var image, out var spriteName))
                {
                    if (image != null && !string.IsNullOrEmpty(spriteName))
                    {
                        // 使用实例方法作为回调，避免 Lambda 闭包
                        RichTextConfig.SetSprite(image, spriteName, m_pendingNativeSizeIcons.ContainsKey(image),
                            OnEmojiSpriteLoaded, m_cts?.Token ?? default);
                    }
                    else
                    {
                        instance.MarkLoadComplete();
                    }
                }
            }
        }

        /// <summary>
        /// 表情精灵加载完成回调（静态化避免闭包）
        /// </summary>
        private void OnEmojiSpriteLoaded(Image img)
        {
            OnIconSpriteLoaded(img);
            // 查找对应的 EmojiAnimationInstance 并标记加载完成
            foreach (var instance in m_emojiInstances)
            {
                if (instance.TargetImage == img)
                {
                    instance.MarkLoadComplete();
                    break;
                }
            }
        }

        private void UpdateEmojiFrames()
        {
            UpdateEmojiFrames(m_cts?.Token ?? default);
        }

        private void UpdateEmojiFrames(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            foreach (var instance in m_emojiInstances)
            {
                if (instance.TryGetNextFrame(out var image, out var spriteName))
                {
                    if (image != null && !string.IsNullOrEmpty(spriteName) && !token.IsCancellationRequested)
                    {
                        RichTextConfig.SetSprite(image, spriteName, m_pendingNativeSizeIcons.ContainsKey(image),
                            OnEmojiSpriteLoaded, token);
                    }
                    else
                    {
                        instance.MarkLoadComplete();
                    }
                }
            }
        }

        #endregion

        #region 辅助方法

        private RichTextParams CreateParamsFromInspector()
        {
            // 复用缓存的参数对象，避免每次分配
            if (m_cachedParams == null)
            {
                m_cachedParams = new RichTextParams();
            }

            m_cachedParams.FontSize = m_fontSize;
            m_cachedParams.IconSize = m_iconSize;
            m_cachedParams.TextColor = m_fontColor;
            m_cachedParams.Alignment = m_alignment;
            m_cachedParams.IconAlignment = m_iconAlignment;
            m_cachedParams.CharacterSpacing = m_characterSpacing;
            m_cachedParams.LineSpacing = m_lineSpacing;
            m_cachedParams.SupportRichText = m_supportRichText;
            m_cachedParams.HorizontalOverflow = m_horizontalOverflow;
            m_cachedParams.VerticalOverflow = m_verticalOverflow;
            // 阴影
            m_cachedParams.EnableShadow = m_enableShadow;
            m_cachedParams.ShadowEffectDistance = m_shadowEffectDistance;
            m_cachedParams.ShadowTopLeftColor = m_shadowTopLeftColor;
            m_cachedParams.ShadowTopRightColor = m_shadowTopRightColor;
            m_cachedParams.ShadowBottomLeftColor = m_shadowBottomLeftColor;
            m_cachedParams.ShadowBottomRightColor = m_shadowBottomRightColor;
            // 描边
            m_cachedParams.EnableOutline = m_enableOutline;
            m_cachedParams.OutlineColor = m_outlineColor;
            m_cachedParams.OutlineWidth = m_outlineWidth;

            return m_cachedParams;
        }

        private void CancelPendingOperations()
        {
            if (m_cts != null)
            {
                m_cts.Cancel();
                m_cts.Dispose();
                m_cts = null;
            }
        }

        /// <summary>
        /// 默认的点击超链接文本事件 只读道具表
        /// </summary>
        /// <param name="linkData"></param>
        private void DefaultLinkTextClickHandler(LinkData linkData)
        {
            // 只有当 OnLinkClicked == null 的时候 才会使用这个默认的超链接点击事件
            DLogger.Info("如果需要默认超链接点击事件 请在这里进行处理默认逻辑");
            // if (TbItemConfig.TryGetValue(linkData.LinkID, out var itemCfg))
            // {
            //
            // }
        }

        #endregion
    }
}
