using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 扇形布局组件
    /// </summary>
    [AddComponentMenu("Layout/Circle Layout Group", 150)]
    public class CircleLayoutGroup : LayoutGroup
    {
        public enum LayoutMode
        {
            [InspectorName("圆形平均分布")]
            Circle = 0,

            [InspectorName("扇形分布")]
            Sector = 1,
        }

        [Header("分布模式: Circle(圆形平均分布) Sector(扇形分布)")]
        public LayoutMode mode = LayoutMode.Circle;

        [Header("半径")] public float radius = 0;

        [Header("起始角度")] public float initAngle = 0;

        [Header("是否保持弧度值不变")] public bool keepRadLen = false;

        [Header("弧度保持不变的值(keepRadLen为true时有效)")]
        public float keepRadLenVal = 0f;

        [Header("扇形分布范围")] public float sectorAngle = 0;

        [Header("扇形分布时且keepRadLen为false时,是否中间对齐到扇形中心,否则两端对齐")]
        public bool sectorAlignCenter = false;

        [Header("扇形分布且sectorAlignCenter为false时,是否为逆时针")]
        public bool sectorClockwise = true;

        [Header("圆心")] public Vector2 circleCenter;
        public bool controlChildSize = false;

        public Vector2 childSize;

        // 缓存变量避免重复计算
        private int cachedChildCount = -1;
        private bool needsRebuild = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            needsRebuild = true;
            CalculateRadial();
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            if (needsRebuild || cachedChildCount != rectChildren.Count)
            {
                cachedChildCount = rectChildren.Count;
                needsRebuild = false;
                CalculateRadial();
            }
        }

        public override void CalculateLayoutInputVertical()
        {
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            needsRebuild = true;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
#endif

        protected void CalculateRadial()
        {
            this.m_Tracker.Clear();

            if (transform.childCount == 0)
            {
                return;
            }

            switch (mode)
            {
                case LayoutMode.Circle:
                    OptimizedCircle();
                    break;
                case LayoutMode.Sector:
                    OptimizedSector();
                    break;
            }
        }

        private void OptimizedCircle()
        {
            if (rectChildren.Count == 0) return;

            int childCount = rectChildren.Count;
            float perRad = 2f * Mathf.PI / childCount;
            float initRad = initAngle * Mathf.Deg2Rad;

            // 使用连续计算优化三角函数
            SetLayoutOptimized(initRad, perRad, childCount);
        }

        private void OptimizedSector()
        {
            if (rectChildren.Count == 0) return;

            int childCount = rectChildren.Count;
            float initRad = initAngle * Mathf.Deg2Rad;
            float sectorRad = sectorAngle * Mathf.Deg2Rad;
            float perRad;

            if (keepRadLen)
            {
                perRad = keepRadLenVal / radius;
                if (sectorAlignCenter && childCount > 1)
                {
                    float radOffset = perRad * ((childCount - 1) * 0.5f);
                    initRad += sectorClockwise ? (sectorRad * 0.5f - radOffset) : (-sectorRad * 0.5f + radOffset);
                }
            }
            else
            {
                perRad = sectorAlignCenter ?
                    sectorRad / (childCount + 1) :
                    (childCount == 1 ? 0f : sectorRad / (childCount - 1));

                if (sectorAlignCenter)
                {
                    initRad += sectorClockwise ? perRad : -perRad;
                }
            }

            if (!sectorClockwise) perRad *= -1;

            SetLayoutOptimized(initRad, perRad, childCount);
        }

        private void SetLayoutOptimized(float initRad, float perRad, int childCount)
        {
            // 预计算边界值
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            // 使用连续三角函数计算优化性能
            float currentSin = Mathf.Sin(initRad);
            float currentCos = Mathf.Cos(initRad);

            // 预计算增量三角函数值
            float deltaSin = Mathf.Sin(perRad);
            float deltaCos = Mathf.Cos(perRad);


            bool controlSize = controlChildSize; // 缓存到局部变量

            Vector2 size = childSize; // 缓存到局部变量

            for (int i = 0; i < childCount; i++)
            {
                RectTransform child = rectChildren[i];

                // 设置驱动属性
                m_Tracker.Add(this, child,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.Pivot);

                // 设置基础属性
                child.pivot = Vector2.one * 0.5f;
                child.anchorMin = child.anchorMax = Vector2.one * 0.5f;

                // 计算位置 - 使用预计算的三角函数值
                float x = circleCenter.x + radius * currentSin;
                float y = circleCenter.y + radius * currentCos;

                child.anchoredPosition = new Vector2(x, y);

                // 控制大小
                if (controlSize)
                {
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                }

                // 更新边界计算
                Vector2 childHalfSize = child.rect.size * 0.5f;
                UpdateBounds(ref minX, ref maxX, ref minY, ref maxY, x, y, childHalfSize);

                // 连续计算下一个角度的三角函数值
                if (i < childCount - 1) // 最后一个不需要计算
                {
                    float newSin = currentSin * deltaCos + currentCos * deltaSin;
                    float newCos = currentCos * deltaCos - currentSin * deltaSin;
                    currentSin = newSin;
                    currentCos = newCos;
                }
            }

            // 设置布局数据
            SetLayoutInputForAxis(radius, Mathf.Abs(maxX - minX), 0, 0);
            SetLayoutInputForAxis(radius, Mathf.Abs(maxY - minY), 0, 1);
        }

        private void UpdateBounds(ref float minX, ref float maxX, ref float minY, ref float maxY, float x, float y, Vector2 halfSize)
        {
            float left = x - halfSize.x;
            float right = x + halfSize.x;
            float bottom = y - halfSize.y;
            float top = y + halfSize.y;

            if (left < minX) minX = left;
            if (right > maxX) maxX = right;
            if (bottom < minY) minY = bottom;
            if (top > maxY) maxY = top;
        }
    }
}