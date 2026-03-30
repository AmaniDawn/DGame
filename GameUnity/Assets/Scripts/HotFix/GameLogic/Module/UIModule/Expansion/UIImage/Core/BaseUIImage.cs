using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class BaseUIImage : Image, IMeshModifier
    {
        [SerializeField] private UIImageMaskExtend m_uiImageMaskExtend = new UIImageMaskExtend();
        [SerializeField] private UIImageRoundedCornersExtend m_uiImageRoundedCornersExtend = new UIImageRoundedCornersExtend();
        [SerializeField] private UIImageMirrorExtend m_uiImageMirrorExtend = new UIImageMirrorExtend();

        public UIImageMirrorExtend UIImageMirrorExtend => m_uiImageMirrorExtend;

        protected override void Awake()
        {
            base.Awake();
            UIImageMirrorExtend?.Initialize(this);
            m_uiImageMaskExtend?.Initialize(this);
            m_uiImageRoundedCornersExtend?.Initialize(this);
        }

        private void Update()
        {
#if UNITY_EDITOR

            // m_uiImageMaskExtend.Update();

#endif
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_uiImageMaskExtend.EditorInitialize(this);
            m_uiImageRoundedCornersExtend.EditorInitialize(this);
            UIImageMirrorExtend.EditorInitialize(this);
            SetVerticesDirty();
        }
#endif

        public void SetFillPercent(float value)
        {
            m_uiImageMaskExtend.SetFillPercent(value);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount <= 0)
            {
                base.OnPopulateMesh(vh);
                return;
            }

            bool isOverride = m_uiImageMaskExtend.UseMaskImage || m_uiImageRoundedCornersExtend.IsUseRoundedCorners;

            if (!isOverride)
            {
                base.OnPopulateMesh(vh);
            }
        }

        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!m_uiImageMaskExtend.UseMaskImage)
            {
                return base.IsRaycastLocationValid(screenPoint, eventCamera);
            }
            else
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local);
                return m_uiImageMaskExtend.Contains(local, m_uiImageMaskExtend.OuterVertices, m_uiImageMaskExtend.InnerVertices);
            }
        }

        public void DrawPolygon(int vertCnt, List<float> percents, float rotation = -1)
        {
            m_uiImageRoundedCornersExtend.IsUseRoundedCorners = false;
            m_uiImageMaskExtend.DrawPolygon(vertCnt, percents, rotation);
        }

        public void ModifyMesh(Mesh mesh)
        {
        }

        public void ModifyMesh(VertexHelper verts)
        {
            if (!IsActive() || verts.currentVertCount <= 0)
            {
                return;
            }

            if (m_uiImageMaskExtend.UseMaskImage)
            {
                m_uiImageMaskExtend.OnPopulateMesh(verts);
            }

            if (m_uiImageRoundedCornersExtend.IsUseRoundedCorners)
            {
                m_uiImageRoundedCornersExtend.OnPopulateMesh(verts);
            }

            UIImageMirrorExtend?.ModifyMesh(verts);
        }
    }
}