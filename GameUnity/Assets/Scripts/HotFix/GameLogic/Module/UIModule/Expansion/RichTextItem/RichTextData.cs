using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 文本对齐方式
    /// </summary>
    public enum RichTextAlignment
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// 文本元素类型
    /// </summary>
    public enum RichTextElementType
    {
        Text,
        Icon,
        Emoji,
        Link
    }

    /// <summary>
    /// 图标垂直对齐方式
    /// </summary>
    public enum RichTextIconAlignment
    {
        Center,
        Bottom,
        Top
    }

    /// <summary>
    /// 链接样式
    /// </summary>
    public enum RichTextLinkStyle
    {
        Normal,      // 普通文本
        Underline    // 下划线
    }

    /// <summary>
    /// 链接数据
    /// </summary>
    public class LinkData
    {
        public int LinkID;                    // 链接ID
        public string LinkText;               // 显示文本
        public string LinkColor;              // 链接颜色 (如 "#00BFFF")
        public RichTextLinkStyle Style;       // 链接样式

        private static readonly Stack<LinkData> s_pool = new Stack<LinkData>(16);

        public static LinkData Create(int id, string text, string color = null, RichTextLinkStyle style = RichTextLinkStyle.Normal)
        {
            LinkData data;
            if (s_pool.Count > 0)
            {
                data = s_pool.Pop();
            }
            else
            {
                data = new LinkData();
            }

            data.LinkID = id;
            data.LinkText = text;
            data.LinkColor = color;
            data.Style = style;
            return data;
        }

        public void Dispose()
        {
            LinkID = 0;
            LinkText = null;
            LinkColor = null;
            Style = RichTextLinkStyle.Normal;
            s_pool.Push(this);
        }

        // 保留原有构造函数以兼容
        public LinkData() { }

        public LinkData(int id, string text, string color = null, RichTextLinkStyle style = RichTextLinkStyle.Normal)
        {
            LinkID = id;
            LinkText = text;
            LinkColor = color;
            Style = style;
        }
    }

    /// <summary>
    /// 富文本配置参数
    /// </summary>
    [Serializable]
    public class RichTextParams
    {
        public int FontSize = 24;
        public int IconSize = 24;
        public Color TextColor = Color.white;
        public RichTextAlignment Alignment = RichTextAlignment.Left;
        public RichTextIconAlignment IconAlignment = RichTextIconAlignment.Center;
        public float CharacterSpacing = 0;
        public float LineSpacing = 0;
        public bool SupportRichText = true;
        public HorizontalWrapMode HorizontalOverflow = HorizontalWrapMode.Wrap;
        public VerticalWrapMode VerticalOverflow = VerticalWrapMode.Overflow;

        // 阴影设置
        public bool EnableShadow = false;
        public Vector2 ShadowEffectDistance = new Vector2(1f, -1f);
        public Color ShadowTopLeftColor = Color.black;
        public Color ShadowTopRightColor = Color.black;
        public Color ShadowBottomLeftColor = Color.black;
        public Color ShadowBottomRightColor = Color.black;

        // 描边设置
        public bool EnableOutline = false;
        public Color OutlineColor = Color.black;
        public int OutlineWidth = 1;

        public RichTextParams() { }

        public RichTextParams(int fontSize, int iconSize)
        {
            FontSize = fontSize;
            IconSize = iconSize;
        }

    }

    /// <summary>
    /// 解析后的文本元素数据
    /// </summary>
    public class RichTextElement : IDisposable
    {
        public RichTextElementType Type;
        public string Content;
        public string FormatData;
        public StringBuilder TextBuilder;
        public bool RaycastEnabled = true;

        private static readonly Stack<RichTextElement> s_pool = new Stack<RichTextElement>(32);

        private RichTextElement()
        {
            TextBuilder = new StringBuilder(64);
        }

        public static RichTextElement Create(RichTextElementType type, string formatData = null)
        {
            RichTextElement element;
            if (s_pool.Count > 0)
            {
                element = s_pool.Pop();
            }
            else
            {
                element = new RichTextElement();
            }

            element.Type = type;
            element.FormatData = formatData;
            element.Content = null;
            element.TextBuilder.Clear();
            element.RaycastEnabled = true;
            return element;
        }

        public void Dispose()
        {
            TextBuilder.Clear();
            Content = null;
            FormatData = null;
            s_pool.Push(this);
        }

        public string GetText()
        {
            return Content ?? TextBuilder.ToString();
        }
    }

    /// <summary>
    /// 布局元素数据 - 表示带有变换信息的渲染元素
    /// </summary>
    public class RichTextLayoutElement : IDisposable
    {
        public RichTextElementType Type;
        public RectTransform RectTransform;
        public float Width;
        public float Height;

        private static readonly Stack<RichTextLayoutElement> s_pool = new Stack<RichTextLayoutElement>(32);

        public static RichTextLayoutElement Create(RichTextElementType type, RectTransform rect)
        {
            RichTextLayoutElement element;
            if (s_pool.Count > 0)
            {
                element = s_pool.Pop();
            }
            else
            {
                element = new RichTextLayoutElement();
            }

            element.Type = type;
            element.RectTransform = rect;
            element.Width = rect != null ? rect.sizeDelta.x : 0;
            element.Height = rect != null ? rect.sizeDelta.y : 0;
            return element;
        }

        public void Dispose()
        {
            RectTransform = null;
            s_pool.Push(this);
        }
    }

    /// <summary>
    /// 行布局数据
    /// </summary>
    public class RichTextRow : IDisposable
    {
        public float Width;
        public float Height;
        public readonly List<RichTextLayoutElement> Elements = new List<RichTextLayoutElement>(8);

        private static readonly Stack<RichTextRow> s_pool = new Stack<RichTextRow>(16);

        public static RichTextRow Create()
        {
            if (s_pool.Count > 0)
            {
                var row = s_pool.Pop();
                row.Width = 0;
                row.Height = 0;
                row.Elements.Clear();
                return row;
            }
            return new RichTextRow();
        }

        public void AddElement(RichTextLayoutElement element)
        {
            Elements.Add(element);
            Width += element.Width;
            if (element.Height > Height)
            {
                Height = element.Height;
            }
        }

        public void Dispose()
        {
            foreach (var element in Elements)
            {
                element.Dispose();
            }
            Elements.Clear();
            s_pool.Push(this);
        }
    }

    /// <summary>
    /// 表情帧动画数据
    /// </summary>
    public class EmojiAnimationData
    {
        public List<string> FrameSprites = new List<string>();
        public bool RaycastEnabled = true;

        public string GetFrame(int index)
        {
            if (FrameSprites.Count == 0) return null;
            return FrameSprites[index % FrameSprites.Count];
        }

        public int FrameCount => FrameSprites.Count;
    }

    /// <summary>
    /// 运行时表情动画实例
    /// </summary>
    internal class EmojiAnimationInstance
    {
        public UIImage TargetImage;
        public EmojiAnimationData AnimationData;
        public int CurrentFrame;
        private string m_lastSpriteName;
        private bool m_isLoading;

        private static readonly Stack<EmojiAnimationInstance> s_pool = new Stack<EmojiAnimationInstance>(16);

        public static EmojiAnimationInstance Create(UIImage targetImage, EmojiAnimationData animData)
        {
            EmojiAnimationInstance instance;
            if (s_pool.Count > 0)
            {
                instance = s_pool.Pop();
            }
            else
            {
                instance = new EmojiAnimationInstance();
            }

            instance.TargetImage = targetImage;
            instance.AnimationData = animData;
            instance.CurrentFrame = 0;
            instance.m_lastSpriteName = null;
            instance.m_isLoading = false;
            return instance;
        }

        public void Dispose()
        {
            TargetImage = null;
            AnimationData = null;
            CurrentFrame = 0;
            m_lastSpriteName = null;
            m_isLoading = false;
            s_pool.Push(this);
        }

        /// <summary>
        /// 尝试获取下一帧信息（无闭包版本）
        /// </summary>
        /// <param name="image">输出：目标 Image</param>
        /// <param name="spriteName">输出：精灵名称</param>
        /// <returns>是否需要更新精灵</returns>
        public bool TryGetNextFrame(out UIImage image, out string spriteName)
        {
            image = null;
            spriteName = null;

            if (AnimationData == null || AnimationData.FrameCount == 0)
                return false;

            // 如果上一帧还在加载中，跳过本次更新
            if (m_isLoading)
                return false;

            spriteName = AnimationData.GetFrame(CurrentFrame);

            // 只在精灵名称实际变化时才需要更新
            if (spriteName != m_lastSpriteName)
            {
                m_lastSpriteName = spriteName;
                m_isLoading = true;
                image = TargetImage;
                CurrentFrame = (CurrentFrame + 1) % AnimationData.FrameCount;
                return true;
            }

            CurrentFrame = (CurrentFrame + 1) % AnimationData.FrameCount;
            return false;
        }

        /// <summary>
        /// 标记加载完成
        /// </summary>
        public void MarkLoadComplete()
        {
            m_isLoading = false;
        }

        public void Reset()
        {
            CurrentFrame = 0;
            m_lastSpriteName = null;
            m_isLoading = false;
        }
    }

    /// <summary>
    /// 链接点击处理器组件（避免 Lambda 闭包）
    /// </summary>
    internal class LinkClickHandler : MonoBehaviour
    {
        public LinkData Data;
        public Action<LinkData> Callback;

        public void OnClick()
        {
            if (Data != null)
            {
                Callback?.Invoke(Data);
            }
        }

        public void Clear()
        {
            Data = null;
            Callback = null;
        }
    }
}
