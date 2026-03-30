using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 描边材质缓存管理器 - 相同Shader的材质复用，减少材质实例和DrawCall
    /// </summary>
    public static class OutlineMaterialCache
    {
        private static readonly Dictionary<string, MaterialCacheEntry> s_materialCache = new Dictionary<string, MaterialCacheEntry>();
        private static Material s_baseMaterial;

        private class MaterialCacheEntry
        {
            public Material Material;
            public int ReferenceCount;
        }

        /// <summary>
        /// UIText 预制材质在 YooAsset 中的地址
        /// 对应文件路径: Assets/BundleAssets/Materials/UGUIPro_UIText.mat
        /// Addressable规则: AddressByFileName (地址=文件名，不含扩展名)
        /// </summary>
        private const string UITextMaterialAddress = "UGUIPro_UIText";

        public static Material GetOrCreateMaterial(string shaderName, Texture fontTexture)
        {
            string cacheKey = fontTexture != null ? $"{shaderName}_{fontTexture.GetInstanceID()}" : shaderName;

            if (s_materialCache.TryGetValue(cacheKey, out var entry))
            {
                entry.ReferenceCount++;
                return entry.Material;
            }

            // 使用 ResourceModule 加载预制材质
            if (s_baseMaterial == null)
            {
                try
                {
                    s_baseMaterial = GameModule.ResourceModule.LoadAsset<Material>(UITextMaterialAddress);
                }
                catch (System.Exception e)
                {
                    DGame.DLogger.Error($"[OutlineMaterialCache] Failed to load material from YooAsset (address: {UITextMaterialAddress}): {e.Message}");
                    return null;
                }
            }

            if (s_baseMaterial == null)
            {
                DGame.DLogger.Error($"[OutlineMaterialCache] Material not found from YooAsset address: {UITextMaterialAddress}");
                return null;
            }

            // 为每个字体纹理创建材质实例
            var material = new Material(s_baseMaterial);
            if (fontTexture != null)
            {
                material.mainTexture = fontTexture;
            }

            s_materialCache[cacheKey] = new MaterialCacheEntry
            {
                Material = material,
                ReferenceCount = 1
            };

            return material;
        }

        public static void ReleaseMaterial(string shaderName, Texture fontTexture)
        {
            string cacheKey = fontTexture != null ? $"{shaderName}_{fontTexture.GetInstanceID()}" : shaderName;

            if (s_materialCache.TryGetValue(cacheKey, out var entry))
            {
                entry.ReferenceCount--;
                if (entry.ReferenceCount <= 0)
                {
                    if (entry.Material != null)
                    {
                        Object.Destroy(entry.Material);
                    }
                    s_materialCache.Remove(cacheKey);
                }
            }
        }

        public static void ClearCache()
        {
            foreach (var entry in s_materialCache.Values)
            {
                if (entry.Material != null)
                {
                    Object.Destroy(entry.Material);
                }
            }
            s_materialCache.Clear();
        }
    }

    /// <summary>
    /// UI文本Shader描边扩展
    /// 注意：如需非Shader描边，请直接使用Unity内置的Outline组件
    /// </summary>
    [Serializable]
    public class UITextOutlineExtend
    {
        [SerializeField] private bool m_isUseTextOutline;
        [SerializeField, Range(1, 10)] private int m_outLineWidth = 1;
        [SerializeField] private Color m_outLineColor = Color.black;
        [SerializeField] private Camera m_camera;
        [SerializeField, Range(0f, 1f)] private float m_alpha = 1f;

        private List<UIVertex> m_vertexList;
        private Text m_text;
        private const string OutLineShaderName = "UIText";
        private bool m_initParams;
        private Texture m_cachedFontTexture;
        private bool m_materialFromCache;

        public bool UseTextOutline
        {
            get => m_isUseTextOutline;
            set
            {
                if (m_isUseTextOutline == value) return;
                m_isUseTextOutline = value;

                if (value)
                {
                    UpdateOutLineInfos();
                }
                Refresh();
            }
        }

        public int outLineWidth
        {
            get => m_outLineWidth;
            set
            {
                if (m_outLineWidth == value) return;
                m_outLineWidth = value;
                Refresh();
            }
        }

        public Color outLineColor
        {
            get => m_outLineColor;
            set
            {
                if (m_outLineColor == value) return;
                m_outLineColor = value;
                Refresh();
            }
        }

        public float alpha
        {
            get => m_alpha;
            set
            {
                if (Mathf.Approximately(m_alpha, value)) return;
                m_alpha = value;
                Refresh();
            }
        }

        public void SetUseTextOutline(bool value)
        {
            UseTextOutline = value;
        }

        public void SetOutLineColor(Color outlineColor)
        {
            m_outLineColor = outlineColor;
            UseTextOutline = true;
            UpdateOutLineInfos();
            Refresh();
        }

        public void SetOutLineWidth(int outlineWidth)
        {
            m_outLineWidth = outlineWidth;
            UseTextOutline = true;
            Refresh();
        }

        public void SetAlpha(float setAlphaValue)
        {
            m_alpha = setAlphaValue;
            m_outLineColor.a = setAlphaValue;
            UseTextOutline = true;
            Refresh();
        }

        public void Initialize(Text text)
        {
            m_text = text;

            if (m_camera == null)
            {
                m_camera = Camera.main;
                if (m_camera == null)
                {
#if UNITY_6000_3_OR_NEWER
                    m_camera = Object.FindFirstObjectByType<Camera>();
#else
                    m_camera = Object.FindObjectOfType<Camera>();
#endif
                }
            }

            Refresh();
        }

#if UNITY_EDITOR
        public void EditorInitialize(Text text)
        {
            m_text = text;
            Refresh();
        }
#endif

        public void Refresh()
        {
            m_text?.SetVerticesDirty();
        }

        #region ShaderOutLine

        public void ModifyMesh(VertexHelper vh)
        {
            if (m_text?.IsActive() == false || !m_isUseTextOutline)
            {
                return;
            }

            if (m_vertexList == null)
            {
                m_vertexList = ListPool<UIVertex>.Get();
            }

            m_vertexList.Clear();
            vh.GetUIVertexStream(m_vertexList);
            ProcessVertices();

            vh.Clear();
            vh.AddUIVertexTriangleStream(m_vertexList);
        }

        private void ProcessVertices()
        {
            int count = m_vertexList.Count - 3;
            if (count < 0) return;

            Vector2 col_rg = new Vector2(m_outLineColor.r, m_outLineColor.g);
            Vector4 col_ba = new Vector4(0, 0, m_outLineColor.b, m_outLineColor.a);
            Vector3 normal = new Vector3(0, 0, m_outLineWidth);
            float outlineWidth = m_outLineWidth;

            for (int i = 0; i <= count; i += 3)
            {
                var v1 = m_vertexList[i];
                var v2 = m_vertexList[i + 1];
                var v3 = m_vertexList[i + 2];

                Vector3 p1 = v1.position;
                Vector3 p2 = v2.position;
                Vector3 p3 = v3.position;

                float minX = p1.x < p2.x ? (p1.x < p3.x ? p1.x : p3.x) : (p2.x < p3.x ? p2.x : p3.x);
                float minY = p1.y < p2.y ? (p1.y < p3.y ? p1.y : p3.y) : (p2.y < p3.y ? p2.y : p3.y);
                float maxX = p1.x > p2.x ? (p1.x > p3.x ? p1.x : p3.x) : (p2.x > p3.x ? p2.x : p3.x);
                float maxY = p1.y > p2.y ? (p1.y > p3.y ? p1.y : p3.y) : (p2.y > p3.y ? p2.y : p3.y);

                float posCenterX = (minX + maxX) * 0.5f;
                float posCenterY = (minY + maxY) * 0.5f;

                Vector2 pos1 = new Vector2(p1.x, p1.y);
                Vector2 pos2 = new Vector2(p2.x, p2.y);
                Vector2 pos3 = new Vector2(p3.x, p3.y);

                Vector2 diff21 = pos2 - pos1;
                Vector2 diff32 = pos3 - pos2;

                Vector2 triX, triY;
                Vector4 uvX, uvY;

                float dot21 = Mathf.Abs(diff21.x);
                float dot32 = Mathf.Abs(diff32.x);
                float mag21 = diff21.magnitude;
                float mag32 = diff32.magnitude;

                if (mag21 > 0.0001f) dot21 /= mag21;
                if (mag32 > 0.0001f) dot32 /= mag32;

                if (dot21 > dot32)
                {
                    triX = diff21;
                    triY = diff32;
                    uvX = v2.uv0 - v1.uv0;
                    uvY = v3.uv0 - v2.uv0;
                }
                else
                {
                    triX = diff32;
                    triY = diff21;
                    uvX = v3.uv0 - v2.uv0;
                    uvY = v2.uv0 - v1.uv0;
                }

                Vector4 uv1 = v1.uv0, uv2 = v2.uv0, uv3 = v3.uv0;
                Vector2 uvMin = new Vector2(
                    uv1.x < uv2.x ? (uv1.x < uv3.x ? uv1.x : uv3.x) : (uv2.x < uv3.x ? uv2.x : uv3.x),
                    uv1.y < uv2.y ? (uv1.y < uv3.y ? uv1.y : uv3.y) : (uv2.y < uv3.y ? uv2.y : uv3.y)
                );
                Vector2 uvMax = new Vector2(
                    uv1.x > uv2.x ? (uv1.x > uv3.x ? uv1.x : uv3.x) : (uv2.x > uv3.x ? uv2.x : uv3.x),
                    uv1.y > uv2.y ? (uv1.y > uv3.y ? uv1.y : uv3.y) : (uv2.y > uv3.y ? uv2.y : uv3.y)
                );

                float triXMag = triX.magnitude;
                float triYMag = triY.magnitude;
                float invTriXMag = triXMag > 0.0001f ? 1f / Mathf.Min(triXMag, 18f) : 0f;
                float invTriYMag = triYMag > 0.0001f ? 1f / Mathf.Min(triYMag, 18f) : 0f;
                float triXSign = Vector2.Dot(triX, Vector2.right) > 0 ? 1f : -1f;
                float triYSign = Vector2.Dot(triY, Vector2.up) > 0 ? 1f : -1f;

                v1 = SetVertexData(v1, outlineWidth, posCenterX, posCenterY, uvX, uvY, uvMin, uvMax, invTriXMag, invTriYMag, triXSign, triYSign, col_rg, col_ba, normal);
                v2 = SetVertexData(v2, outlineWidth, posCenterX, posCenterY, uvX, uvY, uvMin, uvMax, invTriXMag, invTriYMag, triXSign, triYSign, col_rg, col_ba, normal);
                v3 = SetVertexData(v3, outlineWidth, posCenterX, posCenterY, uvX, uvY, uvMin, uvMax, invTriXMag, invTriYMag, triXSign, triYSign, col_rg, col_ba, normal);

                m_vertexList[i] = v1;
                m_vertexList[i + 1] = v2;
                m_vertexList[i + 2] = v3;
            }
        }

        private static UIVertex SetVertexData(UIVertex vertex, float outLineWidth,
            float posCenterX, float posCenterY, Vector4 uvX, Vector4 uvY,
            Vector2 uvMin, Vector2 uvMax,
            float invTriXMag, float invTriYMag, float triXSign, float triYSign,
            Vector2 col_rg, Vector4 col_ba, Vector3 normal)
        {
            Vector3 pos = vertex.position;
            float posXOffset = pos.x > posCenterX ? outLineWidth : -outLineWidth;
            float posYOffset = pos.y > posCenterY ? outLineWidth : -outLineWidth;
            pos.x += posXOffset;
            pos.y += posYOffset;
            vertex.position = pos;

            Vector4 uv = vertex.uv0;
            float uvOffsetX = posXOffset * invTriXMag * triXSign;
            float uvOffsetY = posYOffset * invTriYMag * triYSign;
            uv.x += uvX.x * uvOffsetX + uvY.x * uvOffsetY;
            uv.y += uvX.y * uvOffsetX + uvY.y * uvOffsetY;
            vertex.uv0 = uv;

            vertex.uv1 = uvMin;
            vertex.uv2 = uvMax;
            vertex.uv3 = col_rg;
            vertex.tangent = col_ba;
            vertex.normal = normal;

            return vertex;
        }

        #endregion

        #region Material Management

        public void OnDestroy()
        {
            if (m_vertexList != null)
            {
                ListPool<UIVertex>.Recycle(m_vertexList);
                m_vertexList = null;
            }
            ReleaseMaterial();
        }

        public void SetCamera(Camera c)
        {
            if (m_camera == c) return;
            m_camera = c;
        }

        public void UpdateOutLineInfos()
        {
            UpdateOutLineMaterial();
            OpenShaderParams();
        }

        private void UpdateOutLineMaterial()
        {
            if (!m_isUseTextOutline || m_text == null) return;

            Texture fontTexture = null;
            if (m_text.font != null && m_text.font.material != null)
            {
                fontTexture = m_text.font.material.mainTexture;
            }

#if !UNITY_EDITOR
            if (m_text.material == m_text.defaultMaterial)
            {
                var cachedMaterial = OutlineMaterialCache.GetOrCreateMaterial(OutLineShaderName, fontTexture);
                if (cachedMaterial != null)
                {
                    m_text.material = cachedMaterial;
                    m_cachedFontTexture = fontTexture;
                    m_materialFromCache = true;
                }
            }
#else
            if (!Application.isPlaying)
            {
                if (m_text.material == m_text.defaultMaterial)
                {
                    Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/BundleAssets/Materials/UGUIPro_UIText.mat");
                    if (material == null)
                    {
                        DGame.DLogger.Error("Text Out Line Material Not Find Please Check Material Path!");
                    }
                    else
                    {
                        m_text.material = material;
                        m_materialFromCache = false;
                    }
                }
            }
            else
            {
                if (m_text.material == m_text.defaultMaterial)
                {
                    var cachedMaterial = OutlineMaterialCache.GetOrCreateMaterial(OutLineShaderName, fontTexture);
                    if (cachedMaterial != null)
                    {
                        m_text.material = cachedMaterial;
                        m_cachedFontTexture = fontTexture;
                        m_materialFromCache = true;
                    }
                }
            }
#endif
        }

        public void ReleaseMaterial()
        {
            if (m_materialFromCache)
            {
                OutlineMaterialCache.ReleaseMaterial(OutLineShaderName, m_cachedFontTexture);
                m_materialFromCache = false;
                m_cachedFontTexture = null;
            }
        }

        private void OpenShaderParams()
        {
            if (!m_isUseTextOutline || m_text == null || m_initParams) return;

            if (m_text.canvas != null)
            {
                var channels = m_text.canvas.additionalShaderChannels;

                channels |= AdditionalCanvasShaderChannels.TexCoord1;
                channels |= AdditionalCanvasShaderChannels.TexCoord2;
                channels |= AdditionalCanvasShaderChannels.TexCoord3;
                channels |= AdditionalCanvasShaderChannels.Tangent;
                channels |= AdditionalCanvasShaderChannels.Normal;

                m_text.canvas.additionalShaderChannels = channels;
                m_initParams = true;
            }
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (!m_isUseTextOutline) return;
            UpdateOutLineMaterial();
        }
#endif

        #endregion
    }
}