using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private abstract class ScrollableDebuggerWindowBase : IDebuggerWindow
        {
            private Vector2 m_scrollPosition = Vector2.zero;
            private int m_rowIndex = 0;

            public virtual void Initialize(params object[] args)
            {
            }

            public virtual void OnEnter()
            {
                m_rowIndex = 0;
            }

            public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
            }

            public virtual void OnDraw()
            {
                DebuggerStyles.Initialize();
                m_rowIndex = 0;

                m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);
                {
                    OnDrawScrollableWindow();
                }
                GUILayout.EndScrollView();
            }

            protected abstract void OnDrawScrollableWindow();

            #region 绘制辅助方法

            /// <summary>
            /// 绘制区段标题
            /// </summary>
            protected static void DrawSectionTitle(string title)
            {
                GUILayout.Space(4);
                DebuggerStyles.DrawSectionHeader($"<b>{title}</b>");
            }

            /// <summary>
            /// 绘制属性项（带交替背景和复制功能）
            /// </summary>
            protected void DrawItem(string title, string content)
            {
                if (DebuggerStyles.DrawPropertyRowAlt(title, content, m_rowIndex % 2 == 1))
                {
                    CopyToClipboard(content);
                }
                m_rowIndex++;
            }

            /// <summary>
            /// 绘制属性项（带tooltip、交替背景和复制功能）
            /// </summary>
            protected void DrawItem(string title, string content, string tooltip)
            {
                if (DebuggerStyles.DrawPropertyRowAlt(title, content, m_rowIndex % 2 == 1, tooltip))
                {
                    CopyToClipboard(content);
                }
                m_rowIndex++;
            }

            /// <summary>
            /// 绘制带颜色的属性项
            /// </summary>
            protected void DrawItemColored(string title, string content, Color32 color)
            {
                string coloredContent = DebuggerStyles.ColorText(content, color);
                if (DebuggerStyles.DrawPropertyRowAlt(title, coloredContent, m_rowIndex % 2 == 1))
                {
                    CopyToClipboard(content);
                }
                m_rowIndex++;
            }

            /// <summary>
            /// 开始一个内容面板
            /// </summary>
            protected static void BeginPanel()
            {
                DebuggerStyles.BeginPanel();
            }

            /// <summary>
            /// 结束内容面板
            /// </summary>
            protected static void EndPanel()
            {
                DebuggerStyles.EndPanel();
            }

            /// <summary>
            /// 绘制分隔线
            /// </summary>
            protected static void DrawSeparator()
            {
                DebuggerStyles.DrawSeparator();
            }

            /// <summary>
            /// 绘制按钮
            /// </summary>
            protected static bool DrawButton(string text, float height = DebuggerStyles.ButtonHeight)
            {
                return GUILayout.Button(text, DebuggerStyles.ButtonStyle, GUILayout.Height(height));
            }

            /// <summary>
            /// 绘制一组水平按钮
            /// </summary>
            protected static int DrawHorizontalButtons(string[] texts, float height = DebuggerStyles.ButtonHeight)
            {
                int clickedIndex = -1;
                GUILayout.BeginHorizontal();
                for (int i = 0; i < texts.Length; i++)
                {
                    if (GUILayout.Button(texts[i], DebuggerStyles.ButtonStyle, GUILayout.Height(height)))
                    {
                        clickedIndex = i;
                    }
                }
                GUILayout.EndHorizontal();
                return clickedIndex;
            }

            /// <summary>
            /// 绘制表格头部
            /// </summary>
            protected static void DrawTableHeader(params (string title, float width)[] columns)
            {
                DebuggerStyles.DrawTableHeader(columns);
            }

            /// <summary>
            /// 绘制表格行
            /// </summary>
            protected void DrawTableRow(bool isAlt, params (string value, float width)[] columns)
            {
                GUILayout.BeginHorizontal(isAlt ? DebuggerStyles.TableRowAltStyle : DebuggerStyles.TableRowStyle);
                foreach (var col in columns)
                {
                    if (col.width > 0)
                    {
                        GUILayout.Label(col.value, DebuggerStyles.RichLabelStyle, GUILayout.Width(col.width));
                    }
                    else
                    {
                        GUILayout.Label(col.value, DebuggerStyles.RichLabelStyle);
                    }
                }
                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// 绘制信息提示
            /// </summary>
            protected static void DrawInfoMessage(string message)
            {
                GUILayout.Space(8);
                GUILayout.Label(DebuggerStyles.ColorText(message, DebuggerStyles.TextSecondaryColor),
                    DebuggerStyles.CenteredLabelStyle);
                GUILayout.Space(8);
            }

            /// <summary>
            /// 绘制错误提示
            /// </summary>
            protected static void DrawErrorMessage(string message)
            {
                GUILayout.Space(8);
                GUILayout.Label(DebuggerStyles.ColorText(message, DebuggerStyles.ErrorColor),
                    DebuggerStyles.CenteredLabelStyle);
                GUILayout.Space(8);
            }

            /// <summary>
            /// 重置行索引（用于新区段）
            /// </summary>
            protected void ResetRowIndex()
            {
                m_rowIndex = 0;
            }

            #endregion

            #region 实用方法

            protected static string GetByteLengthString(long byteLength)
            {
                if (byteLength < 1024L) // 2 ^ 10
                {
                    return Utility.StringUtil.Format("{0} Bytes", byteLength);
                }

                if (byteLength < 1048576L) // 2 ^ 20
                {
                    return Utility.StringUtil.Format("{0:F2} KB", byteLength / 1024f);
                }

                if (byteLength < 1073741824L) // 2 ^ 30
                {
                    return Utility.StringUtil.Format("{0:F2} MB", byteLength / 1048576f);
                }

                if (byteLength < 1099511627776L) // 2 ^ 40
                {
                    return Utility.StringUtil.Format("{0:F2} GB", byteLength / 1073741824f);
                }

                if (byteLength < 1125899906842624L) // 2 ^ 50
                {
                    return Utility.StringUtil.Format("{0:F2} TB", byteLength / 1099511627776f);
                }

                if (byteLength < 1152921504606846976L) // 2 ^ 60
                {
                    return Utility.StringUtil.Format("{0:F2} PB", byteLength / 1125899906842624f);
                }

                return Utility.StringUtil.Format("{0:F2} EB", byteLength / 1152921504606846976f);
            }

            #endregion

            public virtual void OnExit()
            {
            }

            public virtual void OnDestroy()
            {
            }
        }
    }
}
