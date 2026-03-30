using UnityEngine;
using UnityEngine.Playables;

namespace DGame
{
    internal abstract class AnimNode
    {
        private readonly PlayableGraph m_graph;
        private Playable m_curSourcePlayable;
        private Playable m_parent;

        private float m_fadeSpeed = 0f;
        private float m_fadeWeight = 0f;
        private bool m_isFading = false;

        /// <summary>
        /// 是否已链接
        /// </summary>
        public bool IsConnected { get; private set; } = false;

        /// <summary>
        /// 输入端口
        /// </summary>
        public int InputPort { get; private set; }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsDone => !m_curSourcePlayable.IsNull() && m_curSourcePlayable.IsDone();

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !m_curSourcePlayable.IsNull() && m_curSourcePlayable.IsValid();

        /// <summary>
        /// 是否正在播放中
        /// </summary>
        public bool IsPlaying => !m_curSourcePlayable.IsNull() && m_curSourcePlayable.GetPlayState() == PlayState.Playing;

        /// <summary>
        /// 时间轴
        /// </summary>
        public float Time
        {
            get => (float)m_curSourcePlayable.GetTime();
            set => m_curSourcePlayable.SetTime(value);
        }

        /// <summary>
        /// 播放速度
        /// </summary>
        public float Speed
        {
            get => (float)m_curSourcePlayable.GetSpeed();
            set => m_curSourcePlayable.SetSpeed(value);
        }

        /// <summary>
        /// 权重值
        /// </summary>
        public float Weight
        {
            get => IsConnected ? m_parent.GetInputWeight(InputPort) : 0f;
            set
            {
                if (IsConnected)
                {
                    m_parent.SetInputWeight(InputPort, value);
                }
            }
        }

        protected AnimNode(PlayableGraph graph) => m_graph = graph;

        protected void SetSourcePlayable(Playable playable) => m_curSourcePlayable = playable;

        public virtual void Update(float elapsedSeconds)
        {
            if (m_isFading)
            {
                Weight = Mathf.MoveTowards(Weight, m_fadeWeight, elapsedSeconds * m_fadeSpeed); // 原本是 / 改成了 * 有问题再看

                if (Mathf.Approximately(Weight, m_fadeWeight))
                {
                    m_isFading = false;
                }
            }
        }

        public virtual void Destroy()
        {
            if (IsValid)
            {
                m_graph.DestroySubgraph(m_curSourcePlayable);
            }
        }

        public virtual void Play()
        {
            m_curSourcePlayable.Play();
            m_curSourcePlayable.SetDone(false);
        }

        public virtual void Pause()
        {
            m_curSourcePlayable.Pause();
            m_curSourcePlayable.SetDone(true);
        }

        public virtual void Reset()
        {
            m_fadeSpeed = 0;
            m_fadeWeight = 0;
            m_isFading = false;

            Time = 0;
            Speed = 1;
            Weight = 0;
        }

        /// <summary>
        /// 链接父节点
        /// </summary>
        /// <param name="parent">父节点对象</param>
        /// <param name="parentInputPort">父节点上的输入端口</param>
        public void Connect(Playable parent, int parentInputPort)
        {
            if (IsConnected)
            {
                throw new DGameException("当前节点已经链接父节点");
            }
            m_parent = parent;
            InputPort = parentInputPort;
            Reset();
            // parent.AddInput(m_curPlayable, 0, parentInputPort);
            // 官方推荐使用
            m_graph.Connect(m_curSourcePlayable, 0, parent, parentInputPort);
            IsConnected = true;
        }

        /// <summary>
        /// 同父节点断开连接
        /// </summary>
        /// <exception cref="DGameException"></exception>
        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new DGameException("当前节点没有链接父节点");
            }

            m_parent.DisconnectInput(InputPort);
            // m_graph.Disconnect(m_parent, InputPort);
            IsConnected = false;
        }

        /// <summary>
        /// 开始权重过渡
        /// </summary>
        /// <param name="destWeight">目标权重值</param>
        /// <param name="fadeDuration">过渡时间</param>
        public void StartWeightFade(float destWeight, float fadeDuration)
        {
            if (fadeDuration <= 0)
            {
                Weight = destWeight;
                m_isFading = false;
                return;
            }

            m_fadeSpeed = 1f / fadeDuration;
            m_fadeWeight = destWeight;
            m_isFading = true;
        }
    }
}