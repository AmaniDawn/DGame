using System;

namespace DGame
{
    /// <summary>
    /// 对象基类 被内存池和对象池同时管理
    /// </summary>
    public abstract class BasePoolObject : IMemory
    {
        /// <summary>
        /// 对象名字
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// 对象
        /// </summary>
        public object Target { get; private set; } = null;

        /// <summary>
        /// 对象是否加锁
        /// </summary>
        public bool Locked { get; set; } = false;

        /// <summary>
        /// 对象优先级
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 对象最后一次使用时间
        /// </summary>
        public DateTime LastUseTime { get; internal set; } = default(DateTime);

        /// <summary>
        /// 自定义释放检查标记
        /// </summary>
        public virtual bool CustomCanReleaseFlag => true;

        #region Initialize

        public void Initialize(object target)
        {
            Initialize(string.Empty, Target);
        }

        public void Initialize(string name, object target)
        {
            Initialize(name, target, 0);
        }

        public void Initialize(string name, object target, int priority)
        {
            Initialize(name, target, false, priority);
        }

        public void Initialize(string name, object target, bool locked)
        {
            Initialize(name, target, locked, 0);
        }

        public void Initialize(string name, object target, bool locked, int priority)
        {
            if (target == null)
            {
                throw new DGameException(Utility.StringUtil.Format("对象资源 '{0}' 是无效的", name));
            }
            Name = string.IsNullOrEmpty(name) ? string.Empty : name;
            Target = target;
            Locked = locked;
            Priority = priority;
            LastUseTime = DateTime.UtcNow;
        }

        #endregion

        #region 内存池方法

        /// <summary>
        /// 回收到内存池 初始化信息
        /// </summary>
        public virtual void OnRelease()
        {
            Name = null;
            Target = null;
            Locked = false;
            Priority = 0;
            LastUseTime = default(DateTime);
        }

        #endregion

        #region 对象池

        /// <summary>
        /// 从对象池中取出
        /// </summary>
        protected internal virtual void OnSpawn()
        { }

        /// <summary>
        /// 回收到对象池
        /// </summary>
        protected internal virtual void OnRecycle()
        { }

        /// <summary>
        /// 从对象池中释放都对象（回收到内存池）
        /// </summary>
        /// <param name="isDestroy">是否在对象池销毁时触发</param>
        protected internal abstract void ReleaseObj(bool isDestroy);

        #endregion
    }
}