using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// UI拖拽监听器，用于监听UI元素的拖拽、按下、抬起等事件
    /// </summary>
    public class UIDragListener : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler,
        IPointerUpHandler
    {
        private event Action<PointerEventData> OnBeginDragAction;

        private event Action<PointerEventData> OnEndDragAction;

        private event Action<PointerEventData> OnDragAction;

        private event Action<PointerEventData> OnPointerDownAction;

        private event Action<PointerEventData> OnPointerUpAction;

        /// <summary>
        /// 获取或添加UIDragListener组件
        /// </summary>
        /// <param name="go">目标GameObject</param>
        /// <returns>UIDragListener实例</returns>
        public static UIDragListener Get(GameObject go)
            => DGame.Utility.UnityUtil.AddMonoBehaviour<UIDragListener>(go);

        /// <summary>
        /// 获取或添加UIDragListener组件并初始化事件监听
        /// </summary>
        /// <param name="go">目标GameObject</param>
        /// <param name="onBeginDrag">开始拖拽回调</param>
        /// <param name="onEndDrag">结束拖拽回调</param>
        /// <param name="onDrag">拖拽中回调</param>
        /// <param name="onPointerDown">按下回调</param>
        /// <param name="onPointerUp">抬起回调</param>
        /// <returns>UIDragListener实例</returns>
        public static UIDragListener Get(GameObject go, Action<PointerEventData> onBeginDrag,
            Action<PointerEventData> onEndDrag, Action<PointerEventData> onDrag,
            Action<PointerEventData> onPointerDown, Action<PointerEventData> onPointerUp)
        {
            var com = DGame.Utility.UnityUtil.AddMonoBehaviour<UIDragListener>(go);
            com?.Init(onBeginDrag, onEndDrag, onDrag, onPointerDown, onPointerUp);
            return com;
        }

        /// <summary>
        /// 初始化所有事件监听
        /// </summary>
        /// <param name="onBeginDrag">开始拖拽回调</param>
        /// <param name="onEndDrag">结束拖拽回调</param>
        /// <param name="onDrag">拖拽中回调</param>
        /// <param name="onPointerDown">按下回调</param>
        /// <param name="onPointerUp">抬起回调</param>
        public void Init(Action<PointerEventData> onBeginDrag, Action<PointerEventData> onEndDrag,
            Action<PointerEventData> onDrag, Action<PointerEventData> onPointerDown,
            Action<PointerEventData> onPointerUp)
        {
            AddOnBeginDragListener(onBeginDrag);
            AddOnEndDragListener(onEndDrag);
            AddOnDragListener(onDrag);
            AddOnPointerDownListener(onPointerDown);
            AddOnPointerUpListener(onPointerUp);
        }

        /// <summary>
        /// 开始拖拽时的回调（IBeginDragHandler接口实现）
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragAction?.Invoke(eventData);
        }

        /// <summary>
        /// 结束拖拽时的回调（IEndDragHandler接口实现）
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragAction?.Invoke(eventData);
        }

        /// <summary>
        /// 拖拽过程中的回调（IDragHandler接口实现）
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnDrag(PointerEventData eventData)
        {
            OnDragAction?.Invoke(eventData);
        }

        /// <summary>
        /// 按下时的回调（IPointerDownHandler接口实现）
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownAction?.Invoke(eventData);
        }

        /// <summary>
        /// 抬起时的回调（IPointerUpHandler接口实现）
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpAction?.Invoke(eventData);
        }

        #region AddListener

        /// <summary>
        /// 添加开始拖拽事件监听
        /// </summary>
        /// <param name="action">事件回调</param>
        public void AddOnBeginDragListener(Action<PointerEventData> action)
        {
            OnBeginDragAction += action;
        }

        /// <summary>
        /// 添加结束拖拽事件监听
        /// </summary>
        /// <param name="action">事件回调</param>
        public void AddOnEndDragListener(Action<PointerEventData> action)
        {
            OnEndDragAction += action;
        }

        /// <summary>
        /// 添加拖拽中事件监听
        /// </summary>
        /// <param name="action">事件回调</param>
        public void AddOnDragListener(Action<PointerEventData> action)
        {
            OnDragAction += action;
        }

        /// <summary>
        /// 添加按下事件监听
        /// </summary>
        /// <param name="action">事件回调</param>
        public void AddOnPointerDownListener(Action<PointerEventData> action)
        {
            OnPointerDownAction += action;
        }

        /// <summary>
        /// 添加抬起事件监听
        /// </summary>
        /// <param name="action">事件回调</param>
        public void AddOnPointerUpListener(Action<PointerEventData> action)
        {
            OnPointerUpAction += action;
        }

        #endregion

        #region RemoveListener

        /// <summary>
        /// 移除开始拖拽事件监听
        /// </summary>
        /// <param name="action">要移除的事件回调</param>
        public void RemoveOnBeginDragListener(Action<PointerEventData> action)
        {
            OnBeginDragAction -= action;
        }

        /// <summary>
        /// 移除结束拖拽事件监听
        /// </summary>
        /// <param name="action">要移除的事件回调</param>
        public void RemoveOnEndDragListener(Action<PointerEventData> action)
        {
            OnEndDragAction -= action;
        }

        /// <summary>
        /// 移除拖拽中事件监听
        /// </summary>
        /// <param name="action">要移除的事件回调</param>
        public void RemoveOnDragListener(Action<PointerEventData> action)
        {
            OnDragAction -= action;
        }

        /// <summary>
        /// 移除按下事件监听
        /// </summary>
        /// <param name="action">要移除的事件回调</param>
        public void RemoveOnPointerDownListener(Action<PointerEventData> action)
        {
            OnPointerDownAction -= action;
        }

        /// <summary>
        /// 移除抬起事件监听
        /// </summary>
        /// <param name="action">要移除的事件回调</param>
        public void RemoveOnPointerUpListener(Action<PointerEventData> action)
        {
            OnPointerUpAction -= action;
        }

        #endregion

        private void OnDestroy()
        {
            Clear();
        }

        /// <summary>
        /// 清除所有事件监听
        /// </summary>
        public void Clear()
        {
            OnBeginDragAction = null;
            OnEndDragAction = null;
            OnDragAction = null;
            OnPointerDownAction = null;
            OnPointerUpAction = null;
        }
    }
}