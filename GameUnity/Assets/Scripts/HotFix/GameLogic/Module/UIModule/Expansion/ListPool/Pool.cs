using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 通用对象池
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    internal class Pool<T> where T : new()
    {
        private readonly Stack<T> m_stack = new Stack<T>();

        private readonly Action<T> m_actionOnGet;

        private readonly Action<T> m_actionOnRecycle;

        /// <summary>
        /// 对象总数
        /// </summary>
        public int count { get; private set; }

        /// <summary>
        /// 活跃对象数量（已借出）
        /// </summary>
        public int activeCount => count - inactiveCount;

        /// <summary>
        /// 非活跃对象数量（在池中）
        /// </summary>
        public int inactiveCount => m_stack.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="actionOnGet">获取对象时的回调</param>
        /// <param name="actionOnRecycle">回收对象时的回调</param>
        public Pool(Action<T> actionOnGet, Action<T> actionOnRecycle)
        {
            m_actionOnGet = actionOnGet;
            m_actionOnRecycle = actionOnRecycle;
        }

        /// <summary>
        /// 从池中获取一个对象
        /// </summary>
        /// <returns>对象实例</returns>
        public T Get()
        {
            T element;
            if (m_stack.Count == 0)
            {
                element = new T();
                count++;
            }
            else
            {
                element = m_stack.Pop();
            }
            m_actionOnGet?.Invoke(element);
            return element;
        }

        /// <summary>
        /// 回收对象到池中
        /// </summary>
        /// <param name="element">要回收的对象</param>
        public void Recycle(T element)
        {
            if(element == null) return;
            if (m_stack.Count > 0 && ReferenceEquals(m_stack.Peek(), element))
            {
                throw new Exception("Internal error. Trying to destroy object that is already released to pool.");
            }

            m_actionOnRecycle?.Invoke(element);
            m_stack.Push(element);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            m_stack.Clear();
            count = 0;
        }
    }
}