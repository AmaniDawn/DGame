using UnityEditor;
using UnityEngine;

namespace DGame
{
    internal sealed class DragAreaGetObject
    {
        public static Object[] GetObjects(string msg = null)
        {
            Event aEvent = Event.current;
            GUI.contentColor = Color.white;

            // 当有对象被拖拽到该区域上方时触发 or 当用户释放鼠标完成拖拽时触发
            if (aEvent.type is EventType.DragUpdated or EventType.DragPerform)
            {
                // 设置拖拽时的视觉模式为"复制"
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                bool needReturn = false;

                // 当用户释放鼠标完成拖拽时触发
                if (aEvent.type == EventType.DragPerform)
                {
                    // 接受拖拽操作
                    DragAndDrop.AcceptDrag();
                    needReturn = true;
                }

                // 标记事件已处理，防止其他UI元素重复处理同一事件
                Event.current.Use();
                if (needReturn)
                {
                    // 返回对象引用
                    return DragAndDrop.objectReferences;
                }
            }
            return null;
        }
    }
}