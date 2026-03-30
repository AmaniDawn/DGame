using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [System.Serializable]
    public class UITextVertexColorExtend
    {
        public enum ColorFilterType
        {
            /// <summary>
            /// 基础色加上新颜色
            /// </summary>
            Additive,

            /// <summary>
            /// 颜色叠加
            /// </summary>
            Overlap,
        }

        [SerializeField] private bool m_isUseVertexColor = false;
        [SerializeField] private ColorFilterType m_colorFilterType = ColorFilterType.Overlap;
        [SerializeField] private Color m_vertexTopLeftColor = Color.white;
        [SerializeField] private Color m_vertexTopRightColor = Color.white;
        [SerializeField] private Color m_vertexBottomLeftColor = Color.white;
        [SerializeField] private Color m_vertexBottomRightColor = Color.white;
        [SerializeField] private Vector2 m_vertexColorOffset = Vector2.zero;

        public bool UseVertexColor { get => m_isUseVertexColor;  set => m_isUseVertexColor = value;  }
        public ColorFilterType VertexColorFilterType { get => m_colorFilterType; set => m_colorFilterType = value; }
        public Color VertexTopLeftColor { get => m_vertexTopLeftColor; set => m_vertexTopLeftColor = value; }
        public Color VertexTopRightColor { get => m_vertexTopRightColor; set => m_vertexTopRightColor = value; }
        public Color VertexBottomLeftColor { get => m_vertexBottomLeftColor; set => m_vertexBottomLeftColor = value; }
        public Color VertexBottomRightColor { get => m_vertexBottomRightColor; set => m_vertexBottomRightColor = value; }
        public Vector2 VertexColorOffset { get => m_vertexColorOffset; set => m_vertexColorOffset = value; }

        // 缓存顶点，避免循环内分配
        private UIVertex m_cachedVertex;

        public void PopulateMesh(VertexHelper toFill, RectTransform rectTransform, Color color)
        {
            if (!m_isUseVertexColor)
            {
                return;
            }

            // 计算UI元素的边界范围 min:左下角坐标 max:右上角坐标
            Vector2 min = rectTransform.pivot;
            min.Scale(-rectTransform.rect.size);
            Vector2 max = rectTransform.rect.size + min;
            int cnt = toFill.currentVertCount;

            // 预计算避免重复计算
            float invWidth = max.x != min.x ? 1f / (max.x - min.x) : 0f;
            float invHeight = max.y != min.y ? 1f / (max.y - min.y) : 0f;

            for (int i = 0; i < cnt; i++)
            {
                toFill.PopulateUIVertex(ref m_cachedVertex, i);
                m_cachedVertex.color = RemapColorOptimized(min, invWidth, invHeight, color, m_cachedVertex.position);
                toFill.SetUIVertex(m_cachedVertex, i);
            }
        }

        private Color RemapColor(Vector2 min, Vector2 max, Color color, Vector2 pos)
        {
            float x01 = max.x == min.x ? 0f : Mathf.Clamp01((pos.x - min.x) / (max.x - min.x));
            float y01 = max.y == min.y ? 0f : Mathf.Clamp01((pos.y - min.y) / (max.y - min.y));
            x01 -= VertexColorOffset.x * (VertexColorOffset.x > 0f ? x01 : (1f - x01));
            y01 -= VertexColorOffset.y * (VertexColorOffset.y > 0f ? y01 : (1f - y01));
            Color newColor =
                Color.Lerp(
                Color.Lerp(VertexBottomLeftColor, VertexBottomRightColor, x01),
                Color.Lerp(VertexTopLeftColor, VertexTopRightColor, x01), y01
                );
            switch (VertexColorFilterType)
            {
                case ColorFilterType.Overlap:
                    float a = Mathf.Max(newColor.a, color.a);
                    newColor = Color.Lerp(color, newColor, newColor.a);
                    newColor.a = a;
                    return newColor;

                case ColorFilterType.Additive:
                default:
                    return color + newColor;
            }
        }

        // 优化版本：预计算除法，减少重复计算
        private Color RemapColorOptimized(Vector2 min, float invWidth, float invHeight, Color color, Vector2 pos)
        {
            float x01 = Mathf.Clamp01((pos.x - min.x) * invWidth);
            float y01 = Mathf.Clamp01((pos.y - min.y) * invHeight);

            float offsetX = m_vertexColorOffset.x;
            float offsetY = m_vertexColorOffset.y;
            x01 -= offsetX * (offsetX > 0f ? x01 : (1f - x01));
            y01 -= offsetY * (offsetY > 0f ? y01 : (1f - y01));

            // 手动插值避免多次Color.Lerp调用
            float invX = 1f - x01;
            float invY = 1f - y01;

            // 双线性插值：一次计算完成
            Color newColor;
            newColor.r = (m_vertexBottomLeftColor.r * invX + m_vertexBottomRightColor.r * x01) * invY +
                         (m_vertexTopLeftColor.r * invX + m_vertexTopRightColor.r * x01) * y01;
            newColor.g = (m_vertexBottomLeftColor.g * invX + m_vertexBottomRightColor.g * x01) * invY +
                         (m_vertexTopLeftColor.g * invX + m_vertexTopRightColor.g * x01) * y01;
            newColor.b = (m_vertexBottomLeftColor.b * invX + m_vertexBottomRightColor.b * x01) * invY +
                         (m_vertexTopLeftColor.b * invX + m_vertexTopRightColor.b * x01) * y01;
            newColor.a = (m_vertexBottomLeftColor.a * invX + m_vertexBottomRightColor.a * x01) * invY +
                         (m_vertexTopLeftColor.a * invX + m_vertexTopRightColor.a * x01) * y01;

            if (m_colorFilterType == ColorFilterType.Overlap)
            {
                float a = newColor.a > color.a ? newColor.a : color.a;
                float t = newColor.a;
                float invT = 1f - t;
                newColor.r = color.r * invT + newColor.r * t;
                newColor.g = color.g * invT + newColor.g * t;
                newColor.b = color.b * invT + newColor.b * t;
                newColor.a = a;
                return newColor;
            }
            else
            {
                newColor.r += color.r;
                newColor.g += color.g;
                newColor.b += color.b;
                newColor.a += color.a;
                return newColor;
            }
        }
    }
}