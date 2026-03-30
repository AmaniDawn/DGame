using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DGame
{
    /// <summary>
    /// 链表对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class DGameLinkedList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ICollection
    {
        private readonly LinkedList<T> m_linkedList = new LinkedList<T>();
        private readonly Queue<LinkedListNode<T>> m_nodePool = new Queue<LinkedListNode<T>>();

        /// <summary>
        /// 链表的实际节点数量
        /// </summary>
        public int Count => m_linkedList.Count;

        /// <summary>
        /// 节点缓存池中的缓存数量
        /// </summary>
        public int CacheNodeCount => m_nodePool.Count;

        /// <summary>
        /// 链表的第一个节点
        /// </summary>
        public LinkedListNode<T> First => m_linkedList.First;

        /// <summary>
        /// 链表的最后一个节点
        /// </summary>
        public LinkedListNode<T> Last => m_linkedList.Last;

        /// <summary>
        /// 获取是否支持多线程同步访问（线程安全） 默认返回false 不是线程安全 需要手动加锁
        /// </summary>
        public bool IsSynchronized => ((ICollection)m_linkedList).IsSynchronized;

        /// <summary>
        /// 获取可用于同步对ICollection的访问的对象 同步锁对象
        /// </summary>
        public object SyncRoot => ((ICollection)m_linkedList).SyncRoot;

        /// <summary>
        /// 获取 ICollection`1 是否为只读 本身是可变的 所以始终返回false
        /// </summary>
        public bool IsReadOnly => ((ICollection<T>)m_linkedList).IsReadOnly;

        #region Add

        /// <summary>
        /// 向链表的指定节点后添加新节点
        /// </summary>
        /// <param name="node">指定节点</param>
        /// <param name="value">新节点</param>
        /// <returns></returns>
        public LinkedListNode<T> AddAfter(LinkedListNode<T> node, T value)
        {
            var newNode = Spawn(value);
            m_linkedList.AddAfter(node, newNode);
            return newNode;
        }

        /// <summary>
        /// 向链表的指定节点后添加新节点
        /// </summary>
        /// <param name="node">指定节点</param>
        /// <param name="newNode">新节点</param>
        /// <returns></returns>
        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            m_linkedList.AddAfter(node, newNode);
        }

        /// <summary>
        /// 向链表的指定节点前添加新节点
        /// </summary>
        /// <param name="node">指定节点</param>
        /// <param name="value">新节点</param>
        /// <returns></returns>
        public LinkedListNode<T> AddBefore(LinkedListNode<T> node, T value)
        {
            var newNode = Spawn(value);
            m_linkedList.AddBefore(node, newNode);
            return newNode;
        }

        /// <summary>
        /// 向链表的指定节点前添加新节点
        /// </summary>
        /// <param name="node">指定节点</param>
        /// <param name="newNode">新节点</param>
        /// <returns></returns>
        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            m_linkedList.AddBefore(node, newNode);
        }

        /// <summary>
        /// 在链表的头部添加新节点
        /// </summary>
        /// <param name="value">新节点</param>
        /// <returns></returns>
        public LinkedListNode<T> AddFirst(T value)
        {
            var node = new LinkedListNode<T>(value);
            m_linkedList.AddFirst(node);
            return node;
        }

        /// <summary>
        /// 在链表的头部添加新节点
        /// </summary>
        /// <param name="node">新节点</param>
        /// <returns></returns>
        public void AddFirst(LinkedListNode<T> node)
        {
            m_linkedList.AddFirst(node);
        }

        /// <summary>
        /// 在链表的尾部添加新节点
        /// </summary>
        /// <param name="value">新节点</param>
        /// <returns></returns>
        public LinkedListNode<T> AddLast(T value)
        {
            var node = Spawn(value);
            m_linkedList.AddLast(node);
            return node;
        }

        /// <summary>
        /// 在链表的尾部添加新节点
        /// </summary>
        /// <param name="node">新节点</param>
        /// <returns></returns>
        public void AddLast(LinkedListNode<T> node)
        {
            m_linkedList.AddLast(node);
        }

        #endregion

        #region Spawn

        private LinkedListNode<T> Spawn(T value)
        {
            LinkedListNode<T> newNode;

            if (m_nodePool.Count > 0)
            {
                newNode = m_nodePool.Dequeue();
                newNode.Value = value;
            }
            else
            {
                newNode = new LinkedListNode<T>(value);
            }

            return newNode;
        }

        #endregion

        #region Recycle

        private void Recycle(LinkedListNode<T> node)
        {
            if (node != null)
            {
                node.Value = default(T);
                m_nodePool.Enqueue(node);
            }
        }

        #endregion

        #region Claer

        /// <summary>
        /// 清空链表中的所有节点
        /// </summary>
        public void Clear()
        {
            LinkedListNode<T> current = m_linkedList.First;

            while (current != null)
            {
                Recycle(current);
                current = current.Next;
            }

            m_linkedList.Clear();
        }

        /// <summary>
        /// 清空缓存池节点
        /// </summary>
        public void ClearNodePool()
        {
            m_nodePool.Clear();
        }

        #endregion

        #region Find

        /// <summary>
        /// 查找包含指定值的第一个节点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LinkedListNode<T> Find(T value)
        {
            return m_linkedList.Find(value);
        }

        /// <summary>
        /// 查找包含指定值的最后一个节点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LinkedListNode<T> FindLast(T value)
        {
            return m_linkedList.FindLast(value);
        }

        #endregion

        #region Remove

        /// <summary>
        /// 从链表中删除指定节点 返回缓存池
        /// </summary>
        /// <param name="value">指定节点</param>
        /// <returns></returns>
        public bool Remove(T value)
        {
            LinkedListNode<T> node = m_linkedList.Find(value);

            if (node != null)
            {
                m_linkedList.Remove(node);
                Recycle(node);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从链表中删除指定节点 返回缓存池
        /// </summary>
        /// <param name="node">指定节点</param>
        /// <returns></returns>
        public void Remove(LinkedListNode<T> node)
        {
            if (node != null)
            {
                m_linkedList.Remove(node);
                Recycle(node);
            }
        }

        /// <summary>
        /// 删除链表的首节点
        /// </summary>
        /// <exception cref="DGameException"></exception>
        public void RemoveFirst()
        {
            LinkedListNode<T> first = m_linkedList.First;

            if (first == null)
            {
                throw new DGameException("链表头节点是无效的");
            }

            m_linkedList.RemoveFirst();
            Recycle(first);
        }

        /// <summary>
        /// 删除链表的尾节点
        /// </summary>
        /// <exception cref="DGameException"></exception>
        public void RemoveLast()
        {
            LinkedListNode<T> last = m_linkedList.Last;

            if (last == null)
            {
                throw new DGameException("链表尾节点是无效的");
            }

            m_linkedList.RemoveLast();
            Recycle(last);
        }

        #endregion

        #region CopyTo

        /// <summary>
        /// 指定索引开始复制整个链表数据到一维数组中
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            m_linkedList.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 从指定索引开始复制整个链表数据到一维数组中
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(Array array, int index)
        {
            ((ICollection)m_linkedList).CopyTo(array, index);
        }

        #endregion

        #region GetEnumerator

        /// <summary>
        /// 返回循环访问集合的枚举数
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(m_linkedList);
        }

        /// <summary>
        /// 返回循环访问集合的枚举数
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 返回循环访问集合的枚举数
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// 向链表尾部添加节点
        /// </summary>
        /// <param name="item"></param>
        void ICollection<T>.Add(T item)
        {
            AddLast(item);
        }

        public bool Contains(T item)
        {
            return m_linkedList.Contains(item);
        }

        #region Enumerator

        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private LinkedList<T>.Enumerator m_enumerator;

            internal Enumerator(LinkedList<T> linkedList)
            {
                if (linkedList == null)
                {
                    throw new DGameException("链表是无效的");
                }

                m_enumerator = linkedList.GetEnumerator();
            }

            /// <summary>
            /// 获取当前节点
            /// </summary>
            public T Current => m_enumerator.Current;

            /// <summary>
            /// 获取当前枚举数
            /// </summary>
            object IEnumerator.Current => Current;

            /// <summary>
            /// 清理枚举数
            /// </summary>
            public void Dispose()
            {
                m_enumerator.Dispose();
            }

            /// <summary>
            /// 获取下一个节点
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                return m_enumerator.MoveNext();
            }

            /// <summary>
            /// 重置枚举数
            /// </summary>
            public void Reset()
            {
                ((IEnumerator<T>)m_enumerator).Reset();
            }
        }

        #endregion
    }
}