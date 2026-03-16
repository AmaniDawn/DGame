using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 解决嵌套使用ScrollRect时的Drag冲突问题。请将该脚本放置到内层ScrollRect上(外层的ScrollRect的Drag事件会被内层的拦截)
    /// </summary>
    public class NestedScrollRect : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>
        /// 外层被拦截需要正常拖动的ScrollRect，可不指定，默认在父对象中找
        /// </summary>
        public ScrollRect anotherScrollRect;

        /// <summary>
        /// 当前的ScrollRect（本脚本所放置的物体上）的拖动方向默认为上下拖动，否则为左右拖动型
        /// </summary>
        public bool thisIsUpAndDown = true;

        private ScrollRect m_curScrollRect;
        private EventTrigger m_anotherEventTrigger;
        private bool m_isDragUpOrDown = false;

        private void Awake()
        {
            m_curScrollRect = GetComponent<ScrollRect>();
            if (anotherScrollRect == null)
            {
                Transform parent = transform.parent;
                while (parent != null)
                {
                    var scroll = parent.GetComponent<ScrollRect>();
                    if (scroll != null)
                    {
                        anotherScrollRect = scroll;
                        break;
                    }
                    parent = parent.parent;
                }
            }
            if (anotherScrollRect != null)
            {
                m_anotherEventTrigger = anotherScrollRect.GetComponent<EventTrigger>();
            }
        }

        /// <summary>
        /// 开始拖拽时的回调，判断拖拽方向并启用对应的ScrollRect
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.delta.sqrMagnitude < 0.001f)
            {
                m_isDragUpOrDown = thisIsUpAndDown;
            }
            else
            {
                float angle = Vector2.Angle(eventData.delta, Vector2.up);
                m_isDragUpOrDown = !(angle is >= 45f and <= 135f);
            }
            m_curScrollRect.enabled = m_isDragUpOrDown == thisIsUpAndDown;
            if (m_isDragUpOrDown != thisIsUpAndDown)
            {
                anotherScrollRect?.OnBeginDrag(eventData);
                m_anotherEventTrigger?.OnBeginDrag(eventData);
            }
        }

        /// <summary>
        /// 拖拽过程中的回调，将事件传递给外层ScrollRect
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (m_isDragUpOrDown != thisIsUpAndDown)
            {
                anotherScrollRect?.OnDrag(eventData);
                m_anotherEventTrigger?.OnDrag(eventData);
            }
        }

        /// <summary>
        /// 结束拖拽时的回调，恢复内层ScrollRect的启用状态
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_isDragUpOrDown != thisIsUpAndDown)
            {
                anotherScrollRect?.OnEndDrag(eventData);
                m_anotherEventTrigger?.OnEndDrag(eventData);
            }

            m_curScrollRect.enabled = true;
        }
    }
}