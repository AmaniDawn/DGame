using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextShadowExtend
    {
        [SerializeField] private bool m_isUseTextShadow;
        [SerializeField] private Color m_shadowTopLeftColor = Color.black;
        [SerializeField] private Color m_shadowTopRightColor = Color.black;
        [SerializeField] private Color m_shadowBottomLeftColor = Color.black;
        [SerializeField] private Color m_shadowBottomRightColor = Color.black;
        [SerializeField] private Vector2 m_effectDistance = new Vector2(1f, -1f);
        private const float MAX_EFFECT_DISTANCE = 600f;
        private Vector2 m_vectorColorOffset = Vector2.zero;
        private Text m_text;

        public bool UseShadow
        {
            get => m_isUseTextShadow;
            set
            {
                m_isUseTextShadow = value;
                Refresh();
            }
        }

        public Color TopLeftColor
        {
            get => m_shadowTopLeftColor;
            set
            {
                m_shadowTopLeftColor = value;
                Refresh();
            }
        }

        public Color TopRightColor
        {
            get => m_shadowTopRightColor;
            set
            {
                m_shadowTopRightColor = value;
                Refresh();
            }
        }

        public Color BottomLeftColor
        {
            get => m_shadowBottomLeftColor;
            set
            {
                m_shadowBottomLeftColor = value;
                Refresh();
            }
        }

        public Color BottomRightColor
        {
            get => m_shadowBottomRightColor;
            set
            {
                m_shadowBottomRightColor = value;
                Refresh();
            }
        }

        public Vector2 EffectDistance
        {
            get => m_effectDistance;
            set
            {
                if (m_effectDistance == value)
                {
                    return;
                }

                if (value.x > MAX_EFFECT_DISTANCE)
                {
                    value.x = MAX_EFFECT_DISTANCE;
                }

                if (value.x < -MAX_EFFECT_DISTANCE)
                {
                    value.x = -MAX_EFFECT_DISTANCE;
                }

                if (value.y > MAX_EFFECT_DISTANCE)
                {
                    value.y = MAX_EFFECT_DISTANCE;
                }

                if (value.y < -MAX_EFFECT_DISTANCE)
                {
                    value.y = -MAX_EFFECT_DISTANCE;
                }

                m_effectDistance = value;
                Refresh();
            }
        }

        public void Initialize(Text text)
        {
            m_text = text;
        }

        public void PopulateMesh(VertexHelper vh, RectTransform rectTransform, Color color)
        {
            if (!m_isUseTextShadow)
            {
                return;
            }

            Vector2 min = rectTransform.pivot;
            min.Scale(-rectTransform.rect.size);
            Vector2 max = rectTransform.rect.size + min;
            List<UIVertex> vertexList = ListPool<UIVertex>.Get();
            try
            {
                vh.GetUIVertexStream(vertexList);
                ApplyShadow(vertexList, min, max, color, m_effectDistance.x, m_effectDistance.y);
                vh.Clear();
                vh.AddUIVertexTriangleStream(vertexList);
            }
            finally
            {
                if (vertexList != null)
                {
                    ListPool<UIVertex>.Recycle(vertexList);
                }
            }
        }

        private void ApplyShadow(List<UIVertex> verts, Vector2 min, Vector2 max, Color32 color, float x, float y)
        {
            int end = verts.Count;
            // 扩容顶点数量 把 shadow 的顶点加入到 verts 里
            var neededCapacity = end << 1; // end * 2

            if (verts.Capacity < neededCapacity)
            {
                verts.Capacity = neededCapacity;
            }

            // 预计算避免循环内重复除法
            float invWidth = max.x != min.x ? 1f / (max.x - min.x) : 0f;
            float invHeight = max.y != min.y ? 1f / (max.y - min.y) : 0f;
            float offsetX = m_vectorColorOffset.x;
            float offsetY = m_vectorColorOffset.y;

            for (int i = 0; i < end; i++)
            {
                UIVertex vt = verts[i];
                verts.Add(vt);

                Vector3 vtPos = vt.position;
                vtPos.x += x;
                vtPos.y += y;
                vt.position = vtPos;
                vt.color = RemapColorOptimized(min, invWidth, invHeight, vtPos, offsetX, offsetY);
                verts[i] = vt;
            }
        }

        private Color RemapColor(Vector2 min, Vector2 max, Color32 color, Vector3 vtPos)
        {
            float x01 = max.x == min.x ? 0f : Mathf.Clamp01((vtPos.x - min.x) / (max.x - min.x));
            float y01 = max.y == min.y ? 0f : Mathf.Clamp01((vtPos.y - min.y) / (max.y - min.y));
            x01 -= m_vectorColorOffset.x * (m_vectorColorOffset.x > 0f ? x01 : (1f - x01));
            y01 -= m_vectorColorOffset.y * (m_vectorColorOffset.y > 0f ? y01 : (1f - y01));
            Color newColor = Color.Lerp(
                Color.Lerp(m_shadowBottomLeftColor, m_shadowBottomRightColor, x01),
                Color.Lerp(m_shadowTopLeftColor, m_shadowTopRightColor, x01),
                y01
            );
            //使用全新颜色 不继承原有的
            return newColor;
            //return color * newColor;
        }

        // 优化版本：预计算除法，手动插值避免Color.Lerp
        private Color RemapColorOptimized(Vector2 min, float invWidth, float invHeight, Vector3 vtPos, float offsetX, float offsetY)
        {
            float x01 = Mathf.Clamp01((vtPos.x - min.x) * invWidth);
            float y01 = Mathf.Clamp01((vtPos.y - min.y) * invHeight);

            x01 -= offsetX * (offsetX > 0f ? x01 : (1f - x01));
            y01 -= offsetY * (offsetY > 0f ? y01 : (1f - y01));

            // 手动双线性插值，避免3次Color.Lerp调用
            float invX = 1f - x01;
            float invY = 1f - y01;

            // 双线性插值：Lerp(Lerp(BL, BR, x), Lerp(TL, TR, x), y)
            Color result;
            result.r = (m_shadowBottomLeftColor.r * invX + m_shadowBottomRightColor.r * x01) * invY +
                       (m_shadowTopLeftColor.r * invX + m_shadowTopRightColor.r * x01) * y01;
            result.g = (m_shadowBottomLeftColor.g * invX + m_shadowBottomRightColor.g * x01) * invY +
                       (m_shadowTopLeftColor.g * invX + m_shadowTopRightColor.g * x01) * y01;
            result.b = (m_shadowBottomLeftColor.b * invX + m_shadowBottomRightColor.b * x01) * invY +
                       (m_shadowTopLeftColor.b * invX + m_shadowTopRightColor.b * x01) * y01;
            result.a = (m_shadowBottomLeftColor.a * invX + m_shadowBottomRightColor.a * x01) * invY +
                       (m_shadowTopLeftColor.a * invX + m_shadowTopRightColor.a * x01) * y01;

            return result;
        }

        public void SetShadowColor(Color32 topLeftColor, Color32 topRightColor, Color32 bottomLeftColor, Color32 bottomRightColor)
        {
            m_shadowTopLeftColor = topLeftColor;
            m_shadowTopRightColor = topRightColor;
            m_shadowBottomLeftColor = bottomLeftColor;
            m_shadowBottomRightColor = bottomRightColor;
            Refresh();
        }

        public void Refresh()
        {
            m_text?.SetVerticesDirty();
        }
    }
}