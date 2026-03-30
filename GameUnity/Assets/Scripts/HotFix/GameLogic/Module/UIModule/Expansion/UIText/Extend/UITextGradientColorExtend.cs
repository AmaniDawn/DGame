using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextGradientColorExtend
    {
        private const int ONE_TEXT_VERTEX = 6;

#pragma warning disable 0414
        [SerializeField]
        private bool m_isUseGradientColor;
        [SerializeField]
        private Color m_colorTop = Color.white;
        [SerializeField]
        private Color m_colorBottom = Color.white;
        [SerializeField]
        private Color m_colorLeft = Color.white;
        [SerializeField]
        private Color m_colorRight = Color.white;
        [SerializeField, Range(-1f, 1f)]
        private float m_gradientOffsetVertical = 0f;
        [SerializeField, Range(-1f, 1f)]
        private float m_gradientOffsetHorizontal = 0f;
        [SerializeField]
        private bool m_splitTextGradient = false;

        public bool isUseGradientColor
        {
            get => m_isUseGradientColor;
            set
            {
                if (m_isUseGradientColor == value)
                {
                    return;
                }
                m_isUseGradientColor = value;
                Refresh();
            }
        }

        public Color colorTop
        {
            get => m_colorTop;
            set { if (m_colorTop != value) { m_colorTop = value; Refresh(); } }
        }

        public Color colorBottom
        {
            get => m_colorBottom;
            set { if (m_colorBottom != value) { m_colorBottom = value; Refresh(); } }
        }

        public Color colorLeft
        {
            get => m_colorLeft;
            set { if (m_colorLeft != value) { m_colorLeft = value; Refresh(); } }
        }

        public Color colorRight
        {
            get => m_colorRight;
            set { if (m_colorRight != value) { m_colorRight = value; Refresh(); } }
        }

        public float gradientOffsetVertical
        {
            get => m_gradientOffsetVertical;
            set { if (m_gradientOffsetVertical != value) { m_gradientOffsetVertical = value; Refresh(); } }
        }

        public float gradientOffsetHorizontal
        {
            get => m_gradientOffsetHorizontal;
            set { if (m_gradientOffsetHorizontal != value) { m_gradientOffsetHorizontal = value; Refresh(); } }
        }

        public bool splitTextGradient
        {
            get => m_splitTextGradient;
            set
            {
                if (m_splitTextGradient != value)
                {
                    m_splitTextGradient = value;
                    Refresh();
                }
            }
        }

        private Text m_text;

#pragma warning disable 0414
        public void Initialize(Text text)
        {
            m_text = text;
        }

#if UNITY_EDITOR
        public void EditorInitialize(Text text)
        {
            m_text = text;
        }
#endif

        public void SetUseGradientColor(bool useGradientColor)
        {
            isUseGradientColor = useGradientColor;
        }

        public void SetGradientColor(Color32 colorTop, Color32 colorBottom, Color32 colorLeft = default, Color32 colorRight = default, float verticalOffset = 0f, float horizontalOffset = 0f, bool splitTextGradient = false)
        {
            SetUseGradientColor(true);
            m_colorTop = colorTop;
            m_colorBottom = colorBottom;
            m_colorLeft = colorLeft;
            m_colorRight = colorRight;
            m_splitTextGradient = splitTextGradient;
            m_gradientOffsetVertical = verticalOffset;
            m_gradientOffsetHorizontal = horizontalOffset;
            Refresh();
        }

        public void Refresh()
        {
            m_text?.SetVerticesDirty();
        }

        #region GradientColor

        public void ModifyMesh(VertexHelper vh)
        {
            if (m_text?.IsActive() == false || !m_isUseGradientColor)
            {
                return;
            }

            List<UIVertex> vList = ListPool<UIVertex>.Get();

            vh.GetUIVertexStream(vList);

            ModifyVertices(vList);

            vh.Clear();
            vh.AddUIVertexTriangleStream(vList);

            if (vList != null)
            {
                ListPool<UIVertex>.Recycle(vList);
            }
        }

        private void ModifyVertices(List<UIVertex> vList)
        {
            if (m_text?.IsActive() == false || vList == null || vList.Count == 0)
            {
                return;
            }

            int count = vList.Count;

            if (m_splitTextGradient)
            {
                // 单字渐变模式：每个字符独立计算
                ModifyVerticesSplitMode(vList, count);
            }
            else
            {
                // 整体渐变模式：一次计算全局min/max
                ModifyVerticesGlobalMode(vList, count);
            }
        }

        // 整体渐变模式 - O(n)复杂度
        private void ModifyVerticesGlobalMode(List<UIVertex> vList, int count)
        {
            // 第一遍：计算全局边界
            float minX = vList[0].position.x;
            float minY = vList[0].position.y;
            float maxX = minX;
            float maxY = minY;

            for (int i = 1; i < count; i++)
            {
                Vector3 pos = vList[i].position;
                if (pos.x < minX) minX = pos.x;
                else if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                else if (pos.y > maxY) maxY = pos.y;
            }

            float invWidth = maxX != minX ? 1f / (maxX - minX) : 0f;
            float invHeight = maxY != minY ? 1f / (maxY - minY) : 0f;

            // 第二遍：应用颜色
            ApplyGradientColor(vList, count, minX, minY, invWidth, invHeight);
        }

        // 单字渐变模式 - O(n)复杂度（优化后）
        private void ModifyVerticesSplitMode(List<UIVertex> vList, int count)
        {
            for (int charStart = 0; charStart < count; charStart += ONE_TEXT_VERTEX)
            {
                int charEnd = charStart + ONE_TEXT_VERTEX;
                if (charEnd > count) charEnd = count;

                // 计算单个字符的边界
                float minX = vList[charStart].position.x;
                float minY = vList[charStart].position.y;
                float maxX = minX;
                float maxY = minY;

                for (int i = charStart + 1; i < charEnd; i++)
                {
                    Vector3 pos = vList[i].position;
                    if (pos.x < minX) minX = pos.x;
                    else if (pos.x > maxX) maxX = pos.x;
                    if (pos.y < minY) minY = pos.y;
                    else if (pos.y > maxY) maxY = pos.y;
                }

                float invWidth = maxX != minX ? 1f / (maxX - minX) : 0f;
                float invHeight = maxY != minY ? 1f / (maxY - minY) : 0f;

                // 应用颜色到当前字符
                for (int i = charStart; i < charEnd; i++)
                {
                    UIVertex vertex = vList[i];
                    vertex.color = CalculateGradientColor(vertex, minX, minY, invWidth, invHeight);
                    vList[i] = vertex;
                }
            }
        }

        // 批量应用渐变色
        private void ApplyGradientColor(List<UIVertex> vList, int count, float minX, float minY, float invWidth, float invHeight)
        {
            for (int i = 0; i < count; i++)
            {
                UIVertex vertex = vList[i];
                vertex.color = CalculateGradientColor(vertex, minX, minY, invWidth, invHeight);
                vList[i] = vertex;
            }
        }

        // 计算单个顶点的渐变色（内联优化）
        private Color CalculateGradientColor(UIVertex vertex, float minX, float minY, float invWidth, float invHeight)
        {
            Vector3 pos = vertex.position;
            float tY = (pos.y - minY) * invHeight + m_gradientOffsetVertical;
            float tX = (pos.x - minX) * invWidth + m_gradientOffsetHorizontal;

            // 手动双线性插值，避免多次Color.Lerp
            float invTY = 1f - tY;
            float invTX = 1f - tX;

            Color colorVertical;
            colorVertical.r = m_colorBottom.r * invTY + m_colorTop.r * tY;
            colorVertical.g = m_colorBottom.g * invTY + m_colorTop.g * tY;
            colorVertical.b = m_colorBottom.b * invTY + m_colorTop.b * tY;
            colorVertical.a = m_colorBottom.a * invTY + m_colorTop.a * tY;

            Color colorHorizontal;
            colorHorizontal.r = m_colorLeft.r * invTX + m_colorRight.r * tX;
            colorHorizontal.g = m_colorLeft.g * invTX + m_colorRight.g * tX;
            colorHorizontal.b = m_colorLeft.b * invTX + m_colorRight.b * tX;
            colorHorizontal.a = m_colorLeft.a * invTX + m_colorRight.a * tX;

            // 颜色相乘
            Color original = vertex.color;
            Color result;
            result.r = original.r * colorVertical.r * colorHorizontal.r;
            result.g = original.g * colorVertical.g * colorHorizontal.g;
            result.b = original.b * colorVertical.b * colorHorizontal.b;
            result.a = original.a * colorVertical.a * colorHorizontal.a;

            return result;
        }

        #endregion
    }
}