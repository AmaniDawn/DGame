using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextCircleExtend
    {
        [SerializeField] private bool m_useTextCircle;
        [SerializeField, Range(100, 1000)] private float m_radius = 100;
        [SerializeField, Range(0, 10)] private float m_spaceCoff = 1f;
        [SerializeField, Range(0, 360)] private float m_angleOffset = 0;

        public bool UseTextCircle => m_useTextCircle;

        // 缓存计算用的变量，避免在循环中重复创建
        private readonly UIVertex[] m_vertices = new UIVertex[4];

        public void ModifyMesh(VertexHelper vh)
        {
            if (!m_useTextCircle || m_radius == 0 || vh.currentVertCount == 0)
            {
                return;
            }

            // 预先计算常用值
            float radiusReciprocal = 1f / m_radius;
            int quadCount = vh.currentVertCount >> 2; // vh.currentVertCount / 4
            float angleOffsetRad = m_angleOffset * Mathf.Deg2Rad;
            float halfPI = Mathf.PI * 0.5f;

            for (int i = 0; i < quadCount; i++)
            {
                // 批量读取顶点数据
                int vertexIndex = i << 2; // i * 4
                for (int j = 0; j < 4; j++)
                {
                    vh.PopulateUIVertex(ref m_vertices[j], vertexIndex + j);
                }

                // 计算中心点（手动插值避免Vector3.Lerp）
                Vector3 p0 = m_vertices[0].position;
                Vector3 p2 = m_vertices[2].position;
                float centerX = (p0.x + p2.x) * 0.5f;
                float centerY = (p0.y + p2.y) * 0.5f;
                float centerZ = (p0.z + p2.z) * 0.5f;

                // 预先计算角度
                float angle = halfPI - (centerX * m_spaceCoff * radiusReciprocal) + angleOffsetRad;
                float cosAngle = Mathf.Cos(angle);
                float sinAngle = Mathf.Sin(angle);

                // 目标位置
                float targetX = cosAngle * m_radius;
                float targetY = sinAngle * m_radius;

                // 旋转角度（绕Z轴）
                float rotationAngle = angle - halfPI; // angle * Rad2Deg - 90 转换为弧度
                float cosRot = Mathf.Cos(rotationAngle);
                float sinRot = Mathf.Sin(rotationAngle);

                // 垂直偏移
                float verticalOffset = -m_radius + centerY;

                // 直接计算变换后的位置，避免创建Matrix4x4
                for (int j = 0; j < 4; j++)
                {
                    Vector3 pos = m_vertices[j].position;

                    // Step 1: 平移到原点 (pos - center)
                    float localX = pos.x - centerX;
                    float localY = pos.y - centerY;

                    // Step 2: 绕Z轴旋转
                    float rotatedX = localX * cosRot - localY * sinRot;
                    float rotatedY = localX * sinRot + localY * cosRot;

                    // Step 3: 平移到目标位置 + 垂直偏移
                    m_vertices[j].position = new Vector3(
                        rotatedX + targetX,
                        rotatedY + targetY + verticalOffset,
                        pos.z
                    );

                    vh.SetUIVertex(m_vertices[j], vertexIndex + j);
                }
            }
        }
    }
}