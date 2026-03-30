using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        /// <summary>
        /// Debugger UI 样式管理类
        /// </summary>
        private static class DebuggerStyles
        {
            // 颜色定义
            public static readonly Color PrimaryColor = new Color(0.2f, 0.6f, 0.9f, 1f);           // 主色调 - 蓝色
            public static readonly Color SecondaryColor = new Color(0.3f, 0.7f, 0.5f, 1f);         // 次要色 - 绿色
            public static readonly Color AccentColor = new Color(0.9f, 0.6f, 0.2f, 1f);            // 强调色 - 橙色
            public static readonly Color BackgroundColor = new Color(0.15f, 0.15f, 0.18f, 0.95f);  // 背景色 - 深灰
            public static readonly Color PanelColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);          // 面板色 - 稍浅灰
            public static readonly Color HeaderColor = new Color(0.25f, 0.25f, 0.3f, 1f);          // 标题栏色
            public static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.4f, 1f);          // 边框色
            public static readonly Color TextColor = new Color(0.9f, 0.9f, 0.9f, 1f);              // 文字色
            public static readonly Color TextSecondaryColor = new Color(0.7f, 0.7f, 0.7f, 1f);     // 次要文字色
            public static readonly Color SecondaryTextColor = TextSecondaryColor;                   // 别名
            public static readonly Color SuccessColor = new Color(0.4f, 0.8f, 0.4f, 1f);           // 成功色 - 绿色
            public static readonly Color WarningColor = new Color(0.95f, 0.75f, 0.2f, 1f);         // 警告色 - 黄色
            public static readonly Color ErrorColor = new Color(0.9f, 0.35f, 0.35f, 1f);           // 错误色 - 红色
            public static readonly Color FatalColor = new Color(0.7f, 0.2f, 0.3f, 1f);             // 致命色 - 深红

            // 尺寸定义
            public const float TitleWidth = 220f;
            public const float ValueMinWidth = 150f;
            public const float ButtonHeight = 32f;
            public const float SmallButtonHeight = 26f;
            public const float LargeButtonHeight = 40f;
            public const float RowHeight = 24f;
            public const float HeaderHeight = 28f;
            public const float Padding = 8f;
            public const float SmallPadding = 4f;
            public const float LargePadding = 12f;
            public const float BorderRadius = 4f;
            public const float IconSize = 20f;
            public const float TabHeight = 32f;

            // 缓存的 GUIStyle
            private static GUIStyle _titleStyle;
            private static GUIStyle _sectionHeaderStyle;
            private static GUIStyle _labelStyle;
            private static GUIStyle _valueStyle;
            private static GUIStyle _buttonStyle;
            private static GUIStyle _toolbarButtonStyle;
            private static GUIStyle _toggleStyle;
            private static GUIStyle _boxStyle;
            private static GUIStyle _panelStyle;
            private static GUIStyle _headerBoxStyle;
            private static GUIStyle _richLabelStyle;
            private static GUIStyle _centeredLabelStyle;
            private static GUIStyle _tableHeaderStyle;
            private static GUIStyle _tableRowStyle;
            private static GUIStyle _tableRowAltStyle;
            private static GUIStyle _logInfoStyle;
            private static GUIStyle _logWarningStyle;
            private static GUIStyle _logErrorStyle;
            private static GUIStyle _logFatalStyle;
            private static GUIStyle _iconButtonStyle;
            private static GUIStyle _fpsButtonStyle;
            private static GUIStyle _primaryBoxStyle;
            private static GUIStyle _warningBoxStyle;

            private static Texture2D _whiteTexture;
            private static Texture2D _panelTexture;
            private static Texture2D _headerTexture;
            private static Texture2D _buttonNormalTexture;
            private static Texture2D _buttonHoverTexture;
            private static Texture2D _buttonActiveTexture;

            /// <summary>
            /// 初始化样式（需要在OnGUI中调用一次）
            /// </summary>
            public static void Initialize()
            {
                if (_whiteTexture == null)
                {
                    CreateTextures();
                }
            }

            private static void CreateTextures()
            {
                _whiteTexture = CreateTexture(2, 2, Color.white);
                _panelTexture = CreateTexture(2, 2, PanelColor);
                _headerTexture = CreateTexture(2, 2, HeaderColor);
                _buttonNormalTexture = CreateTexture(2, 2, new Color(0.3f, 0.3f, 0.35f, 1f));
                _buttonHoverTexture = CreateTexture(2, 2, new Color(0.35f, 0.35f, 0.4f, 1f));
                _buttonActiveTexture = CreateTexture(2, 2, PrimaryColor);
            }

            private static Texture2D CreateTexture(int width, int height, Color color)
            {
                Texture2D texture = new Texture2D(width, height);
                Color[] pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }
                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            }

            /// <summary>
            /// 窗口标题样式
            /// </summary>
            public static GUIStyle TitleStyle
            {
                get
                {
                    if (_titleStyle == null)
                    {
                        _titleStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 16,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(10, 10, 8, 8),
                            margin = new RectOffset(0, 0, 4, 8),
                            richText = true
                        };
                        _titleStyle.normal.textColor = PrimaryColor;
                    }
                    return _titleStyle;
                }
            }

            /// <summary>
            /// 区段标题样式
            /// </summary>
            public static GUIStyle SectionHeaderStyle
            {
                get
                {
                    if (_sectionHeaderStyle == null)
                    {
                        _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 13,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(8, 8, 6, 6),
                            margin = new RectOffset(0, 0, 8, 4),
                            richText = true
                        };
                        _sectionHeaderStyle.normal.textColor = TextColor;
                        _sectionHeaderStyle.normal.background = _headerTexture;
                    }
                    return _sectionHeaderStyle;
                }
            }

            /// <summary>
            /// 标签样式（属性名）
            /// </summary>
            public static GUIStyle LabelStyle
            {
                get
                {
                    if (_labelStyle == null)
                    {
                        _labelStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(8, 4, 4, 4),
                            richText = true,
                            wordWrap = false
                        };
                        _labelStyle.normal.textColor = TextSecondaryColor;
                    }
                    return _labelStyle;
                }
            }

            /// <summary>
            /// 值样式（属性值，可点击复制）
            /// </summary>
            public static GUIStyle ValueStyle
            {
                get
                {
                    if (_valueStyle == null)
                    {
                        _valueStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(4, 8, 4, 4),
                            richText = true,
                            wordWrap = true
                        };
                        _valueStyle.normal.textColor = TextColor;
                        _valueStyle.hover.textColor = PrimaryColor;
                    }
                    return _valueStyle;
                }
            }

            /// <summary>
            /// 按钮样式
            /// </summary>
            public static GUIStyle ButtonStyle
            {
                get
                {
                    if (_buttonStyle == null)
                    {
                        _buttonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter,
                            padding = new RectOffset(12, 12, 6, 6),
                            margin = new RectOffset(4, 4, 4, 4),
                            richText = true
                        };
                        _buttonStyle.normal.textColor = TextColor;
                        _buttonStyle.hover.textColor = Color.white;
                        _buttonStyle.active.textColor = Color.white;
                        _buttonStyle.normal.background = _buttonNormalTexture;
                        _buttonStyle.hover.background = _buttonHoverTexture;
                        _buttonStyle.active.background = _buttonActiveTexture;
                    }
                    return _buttonStyle;
                }
            }

            /// <summary>
            /// 工具栏按钮样式
            /// </summary>
            public static GUIStyle ToolbarButtonStyle
            {
                get
                {
                    if (_toolbarButtonStyle == null)
                    {
                        _toolbarButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 11,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter,
                            padding = new RectOffset(8, 8, 4, 4),
                            margin = new RectOffset(2, 2, 2, 2),
                            richText = true,
                            fixedHeight = TabHeight
                        };
                        _toolbarButtonStyle.normal.textColor = TextSecondaryColor;
                        _toolbarButtonStyle.hover.textColor = TextColor;
                        _toolbarButtonStyle.active.textColor = Color.white;
                        _toolbarButtonStyle.onNormal.textColor = Color.white;
                        _toolbarButtonStyle.onNormal.background = _buttonActiveTexture;
                    }
                    return _toolbarButtonStyle;
                }
            }

            /// <summary>
            /// 切换开关样式
            /// </summary>
            public static GUIStyle ToggleStyle
            {
                get
                {
                    if (_toggleStyle == null)
                    {
                        _toggleStyle = new GUIStyle(GUI.skin.toggle)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(20, 4, 4, 4),
                            margin = new RectOffset(4, 4, 2, 2),
                            richText = true
                        };
                        _toggleStyle.normal.textColor = TextColor;
                        _toggleStyle.onNormal.textColor = PrimaryColor;
                    }
                    return _toggleStyle;
                }
            }

            /// <summary>
            /// 盒子样式
            /// </summary>
            public static GUIStyle BoxStyle
            {
                get
                {
                    if (_boxStyle == null)
                    {
                        _boxStyle = new GUIStyle(GUI.skin.box)
                        {
                            padding = new RectOffset(8, 8, 8, 8),
                            margin = new RectOffset(4, 4, 4, 4)
                        };
                        _boxStyle.normal.background = _panelTexture;
                    }
                    return _boxStyle;
                }
            }

            /// <summary>
            /// 面板样式
            /// </summary>
            public static GUIStyle PanelStyle
            {
                get
                {
                    if (_panelStyle == null)
                    {
                        _panelStyle = new GUIStyle()
                        {
                            padding = new RectOffset(10, 10, 8, 8),
                            margin = new RectOffset(0, 0, 4, 8)
                        };
                        _panelStyle.normal.background = _panelTexture;
                    }
                    return _panelStyle;
                }
            }

            /// <summary>
            /// 标题区域样式
            /// </summary>
            public static GUIStyle HeaderBoxStyle
            {
                get
                {
                    if (_headerBoxStyle == null)
                    {
                        _headerBoxStyle = new GUIStyle()
                        {
                            padding = new RectOffset(8, 8, 6, 6),
                            margin = new RectOffset(0, 0, 0, 4)
                        };
                        _headerBoxStyle.normal.background = _headerTexture;
                    }
                    return _headerBoxStyle;
                }
            }

            /// <summary>
            /// 富文本标签样式
            /// </summary>
            public static GUIStyle RichLabelStyle
            {
                get
                {
                    if (_richLabelStyle == null)
                    {
                        _richLabelStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(4, 4, 2, 2),
                            richText = true,
                            wordWrap = true
                        };
                        _richLabelStyle.normal.textColor = TextColor;
                    }
                    return _richLabelStyle;
                }
            }

            /// <summary>
            /// 居中标签样式
            /// </summary>
            public static GUIStyle CenteredLabelStyle
            {
                get
                {
                    if (_centeredLabelStyle == null)
                    {
                        _centeredLabelStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleCenter,
                            padding = new RectOffset(4, 4, 4, 4),
                            richText = true
                        };
                        _centeredLabelStyle.normal.textColor = TextColor;
                    }
                    return _centeredLabelStyle;
                }
            }

            /// <summary>
            /// 表格标题样式
            /// </summary>
            public static GUIStyle TableHeaderStyle
            {
                get
                {
                    if (_tableHeaderStyle == null)
                    {
                        _tableHeaderStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(8, 8, 6, 6),
                            richText = true
                        };
                        _tableHeaderStyle.normal.textColor = PrimaryColor;
                        _tableHeaderStyle.normal.background = _headerTexture;
                    }
                    return _tableHeaderStyle;
                }
            }

            /// <summary>
            /// 表格行样式
            /// </summary>
            public static GUIStyle TableRowStyle
            {
                get
                {
                    if (_tableRowStyle == null)
                    {
                        _tableRowStyle = new GUIStyle()
                        {
                            padding = new RectOffset(8, 8, 4, 4),
                            margin = new RectOffset(0, 0, 0, 1)
                        };
                        _tableRowStyle.normal.background = CreateTexture(2, 2, new Color(0.18f, 0.18f, 0.22f, 0.8f));
                    }
                    return _tableRowStyle;
                }
            }

            /// <summary>
            /// 表格交替行样式
            /// </summary>
            public static GUIStyle TableRowAltStyle
            {
                get
                {
                    if (_tableRowAltStyle == null)
                    {
                        _tableRowAltStyle = new GUIStyle()
                        {
                            padding = new RectOffset(8, 8, 4, 4),
                            margin = new RectOffset(0, 0, 0, 1)
                        };
                        _tableRowAltStyle.normal.background = CreateTexture(2, 2, new Color(0.22f, 0.22f, 0.26f, 0.8f));
                    }
                    return _tableRowAltStyle;
                }
            }

            /// <summary>
            /// Info日志样式
            /// </summary>
            public static GUIStyle LogInfoStyle
            {
                get
                {
                    if (_logInfoStyle == null)
                    {
                        _logInfoStyle = new GUIStyle(GUI.skin.toggle)
                        {
                            fontSize = 11,
                            alignment = TextAnchor.MiddleLeft,
                            padding = new RectOffset(20, 4, 3, 3),
                            margin = new RectOffset(0, 0, 1, 1),
                            richText = true,
                            wordWrap = false
                        };
                        _logInfoStyle.normal.textColor = TextColor;
                        _logInfoStyle.onNormal.textColor = TextColor;
                        _logInfoStyle.normal.background = CreateTexture(2, 2, new Color(0.18f, 0.18f, 0.22f, 0.6f));
                        _logInfoStyle.onNormal.background = CreateTexture(2, 2, new Color(0.2f, 0.4f, 0.6f, 0.6f));
                    }
                    return _logInfoStyle;
                }
            }

            /// <summary>
            /// 图标按钮样式
            /// </summary>
            public static GUIStyle IconButtonStyle
            {
                get
                {
                    if (_iconButtonStyle == null)
                    {
                        _iconButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 14,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter,
                            padding = new RectOffset(4, 4, 4, 4),
                            margin = new RectOffset(2, 2, 2, 2),
                            fixedWidth = 28,
                            fixedHeight = 28
                        };
                    }
                    return _iconButtonStyle;
                }
            }

            /// <summary>
            /// FPS按钮样式（漂浮图标）
            /// </summary>
            public static GUIStyle FpsButtonStyle
            {
                get
                {
                    if (_fpsButtonStyle == null)
                    {
                        _fpsButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 14,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.MiddleCenter,
                            padding = new RectOffset(8, 8, 8, 8),
                            margin = new RectOffset(4, 4, 4, 4),
                            richText = true
                        };
                        _fpsButtonStyle.normal.background = CreateTexture(2, 2, new Color(0.15f, 0.15f, 0.2f, 0.9f));
                        _fpsButtonStyle.hover.background = CreateTexture(2, 2, new Color(0.2f, 0.2f, 0.25f, 0.95f));
                        _fpsButtonStyle.active.background = CreateTexture(2, 2, PrimaryColor);
                    }
                    return _fpsButtonStyle;
                }
            }

            /// <summary>
            /// 主色调高亮行样式
            /// </summary>
            public static GUIStyle PrimaryBoxStyle
            {
                get
                {
                    if (_primaryBoxStyle == null)
                    {
                        _primaryBoxStyle = new GUIStyle()
                        {
                            padding = new RectOffset(8, 8, 4, 4),
                            margin = new RectOffset(0, 0, 0, 1)
                        };
                        _primaryBoxStyle.normal.background = CreateTexture(2, 2, new Color(0.2f, 0.4f, 0.6f, 0.5f));
                    }
                    return _primaryBoxStyle;
                }
            }

            /// <summary>
            /// 警告高亮行样式
            /// </summary>
            public static GUIStyle WarningBoxStyle
            {
                get
                {
                    if (_warningBoxStyle == null)
                    {
                        _warningBoxStyle = new GUIStyle()
                        {
                            padding = new RectOffset(8, 8, 4, 4),
                            margin = new RectOffset(0, 0, 0, 1)
                        };
                        _warningBoxStyle.normal.background = CreateTexture(2, 2, new Color(0.6f, 0.5f, 0.2f, 0.4f));
                    }
                    return _warningBoxStyle;
                }
            }

            #region 辅助绘制方法

            /// <summary>
            /// 绘制区段标题
            /// </summary>
            public static void DrawSectionHeader(string title)
            {
                GUILayout.Label(title, SectionHeaderStyle);
            }

            /// <summary>
            /// 绘制属性行（标题 + 值）
            /// </summary>
            public static bool DrawPropertyRow(string title, string value, string tooltip = null)
            {
                bool clicked = false;
                GUILayout.BeginHorizontal(TableRowStyle);
                {
                    GUIContent labelContent = tooltip != null ? new GUIContent(title, tooltip) : new GUIContent(title);
                    GUILayout.Label(labelContent, LabelStyle, GUILayout.Width(TitleWidth));

                    if (GUILayout.Button(value, ValueStyle))
                    {
                        clicked = true;
                    }
                }
                GUILayout.EndHorizontal();
                return clicked;
            }

            /// <summary>
            /// 绘制带交替背景的属性行
            /// </summary>
            public static bool DrawPropertyRowAlt(string title, string value, bool isAlt, string tooltip = null)
            {
                bool clicked = false;
                GUILayout.BeginHorizontal(isAlt ? TableRowAltStyle : TableRowStyle);
                {
                    GUIContent labelContent = tooltip != null ? new GUIContent(title, tooltip) : new GUIContent(title);
                    GUILayout.Label(labelContent, LabelStyle, GUILayout.Width(TitleWidth));

                    if (GUILayout.Button(value, ValueStyle))
                    {
                        clicked = true;
                    }
                }
                GUILayout.EndHorizontal();
                return clicked;
            }

            /// <summary>
            /// 开始一个面板区域
            /// </summary>
            public static void BeginPanel()
            {
                GUILayout.BeginVertical(PanelStyle);
            }

            /// <summary>
            /// 结束面板区域
            /// </summary>
            public static void EndPanel()
            {
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 绘制分隔线
            /// </summary>
            public static void DrawSeparator()
            {
                GUILayout.Space(4);
                Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
                EditorDrawLine(rect, BorderColor);
                GUILayout.Space(4);
            }

            private static void EditorDrawLine(Rect rect, Color color)
            {
                Color oldColor = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(rect, _whiteTexture ?? Texture2D.whiteTexture);
                GUI.color = oldColor;
            }

            /// <summary>
            /// 绘制表格头部
            /// </summary>
            public static void DrawTableHeader(params (string title, float width)[] columns)
            {
                GUILayout.BeginHorizontal(HeaderBoxStyle);
                foreach (var col in columns)
                {
                    if (col.width > 0)
                    {
                        GUILayout.Label($"<b>{col.title}</b>", TableHeaderStyle, GUILayout.Width(col.width));
                    }
                    else
                    {
                        GUILayout.Label($"<b>{col.title}</b>", TableHeaderStyle);
                    }
                }
                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// 获取FPS对应的颜色
            /// </summary>
            public static Color GetFpsColor(float fps)
            {
                if (fps >= 55f) return SuccessColor;
                if (fps >= 30f) return WarningColor;
                return ErrorColor;
            }

            /// <summary>
            /// 格式化为带颜色的文本
            /// </summary>
            public static string ColorText(string text, Color32 color)
            {
                return $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{text}</color>";
            }

            /// <summary>
            /// 格式化为粗体文本
            /// </summary>
            public static string BoldText(string text)
            {
                return $"<b>{text}</b>";
            }

            /// <summary>
            /// 格式化为带颜色的粗体文本
            /// </summary>
            public static string ColorBoldText(string text, Color32 color)
            {
                return $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}><b>{text}</b></color>";
            }

            /// <summary>
            /// 绘制悬停提示（在窗口末尾调用）
            /// </summary>
            public static void DrawTooltip()
            {
                if (!string.IsNullOrEmpty(GUI.tooltip))
                {
                    GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(10, 10, 6, 6),
                        wordWrap = true,
                        richText = true
                    };
                    tooltipStyle.normal.textColor = TextColor;
                    tooltipStyle.normal.background = CreateTexture(2, 2, new Color(0.1f, 0.1f, 0.15f, 0.95f));

                    Vector2 mousePos = Event.current.mousePosition;
                    GUIContent content = new GUIContent(GUI.tooltip);
                    Vector2 size = tooltipStyle.CalcSize(content);

                    // 限制最大宽度
                    if (size.x > 300f)
                    {
                        size.x = 300f;
                        size.y = tooltipStyle.CalcHeight(content, size.x);
                    }

                    // 确保tooltip不超出屏幕
                    float x = mousePos.x + 15f;
                    float y = mousePos.y + 15f;

                    Rect tooltipRect = new Rect(x, y, size.x + 20f, size.y + 12f);
                    GUI.Box(tooltipRect, GUI.tooltip, tooltipStyle);
                }
            }

            #endregion
        }
    }
}
