using System;

namespace DGame
{
    public abstract class BaseFsm
    {
        private string m_name;

        public BaseFsm()
        {
            m_name = string.Empty;
        }

        /// <summary>
        /// 有限状态机名称
        /// </summary>
        public string Name
        {
            get => m_name;
            set => m_name = string.IsNullOrEmpty(value) ? String.Empty : value;
        }

        /// <summary>
        /// 有限状态机持有者类型
        /// </summary>
        public abstract Type OwnerType { get; }

        /// <summary>
        /// 有限状态机完整名称
        /// </summary>
        public string FullName => new TypeNamePair(OwnerType, m_name).ToString();

        /// <summary>
        /// 状态机中状态的数量
        /// </summary>
        public abstract int FsmStateCount { get; }

        /// <summary>
        /// 有限状态机是否正在运行
        /// </summary>
        public abstract bool IsRunning { get; }

        /// <summary>
        /// 有限状态机是否被销毁
        /// </summary>
        public abstract bool IsDestroyed { get; }

        /// <summary>
        /// 当前状态名称
        /// </summary>
        public abstract string CurrentStateName { get; }

        /// <summary>
        /// 当前有限状态机状态持续时间
        /// </summary>
        public abstract float CurrentStateTime { get; }

        /// <summary>
        /// 状态机轮询
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        /// <param name="realElapsedSeconds"></param>
        internal abstract void Update(float elapsedSeconds, float realElapsedSeconds);

        /// <summary>
        /// 状态机物理轮询
        /// </summary>
        internal abstract void FixedUpdate();

        /// <summary>
        /// 销毁状态机
        /// </summary>
        internal abstract void Destroy();
    }
}