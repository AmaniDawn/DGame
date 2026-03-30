using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 材质效果类型枚举
    /// </summary>
    [Flags]
    public enum MatEffectType
    {
        /// <summary>
        /// 无效果
        /// </summary>
        None = 0,

        /// <summary>
        /// 灰度效果
        /// </summary>
        Gray = 1 << 0,

        /// <summary>
        /// 圆形遮罩效果
        /// </summary>
        Circle = 1 << 1,
    }

    /// <summary>
    /// UI图片特效组件，支持灰度和圆形遮罩效果
    /// </summary>
    public class UIImageEffect : BaseMeshEffect
    {
        private const string MATERIAL_LOCATION = "UIMat";
        private const string GRAY_KEY_WORD_NAME = "GRAY_ON";
        private const string CIRCLE_KEY_WORD_NAME = "_IsCircle";
        private static readonly int IS_CIRCLE = Shader.PropertyToID(CIRCLE_KEY_WORD_NAME);

        [SerializeField] private bool m_debugRefresh = false;

        /// <summary>
        /// 是否需要刷新效果
        /// </summary>
        public bool Dirty { get; set; } = false;

        [SerializeField] private bool m_isGray = false;

        /// <summary>
        /// 是否应用灰度效果
        /// </summary>
        public bool IsGray
        {
            get => m_isGray;
            set
            {
                if (m_isGray != value)
                {
                    m_isGray = value;
                    Dirty = true;
                }
            }
        }

        [SerializeField] private bool m_isCircle = false;

        /// <summary>
        /// 是否应用圆形遮罩效果
        /// </summary>
        public bool IsCircle
        {
            get => m_isCircle;
            set
            {
                if (m_isCircle != value)
                {
                    m_isCircle = value;
                    Dirty = true;
                }
            }
        }

        private Image m_cacheImage;

        /// <summary>
        /// 获取缓存的Image组件
        /// </summary>
        public Image CacheImage => m_cacheImage == null ? m_cacheImage = GetComponent<Image>() : m_cacheImage;

        private static bool m_init = false;
        private static Dictionary<MatEffectType, Material> m_matDict = new Dictionary<MatEffectType, Material>();

        protected override void Awake()
        {
            Dirty = true;
        }

        private void LateUpdate()
        {
            if (m_debugRefresh || Dirty)
            {
                m_debugRefresh = false;
                Dirty = false;
                ApplyChange();
            }
        }

        private void ApplyChange()
        {
            if (CacheImage != null)
            {
                var mainTexture = CacheImage.mainTexture;

                if (mainTexture != null)
                {
                    CacheImage.material = GetMat(IsGray, IsCircle);
                }
            }
        }

        private static void InitMatDict()
        {
            if (m_init)
            {
                return;
            }

            m_init = true;
            var mat = GameModule.ResourceModule.LoadAsset<Material>(MATERIAL_LOCATION);

            if (mat != null)
            {
                m_matDict.Add(MatEffectType.None, null);
                AddNewMatToDict(mat, true, true);
                AddNewMatToDict(mat, false, true);
                AddNewMatToDict(mat, true, false);
            }
        }

        private static void AddNewMatToDict(Material mat, bool isGray, bool isCircle)
        {
            var newMat = new Material(mat);

            if (isGray)
            {
                newMat.EnableKeyword(GRAY_KEY_WORD_NAME);
            }
            else
            {
                newMat.DisableKeyword(GRAY_KEY_WORD_NAME);
            }

            newMat.SetInt(IS_CIRCLE, isCircle ? 1 : 0);
            m_matDict.Add(GetMatKey(isGray, isCircle), newMat);
        }

        private static MatEffectType GetMatKey(bool isGray, bool isCircle)
        {
            MatEffectType key = MatEffectType.None;
            if (isGray) key |= MatEffectType.Gray;
            if (isCircle) key |= MatEffectType.Circle;
            return key;
        }

        private static Material GetMat(bool isGray, bool isCircle)
        {
            InitMatDict();
            m_matDict.TryGetValue(GetMatKey(isGray, isCircle), out var mat);
            return mat;
        }
        
        /// <summary>
        /// 清空材质缓存
        /// </summary>
        public static void ClearCache()
        {
            foreach (var kvp in m_matDict)
            {
                if (kvp.Value != null)
                {
                    GameObject.Destroy(kvp.Value);
                }
            }
            m_matDict.Clear();
            m_init = false;
        }

        /// <summary>
        /// 修改网格顶点以应用圆形遮罩效果
        /// </summary>
        /// <param name="vh">顶点辅助器</param>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (CacheImage == null || !IsActive())
            {
                return;
            }

            if (IsCircle)
            {
                Vector4 uv = m_cacheImage.sprite != null
                    ? DataUtility.GetOuterUV(m_cacheImage.sprite)
                    : Vector4.zero;
                float uvWidth = uv.z - uv.x;
                float uvHeight = uv.w - uv.y;

                if (uvWidth == 0 || uvHeight == 0)
                {
                    return;
                }

                int vertCount = vh.currentVertCount;
                var vert = new UIVertex();

                for (int i = 0; i < vertCount; ++i)
                {
                    vh.PopulateUIVertex(ref vert, i);
                    vert.uv1.x = (vert.uv0.x - uv.x) / uvWidth;
                    vert.uv1.y = (vert.uv0.y - uv.y) / uvHeight;
                    vh.SetUIVertex(vert, i);
                }
            }
        }
    }
}