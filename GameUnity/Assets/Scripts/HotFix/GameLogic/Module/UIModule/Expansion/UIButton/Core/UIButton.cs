using UnityEngine;

namespace GameLogic
{
    public class UIButton : BaseUIButton
    {
        /// <summary>
        /// 矩阵位置组件
        /// </summary>
        public RectTransform rectTransform => transform as RectTransform;
    }
}