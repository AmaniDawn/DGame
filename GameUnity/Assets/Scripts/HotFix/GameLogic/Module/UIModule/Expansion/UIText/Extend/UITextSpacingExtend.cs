using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [System.Serializable]
    public class UITextSpacingExtend
    {
        [SerializeField] private bool m_isUseTextSpacing;
        public bool IsUseTextSpacing { get => m_isUseTextSpacing; set => m_isUseTextSpacing = value; }
        [SerializeField, Range(-10, 100)] private float m_textSpacing = 1f;
        public float TextSpacing { get => m_textSpacing; set => m_textSpacing = value; }

        public void PopulateMesh(VertexHelper toFill)
        {
            // toFill.currentVertCount：唯一顶点数 一个字符就是4个顶点数
            if (!m_isUseTextSpacing || toFill.currentVertCount <= 0)
            {
                return;
            }

            List<UIVertex> verts = ListPool<UIVertex>.Get();

            try
            {
                toFill.GetUIVertexStream(verts);
                // toFill.currentIndexCount：索引顶点数 一个字符就是6个索引顶点数
                int indexCnt = toFill.currentIndexCount;
                UIVertex vertex;

                // 每个字符由六个顶点组成 两个三角形
                // 实际只有四个顶点 矩形的四个角
                // 索引模式：0-1-2 和2-3-0
                // i / 6 得到当前字符的索引  i % 6 得到当前字符内的顶点索引（0-5）
                for (int i = 6; i < indexCnt; i++)
                {
                    // 第一个字不用改变位置
                    vertex = verts[i];
                    vertex.position += new Vector3(m_textSpacing * (i / 6), 0, 0);
                    // verts[i] = vertex;

                    // 当前字符的前三个顶点 第一个三角形 先设置第一个三角形的三个顶点 0-1-2
                    if (i % 6 <= 2)
                    {
                        toFill.SetUIVertex(vertex, (i / 6) * 4 + i % 6);
                    }

                    // 后设置第二个三角形的三个顶点 2-3-0（3-4-5），因为 2（3） 和 0（5） 在第一个三角形的时候已经设置过了
                    // 第三个顶点 特殊处理 因为三角形对应的是 0-1-2 2-3-0 六个顶点中 2 和 0 是重复的，所以只要处理第四个顶点即可
                    if (i % 6 == 4)
                    {
                        toFill.SetUIVertex(vertex, (i / 6) * 4 + i % 6 - 1);
                    }
                }
            }
            finally
            {
                if (verts != null)
                {
                    ListPool<UIVertex>.Recycle(verts);
                }
            }
        }
    }
}