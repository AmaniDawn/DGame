using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UIImageMaskExtend
    {
        private Image m_image;
        private RectTransform m_rectTransform;
        [SerializeField] private bool m_isUseMaskImage;

        public bool UseMaskImage
        {
	        get => m_isUseMaskImage;
	        set { if (m_isUseMaskImage != value) { m_isUseMaskImage = value; Refresh(); } }
        }

        public Sprite OverrideSprite
        {
            get => m_image?.overrideSprite;
            set
            {
                if (m_image == null)
                {
                    return;
                }
                m_image.overrideSprite = value;
                m_image.SetAllDirty();
            }
        }

        private List<Vector3> m_innerVertices;
        private List<Vector3> m_outerVertices;

        public List<Vector3> InnerVertices => m_innerVertices;
        public List<Vector3> OuterVertices => m_outerVertices;

        [SerializeField, Range(0, 1), Tooltip("圆形或扇形填充比例")] private float m_fillPercent = 1f;
        [SerializeField, Tooltip("是否填充圆形")]private bool m_fill = true;
        [SerializeField, Tooltip("圆环宽度")] private float m_ringWidth = 5;
        [SerializeField, Range(3, 100), Tooltip("圆形")] private int m_segements = 20;
        [SerializeField, Range(0f, 360f)] private float m_rotation = 0f;
        [SerializeField, Header("顶点距离设置")] private bool m_isUsePercentVert;
        [SerializeField, Range(0f, 1f)] private float[] m_verticesDistances;

        public float fillPercent
        {
	        get => m_fillPercent;
	        set { if (m_fillPercent != value) { m_fillPercent = value; Refresh(); } }
        }

        public bool fill
        {
	        get => m_fill;
	        set { if (m_fill != value) { m_fill = value; Refresh(); } }
        }

        public float ringWidth
        {
	        get => m_ringWidth;
	        set { if (m_ringWidth != value) { m_ringWidth = value; Refresh(); } }
        }

        public int segements
        {
	        get => m_segements;
	        set { if (m_segements != value) { m_segements = value; Refresh(); } }
        }

        public float rotation
        {
	        get => m_rotation;
	        set { if (m_rotation != value) { m_rotation = value; Refresh(); } }
        }

        public bool isUsePercentVert
        {
	        get => m_isUsePercentVert;
	        set { if (m_isUsePercentVert != value) { m_isUsePercentVert = value; Refresh(); } }
        }

        public float[] verticesDistances
        {
	        get => m_verticesDistances;
	        set { if (m_verticesDistances != value) { m_verticesDistances = value; Refresh(); } }
        }

        public void SetFillPercent(float value)
        {
	        fillPercent = value;
        }

        public void Initialize(Image image)
        {
            m_image = image;
            m_rectTransform = m_image.rectTransform;
            m_innerVertices = new List<Vector3>();
            m_outerVertices = new List<Vector3>();
        }

#if UNITY_EDITOR
        public void EditorInitialize(Image image)
        {
            m_image = image;
            m_rectTransform = m_image.rectTransform;
        }
#endif

        public void Update()
        {
            if (m_image == null || m_rectTransform == null) return;
            this.m_ringWidth = (float)Mathf.Clamp(m_ringWidth, 0, m_rectTransform.rect.width / 2f);
        }

        public void OnPopulateMesh(VertexHelper vh)
        {
	        vh.Clear();

	        m_innerVertices.Clear();
	        m_outerVertices.Clear();

	        if (m_image != null && m_rectTransform != null)
	        {
		        this.m_ringWidth = (float)Mathf.Clamp(m_ringWidth, 0, m_rectTransform.rect.width / 2f);
	        }

	        float degreeDelta = (float)(2 * Mathf.PI / m_segements);
	        int curSegements = (int)(m_segements * m_fillPercent);

	        float tw = m_rectTransform.rect.width;
	        float th = m_rectTransform.rect.height;
	        float outerRadius = m_rectTransform.pivot.x * tw;
	        float innerRadius = m_rectTransform.pivot.x * tw - m_ringWidth;

	        Vector4 uv = OverrideSprite != null ? DataUtility.GetOuterUV(OverrideSprite) : Vector4.zero;

	        float uvCenterX = (uv.x + uv.z) * 0.5f;
	        float uvCenterY = (uv.y + uv.w) * 0.5f;
	        float uvScaleX = (uv.z - uv.x) / tw;
	        float uvScaleY = (uv.w - uv.y) / th;

	        // 将旋转角度转换为弧度
	        float rotationRadians = -m_rotation * Mathf.Deg2Rad;
	        // 从旋转角度开始
	        float curDegree = rotationRadians;
	        // float curDegree = 0;
	        UIVertex uiVertex;
	        int verticesCount;
	        int triangleCount;
	        Vector2 curVertice;
	        //Debug.Log("m_Fill:"+ m_Fill);
	        if (m_fill) //圆形
	        {
	            curVertice = Vector2.zero;
	            verticesCount = curSegements + 1;
	            uiVertex = new UIVertex();
	            uiVertex.color = m_image.color;
	            uiVertex.position = curVertice;
	            uiVertex.uv0 = new Vector2(curVertice.x * uvScaleX + uvCenterX, curVertice.y * uvScaleY + uvCenterY);
	            vh.AddVert(uiVertex);

	            for (int i = 1; i < verticesCount; i++)
	            {
	                float cosA = Mathf.Cos(curDegree);
	                float sinA = Mathf.Sin(curDegree);

	                if (m_isUsePercentVert)
	                {
		                // 根据顶点距离比例调整半径
		                float distanceRatio = GetVertexDistance(i - 1);
		                float adjustedRadius = outerRadius * distanceRatio;
		                curVertice = new Vector2(cosA * adjustedRadius, sinA * adjustedRadius);
	                }
	                else
	                {
		                curVertice = new Vector2(cosA * outerRadius, sinA * outerRadius);
	                }

	                curDegree += degreeDelta;

	                uiVertex = new UIVertex();
	                uiVertex.color = m_image.color;
	                uiVertex.position = curVertice;
	                uiVertex.uv0 = new Vector2(curVertice.x * uvScaleX + uvCenterX, curVertice.y * uvScaleY + uvCenterY);
	                vh.AddVert(uiVertex);

	                m_outerVertices.Add(curVertice);
	            }

	            triangleCount = curSegements * 3;
	            for (int i = 0, vIdx = 1; i < triangleCount - 3; i += 3, vIdx++)
	            {
	                vh.AddTriangle(vIdx, 0, vIdx + 1);
	            }
	            if (m_fillPercent == 1)
	            {
	                //首尾顶点相连
	                vh.AddTriangle(verticesCount - 1, 0, 1);
	            }
	        }
	        else//圆环
	        {
	            verticesCount = curSegements * 2;
	            for (int i = 0; i < verticesCount; i += 2)
	            {
	                float cosA = Mathf.Cos(curDegree);
	                float sinA = Mathf.Sin(curDegree);
	                curDegree += degreeDelta;

	                float distanceRatio = 0;
	                float adjustedInnerRadius = 0;
	                float adjustedOuterRadius = 0;

	                if (m_isUsePercentVert)
	                {
		                // 根据顶点距离比例调整内外半径
		                distanceRatio = GetVertexDistance(i / 2);
		                adjustedInnerRadius = innerRadius * distanceRatio;
		                curVertice = new Vector3(cosA * adjustedInnerRadius, sinA * adjustedInnerRadius);
	                }
	                else
	                {
		                curVertice = new Vector3(cosA * innerRadius, sinA * innerRadius);
	                }

	                // curVertice = new Vector3(cosA * innerRadius, sinA * innerRadius);
	                uiVertex = new UIVertex();
	                uiVertex.color = m_image.color;
	                uiVertex.position = curVertice;
	                uiVertex.uv0 = new Vector2(curVertice.x * uvScaleX + uvCenterX, curVertice.y * uvScaleY + uvCenterY);
	                vh.AddVert(uiVertex);
	                m_innerVertices.Add(curVertice);

	                if (m_isUsePercentVert)
	                {
		                adjustedOuterRadius = outerRadius * distanceRatio;
		                curVertice = new Vector3(cosA * adjustedOuterRadius, sinA * adjustedOuterRadius);
	                }
	                else
	                {
		                curVertice = new Vector3(cosA * outerRadius, sinA * outerRadius);
	                }
	                uiVertex = new UIVertex();
	                uiVertex.color = m_image.color;
	                uiVertex.position = curVertice;
	                uiVertex.uv0 = new Vector2(curVertice.x * uvScaleX + uvCenterX, curVertice.y * uvScaleY + uvCenterY);
	                vh.AddVert(uiVertex);
	                m_outerVertices.Add(curVertice);
	            }

	            triangleCount = curSegements * 3 * 2;
	            for (int i = 0, vIdx = 0; i < triangleCount - 6; i += 6, vIdx += 2)
	            {
	                vh.AddTriangle(vIdx + 1, vIdx, vIdx + 3);
	                vh.AddTriangle(vIdx, vIdx + 2, vIdx + 3);
	            }
	            if (m_fillPercent == 1)
	            {
	                //首尾顶点相连
	                vh.AddTriangle(verticesCount - 1, verticesCount - 2, 1);
	                vh.AddTriangle(verticesCount - 2, 0, 1);
	            }
	        }
        }

        private float GetVertexDistance(int vertexIndex)
        {
	        if (m_verticesDistances.Length != m_segements)
	        {
		        m_verticesDistances = new float[m_segements];
		        for (int i = 0; i < m_verticesDistances.Length; i++)
		        {
			        m_verticesDistances[i] = 1;
		        }
	        }
	        if (m_verticesDistances == null || m_verticesDistances.Length == 0)
	        {
		        return 1f;
	        }

	        // 循环使用顶点距离数组
	        int index = vertexIndex % m_verticesDistances.Length;
	        return Mathf.Clamp01(m_verticesDistances[index]);
        }

        public bool Contains(Vector2 pos, List<Vector3> outerVertices, List<Vector3> innerVertices)
        {
	        var crossNumber = 0;
	        RayCrossing(pos, innerVertices, ref crossNumber);//检测内环
	        RayCrossing(pos, outerVertices, ref crossNumber);//检测外环
	        return (crossNumber & 1) == 1;
        }

        public void DrawPolygon(int verCnt, List<float> percents, float rotation = -1)
        {
	        if (rotation >= 0)
	        {
		        m_rotation = rotation;
	        }
	        m_isUseMaskImage = true;
	        m_isUsePercentVert = true;
	        m_segements = verCnt;
	        verticesDistances = percents.ToArray();
        }

        /// <summary>
        /// 使用RayCrossing算法判断点击点是否在封闭多边形里
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="vertices"></param>
        /// <param name="crossNumber"></param>
        private void RayCrossing(Vector2 pos, List<Vector3> vertices, ref int crossNumber)
        {
	        for (int i = 0, count = vertices.Count; i < count; i++)
	        {
		        var v1 = vertices[i];
		        var v2 = vertices[(i + 1) % count];

		        //点击点水平线必须与两顶点线段相交
		        if (((v1.y <= pos.y) && (v2.y > pos.y))
		            || ((v1.y > pos.y) && (v2.y <= pos.y)))
		        {
			        //只考虑点击点右侧方向，点击点水平线与线段相交，且交点x > 点击点x，则crossNumber+1
			        if (pos.x < v1.x + (pos.y - v1.y) / (v2.y - v1.y) * (v2.x - v1.x))
			        {
				        crossNumber += 1;
			        }
		        }
	        }
        }

        public void Refresh()
        {
	        m_image?.SetVerticesDirty();
        }
    }
}