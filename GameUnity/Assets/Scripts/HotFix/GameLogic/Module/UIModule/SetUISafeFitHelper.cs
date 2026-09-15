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

        private readonly RectTransform m_curFitRect;

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
        /// 设置某一个节点不受m_curRect影响
        /// </summary>
        /// <param name="rect"></param>
        public void SetUINotFit(RectTransform rect)
        {
            if (m_curFitRect == null || rect == null)
            {
                return;
            }

            var position = rect.anchoredPosition;

            rect.anchoredPosition = new Vector2(position.x - m_curFitRect.sizeDelta.x,
                position.y - m_curFitRect.sizeDelta.y);
        }

        /// <summary>
        /// 设置某一个节点不受指定RectTransform的影响
        /// </summary>
        /// <param name="rect">设置的RectTransform</param>
        /// <param name="refRect">依赖的RectTransform</param>
        public void SetUINotFit(RectTransform rect, RectTransform refRect)
        {
            if (rect == null || refRect == null)
            {
                return;
            }

            var position = rect.anchoredPosition;

            rect.anchoredPosition = new Vector2(position.x - refRect.sizeDelta.x,
                position.y - refRect.sizeDelta.y);
        }
    }
}
