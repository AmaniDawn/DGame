using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 高性能富文本组件，支持文本、图标、表情和链接
    /// 与项目的资源加载模块集成，用于加载图片
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UGUIPro/RichTextItem")]
    public class RichTextItem : MonoBehaviour
    {
        #region 序列化字段

        [Header("Text Settings")] [SerializeField]
        private Font m_font;

        [SerializeField] private int m_fontSize = 24;
        [SerializeField] private Color m_fontColor = Color.white;
        [SerializeField] private bool m_supportRichText = true;

        [Header("Icon Settings")] [SerializeField]
        private int m_iconSize = 24;

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

        // 静态缓存 - 减少 GC 分配
        [ThreadStatic] private static StringBuilder s_textBuilder;
        [ThreadStatic] private static StringBuilder s_colorBuilder;

        private static StringBuilder TextBuilder => s_textBuilder ??= new StringBuilder(256);
        private static StringBuilder ColorBuilder => s_colorBuilder ??= new StringBuilder(128);

        // 缓存的参数对象，避免每次创建新实例
        private RichTextParams m_cachedParams;

        // 对象池
        private readonly List<UIText> m_textPool = new List<UIText>();
        private readonly List<UIImage> m_imagePool = new List<UIImage>();
        private readonly List<UIText> m_activeTexts = new List<UIText>();
        private readonly List<UIImage> m_activeImages = new List<UIImage>();
        private readonly List<UIImage> m_underlinePool = new List<UIImage>();
        private readonly List<UIImage> m_activeUnderlines = new List<UIImage>();

        // 表情动画
        private readonly List<EmojiAnimationInstance> m_emojiInstances = new List<EmojiAnimationInstance>();
        private float m_emojiTimer = 0f;
        private bool m_hasEmojis = false;

        // 布局数据
        private readonly List<RichTextRow> m_rows = new List<RichTextRow>();

        // 延迟应用特效
        private bool m_pendingEffects = false;

        // 链接元素 - 只存储 text，LinkData 单独管理避免重复 Dispose
        private readonly List<UIText> m_linkTexts = new List<UIText>();
        // 唯一的 LinkData 列表，避免重复 Dispose
        private readonly List<LinkData> m_linkDataList = new List<LinkData>();

        // 异步渲染状态
        private bool m_isRendering = false;

        // 图标加载上下文（避免 Lambda 闭包）
        private readonly Dictionary<Image, float> m_pendingIconSizes = new Dictionary<Image, float>();

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
        public int RowCount => m_rows.Count;

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

        public int IconSize
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
        /// 渲染进度回调 (0~1)
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

            // 在布局之前保存原始尺寸（用于 Wrap/Truncate 模式）
            m_originalWidth = RectTransform.sizeDelta.x;
            m_originalHeight = RectTransform.sizeDelta.y;

            // 取消任何待处理的操作
            CancelPendingOperations();
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

            // 同步渲染
            BuildLayout(elements);
            ApplyLayout();

            // 标记下一帧应用特效（在所有渲染完成后）
            m_pendingEffects = m_currentParams.EnableShadow || m_currentParams.EnableOutline;

            // 如果有表情则启动动画
            m_emojiTimer = 0f;

            if (m_hasEmojis)
            {
                UpdateEmojiFrames();
            }

            // 清理解析后的元素
            foreach (var element in elements)
            {
                element.Dispose();
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
                if (useAsync)
                {
                    await BuildLayoutAsync(elements, token);
                }
                else
                {
                    // 同步渲染
                    BuildLayout(elements);
                }

                // 检查取消
                token.ThrowIfCancellationRequested();

                ApplyLayout();
                FinalizeRendering(elements, token);
            }
            catch (OperationCanceledException)
            {
                // 取消时清理元素
                foreach (var element in elements)
                {
                    element.Dispose();
                }

                throw;
            }
            finally
            {
                m_isRendering = false;
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
            ClearLinkElements();
            RecycleAllElements();
            ClearRows();
            RecycleEmojiInstances();
            m_hasEmojis = false;
            m_pendingEffects = false;
            RectTransform.sizeDelta = Vector2.zero;
        }

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
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
                if (m_layoutWidth > 0)
                {
                    RectTransform.sizeDelta = new Vector2(m_layoutWidth, RectTransform.sizeDelta.y);
                }

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
                if (m_layoutWidth > 0)
                {
                    RectTransform.sizeDelta = new Vector2(m_layoutWidth, RectTransform.sizeDelta.y);
                }

                return;
            }

            RichTextRow currentRow = RichTextRow.Create();
            m_rows.Add(currentRow);

            int processedCount = 0;
            int totalCount = elements.Count;

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

                // 报告进度
                float progress = (float)processedCount / totalCount;
                OnRenderProgress?.Invoke(progress);

                // 每处理 N 个元素暂停一帧 (使用 PreLateUpdate 不受 timeScale 影响)
                if (processedCount % m_elementsPerFrame == 0)
                {
                    await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, token);
                }
            }

            // 计算尺寸
            CalculateTotalSize();
        }

        /// <summary>
        /// 完成渲染的收尾工作
        /// </summary>
        private void FinalizeRendering(List<RichTextElement> elements)
        {
            FinalizeRendering(elements, m_cts?.Token ?? default);
        }

        /// <summary>
        /// 完成渲染的收尾工作 (带取消令牌)
        /// </summary>
        private void FinalizeRendering(List<RichTextElement> elements, CancellationToken token)
        {
            // 标记下一帧应用特效
            m_pendingEffects = m_currentParams.EnableShadow || m_currentParams.EnableOutline;

            // 如果有表情且未取消则启动动画
            m_emojiTimer = 0f;

            if (m_hasEmojis && !token.IsCancellationRequested)
            {
                UpdateEmojiFrames(token);
            }

            // 清理解析后的元素
            foreach (var element in elements)
            {
                element.Dispose();
            }

            // 触发完成回调 (即使取消也触发，让调用方知道处理结束)
            OnRenderProgress?.Invoke(1f);
            OnRenderComplete?.Invoke();
        }

        /// <summary>
        /// 内部异步渲染方法（避免 Lambda 闭包）
        /// </summary>
        private async UniTaskVoid RenderAsyncInternal(List<RichTextElement> elements, CancellationToken token)
        {
            try
            {
                await BuildLayoutAsync(elements, token);

                // 检查是否已被取消
                if (token.IsCancellationRequested)
                {
                    // 清理元素但不执行渲染
                    foreach (var element in elements)
                    {
                        element.Dispose();
                    }

                    m_isRendering = false;
                    return;
                }

                ApplyLayout();
                FinalizeRendering(elements, token);
            }
            finally
            {
                m_isRendering = false;
            }
        }

        private void ProcessTextElement(RichTextElement element, ref RichTextRow currentRow)
        {
            string text = element.GetText();
            if (string.IsNullOrEmpty(text)) return;

            m_font.RequestCharactersInTexture(text, m_currentParams.FontSize);

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
                m_font.GetCharacterInfo(c, out CharacterInfo info, m_currentParams.FontSize);
                float charWidth = info.advance + charSpacing;

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

            float size = m_currentParams.IconSize;

            // 存储图标尺寸到字典，供回调使用（避免 Lambda 闭包）
            m_pendingIconSizes[image] = size;

            // 加载图片并调整尺寸 - 使用实例方法避免闭包
            RichTextConfig.SetSprite(image, element.FormatData, true, OnIconSpriteLoaded, m_cts?.Token ?? default);

            image.rectTransform.sizeDelta = new Vector2(size, size);

            // 检查是否需要换行（仅当 m_layoutWidth > 0 时）
            if (m_layoutWidth > 0 && currentRow.Width + size > m_layoutWidth)
            {
                currentRow = RichTextRow.Create();
                m_rows.Add(currentRow);
            }

            var layoutElement = RichTextLayoutElement.Create(RichTextElementType.Icon, image.rectTransform);
            layoutElement.Width = size;
            layoutElement.Height = size;
            currentRow.AddElement(layoutElement);
            m_activeImages.Add(image);
        }

        /// <summary>
        /// 图标精灵加载完成回调（避免 Lambda 闭包）
        /// </summary>
        private void OnIconSpriteLoaded(Image img)
        {
            if (img == null || img.sprite == null) return;

            if (m_pendingIconSizes.TryGetValue(img, out float size))
            {
                float aspectRatio = img.sprite.rect.width / img.sprite.rect.height;
                img.rectTransform.sizeDelta = new Vector2(size * aspectRatio, size);
                m_pendingIconSizes.Remove(img);
            }
        }

        private void ProcessEmojiElement(RichTextElement element, ref RichTextRow currentRow)
        {
            var emojiData = RichTextConfig.GetEmojiData(element.FormatData);
            if (emojiData == null) return;

            var image = GetOrCreateImage();
            SetupImageCommon(image, emojiData.RaycastEnabled);

            float size = m_currentParams.IconSize;
            image.rectTransform.sizeDelta = new Vector2(size, size);

            // 使用对象池创建表情动画实例
            var animInstance = EmojiAnimationInstance.Create(image, emojiData);
            m_emojiInstances.Add(animInstance);
            m_hasEmojis = true;

            // 检查是否需要换行（仅当 m_layoutWidth > 0 时）
            if (m_layoutWidth > 0 && currentRow.Width + size > m_layoutWidth)
            {
                currentRow = RichTextRow.Create();
                m_rows.Add(currentRow);
            }

            var layoutElement = RichTextLayoutElement.Create(RichTextElementType.Emoji, image.rectTransform);
            layoutElement.Width = size;
            layoutElement.Height = size;
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
                    m_font.GetCharacterInfo(c, out CharacterInfo info, m_currentParams.FontSize);
                    totalWidth += info.advance + charSpacing;
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
                    m_font.GetCharacterInfo(c, out CharacterInfo info, m_currentParams.FontSize);
                    float charWidth = info.advance + charSpacing;

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

            float height = m_currentParams.FontSize + m_currentParams.LineSpacing;
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

            float height = m_currentParams.FontSize + m_currentParams.LineSpacing;
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

            RectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);
        }

        #endregion

        #region 对象池

        private UIText GetOrCreateText()
        {
            UIText text;

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
                text = go.AddComponent<UIText>();
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

        private void SetupTextCommon(UIText text)
        {
            text.font = m_font;
            text.fontSize = m_currentParams.FontSize;
            text.color = m_currentParams.TextColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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

            // 注意: 文本特效（阴影/描边）在布局完成后通过 ApplyAllTextEffects() 应用
            // 以确保正确渲染
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

        private void ApplyTextEffects(UIText text)
        {
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
        }

        private void RecycleAllElements()
        {
            foreach (var text in m_activeTexts)
            {
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
            m_pendingIconSizes.Clear();

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
                        RichTextConfig.SetSprite(image, spriteName, false, OnEmojiSpriteLoaded, default);
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
                        RichTextConfig.SetSprite(image, spriteName, false, OnEmojiSpriteLoaded, token);
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