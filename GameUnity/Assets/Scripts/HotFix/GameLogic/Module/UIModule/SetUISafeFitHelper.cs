using System.Collections.Generic;
using DGame;
using UnityEngine;
using Application = UnityEngine.Device.Application;
using Screen = UnityEngine.Device.Screen;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace GameLogic
{
    public class SetUISafeFitHelper
    {
        /// <summary>
        /// 是否适配刘海侧安全区（竖屏顶部，横屏随朝向映射到左右）
        /// </summary>
        public bool LiuHaiFit { get; set; } = false;

        /// <summary>
        /// 刘海侧安全区回补距离（屏幕像素），由已验证的平台及机型参数覆盖
        /// </summary>
        public float TopSpacing { get; set; } = 0;

        /// <summary>
        /// 是否适配另一侧及底部手势区
        /// </summary>
        public bool BottomFit { get; set; } = false;

        /// <summary>
        /// 另一侧安全区回补距离（屏幕像素），由已验证的平台及机型参数覆盖
        /// </summary>
        public float BottomSpacing { get; set; } = 0;

        private RectTransform m_curFitRect;
        private Dictionary<RectTransform, NotFitOffset> m_notFitOffsets;

        private struct NotFitOffset
        {
            public Vector2 Min;
            public Vector2 Max;
        }

        /// <summary>
        /// 移动设备屏幕适配
        /// </summary>
        /// <param name="fitRect">安全区容器，其父节点须覆盖完整屏幕</param>
        /// <param name="liuHaiFit">是否适配刘海侧安全区</param>
        /// <param name="topSpacing">刘海侧回补距离（屏幕像素，Windows/iOS 按机型覆盖）</param>
        /// <param name="bottomFit">是否适配另一侧及底部手势区</param>
        /// <param name="bottomSpacing">另一侧回补距离（屏幕像素，Windows/iOS 按机型覆盖）</param>
        public SetUISafeFitHelper(RectTransform fitRect, bool liuHaiFit = true, float topSpacing = 0, bool bottomFit = true, float bottomSpacing = 0)
        {
            LiuHaiFit = liuHaiFit;
            TopSpacing = topSpacing;
            BottomFit = bottomFit;
            BottomSpacing = bottomSpacing;
            m_curFitRect = fitRect;
        }

        public SetUISafeFitHelper() { }

        /// <summary>绑定当前适配容器及参数，保留已有子节点的反向补偿记录。</summary>
        internal void SetUIFit(RectTransform fitRect, bool liuHaiFit, float topSpacing, bool bottomFit, float bottomSpacing)
        {
            m_curFitRect = fitRect;
            LiuHaiFit = liuHaiFit;
            TopSpacing = topSpacing;
            BottomFit = bottomFit;
            BottomSpacing = bottomSpacing;
            SetUIFit();
        }

        /// <summary>
        /// 按平台及机型回补安全区，再映射到全屏父节点的归一化锚点。
        /// </summary>
        public void SetUIFit()
        {
            if (m_curFitRect == null)
            {
                return;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    TopSpacing = 70;
                    BottomSpacing = 80;
                    break;

                case RuntimePlatform.Android:
                    break;

                case RuntimePlatform.IPhonePlayer:
                    var phoneType = SystemInfo.deviceModel;
                    TopSpacing = 70;
                    BottomSpacing = 80;
                    if (phoneType == "iPhone12,1" || phoneType == "iPhone11,8")
                    {
                        TopSpacing = 30;
                        BottomSpacing = 70;
                    }
                    break;
            }

            Rect safeArea = Screen.safeArea;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Vector2 insetMin = safeArea.min;
            Vector2 insetMax = new Vector2(screenWidth - safeArea.xMax, screenHeight - safeArea.yMax);
            float topSpacing = Mathf.Max(0f, TopSpacing);
            float bottomSpacing = Mathf.Max(0f, BottomSpacing);

            if (screenWidth > screenHeight)
            {
                bool notchOnLeft = Screen.orientation != ScreenOrientation.LandscapeRight;
                insetMin.x = (notchOnLeft ? LiuHaiFit : BottomFit)
                    ? Mathf.Max(0f, insetMin.x - (notchOnLeft ? topSpacing : bottomSpacing)) : 0f;
                insetMax.x = (notchOnLeft ? BottomFit : LiuHaiFit)
                    ? Mathf.Max(0f, insetMax.x - (notchOnLeft ? bottomSpacing : topSpacing)) : 0f;
                // 横屏上下保持系统安全区，底部手势区不套用横向回补。
                insetMin.y = BottomFit ? insetMin.y : 0f;
                insetMax.y = LiuHaiFit ? insetMax.y : 0f;
            }
            else
            {
                bool upsideDown = Screen.orientation == ScreenOrientation.PortraitUpsideDown;
                insetMin.y = (upsideDown ? LiuHaiFit : BottomFit)
                    ? Mathf.Max(0f, insetMin.y - (upsideDown ? topSpacing : bottomSpacing)) : 0f;
                insetMax.y = (upsideDown ? BottomFit : LiuHaiFit)
                    ? Mathf.Max(0f, insetMax.y - (upsideDown ? bottomSpacing : topSpacing)) : 0f;
                insetMin.x = LiuHaiFit ? insetMin.x : 0f;
                insetMax.x = LiuHaiFit ? insetMax.x : 0f;
            }

            m_curFitRect.anchorMin = new Vector2(insetMin.x / screenWidth, insetMin.y / screenHeight);
            m_curFitRect.anchorMax = new Vector2(1f - insetMax.x / screenWidth, 1f - insetMax.y / screenHeight);
            m_curFitRect.offsetMin = Vector2.zero;
            m_curFitRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 将直属子节点的位置和尺寸补偿到当前安全区容器铺满父节点时的布局。
        /// </summary>
        /// <param name="rect">安全区直属子节点，不应由 LayoutGroup 等组件驱动其位置和尺寸</param>
        public void SetUINotFit(RectTransform rect)
        {
            SetUINotFit(rect, m_curFitRect);
        }

        /// <summary>
        /// 按子节点锚点反向补偿安全区收缩，保持层级、锚点和轴心不变；重复调用不累积偏移。
        /// 容器再次适配后需再次调用。嵌套 UI 应对其位于安全区下的直属容器调用。
        /// </summary>
        /// <param name="rect">refRect 的直属子节点，不应由 LayoutGroup 等组件驱动其位置和尺寸</param>
        /// <param name="refRect">安全区容器，仅通过锚点及偏移适配，保持单位缩放和零旋转；其父节点为未适配范围</param>
        public void SetUINotFit(RectTransform rect, RectTransform refRect)
        {
            if (rect == null || refRect == null)
            {
                return;
            }

            if (rect.parent != refRect || !(refRect.parent is RectTransform fullRect))
            {
                DLogger.Warning("SetUINotFit 需要安全区直属子节点，且安全区的父节点必须是 RectTransform。");
                return;
            }

            Rect fullBounds = fullRect.rect;
            Rect fitBounds = refRect.rect;
            Vector2 fullMin = refRect.InverseTransformPoint(fullRect.TransformPoint(fullBounds.min));
            Vector2 fullMax = refRect.InverseTransformPoint(fullRect.TransformPoint(fullBounds.max));
            Vector2 minDelta = fullMin - fitBounds.min;
            Vector2 maxDelta = fullMax - fitBounds.max;
            NotFitOffset compensation = new NotFitOffset
            {
                Min = minDelta + Vector2.Scale(maxDelta - minDelta, rect.anchorMin),
                Max = minDelta + Vector2.Scale(maxDelta - minDelta, rect.anchorMax)
            };

            m_notFitOffsets ??= new Dictionary<RectTransform, NotFitOffset>();
            m_notFitOffsets.TryGetValue(rect, out NotFitOffset previous);
            // 只应用本次与上次补偿的差值，保留调用方对子节点原始布局的修改。
            Vector2 offsetMin = rect.offsetMin + (compensation.Min - previous.Min);
            Vector2 offsetMax = rect.offsetMax + (compensation.Max - previous.Max);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            m_notFitOffsets[rect] = compensation;
        }
    }
}
