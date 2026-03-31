using Fantasy.Entitas;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单位变换同步组件。
    /// <remarks>负责将逻辑层的定点位置与朝向同步到渲染层根节点，并处理初始对齐、平滑插值与瞬移吸附。</remarks>
    /// </summary>
    public sealed class UnitTransformSyncComponent : Entity
    {
        /// <summary>
        /// 当前所属的渲染单位。
        /// </summary>
        public RenderUnit OwnerUnit { get; private set; }

        /// <summary>
        /// 当前是否已经完成首次同步初始化。
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 是否忽略旋转同步。
        /// 为 <see langword="true"/> 时仅同步位置，不修改渲染根节点旋转。
        /// </summary>
        public bool IgnoreRotation { get; set; }

        /// <summary>
        /// 位置平滑时间。
        /// 数值越小，位置越快追上逻辑目标点。
        /// </summary>
        public float PositionSmoothTime { get; set; } = 0.08f;

        /// <summary>
        /// 旋转平滑速度，单位为度每秒。
        /// </summary>
        public float RotationSmoothSpeed { get; set; } = 720f;

        /// <summary>
        /// 触发瞬移吸附的距离阈值。
        /// <remarks>当渲染位置与逻辑目标点距离超过该值时，将直接吸附到目标位置。</remarks>
        /// </summary>
        public float SnapDistance { get; set; } = 6f;

        private Vector3 m_currentPosition;
        private Quaternion m_currentRotation = Quaternion.identity;
        private Vector3 m_positionVelocity;
        private bool m_needSnap;

        /// <summary>
        /// 初始化同步组件。
        /// <remarks>将同步组件与所属的渲染单位关联，并设置默认参数。</remarks>
        /// </summary>
        /// <param name="owner">所属的渲染单位。</param>
        public void Init(RenderUnit owner)
        {
            OwnerUnit = owner;
            IsInitialized = false;
            IgnoreRotation = false;
            PositionSmoothTime = 0.08f;
            RotationSmoothSpeed = 720f;
            SnapDistance = 6f;
            m_currentPosition = Vector3.zero;
            m_currentRotation = Quaternion.identity;
            m_positionVelocity = Vector3.zero;
            m_needSnap = true;
        }

        /// <summary>
        /// 执行首次同步初始化。
        /// <remarks>通常用于单位创建后立即将渲染根节点对齐到逻辑位置与朝向，避免初始帧插值拖尾。</remarks>
        /// </summary>
        public void SyncInit()
        {
            if (!TryGetTarget(out var targetPosition, out var targetRotation))
            {
                return;
            }

            ApplyTransform(targetPosition, targetRotation, true);
            IsInitialized = true;
            m_needSnap = false;
            m_positionVelocity = Vector3.zero;
        }

        /// <summary>
        /// 标记下一次同步时采用吸附方式直接对齐目标位置。
        /// </summary>
        public void MarkSnap()
        {
            m_needSnap = true;
        }

        /// <summary>
        /// 通知逻辑层变换数据已发生变化。
        /// <remarks>当逻辑目标点与当前渲染位置距离过大时，会自动切换为瞬移吸附模式。</remarks>
        /// </summary>
        public void NotifyLogicTransformChanged()
        {
            if (!TryGetTarget(out var targetPosition, out _))
            {
                return;
            }

            if (OwnerUnit?.UnitRootTransform != null)
            {
                var distance = Vector3.Distance(OwnerUnit.UnitRootTransform.position, targetPosition);
                if (distance >= SnapDistance)
                {
                    m_needSnap = true;
                }
            }
        }

        /// <summary>
        /// 根据逻辑层目标位置与朝向执行一次同步。
        /// </summary>
        /// <param name="deltaTime">当前渲染帧时间间隔。</param>
        public void Sync(float deltaTime)
        {
            if (!TryGetTarget(out var targetPosition, out var targetRotation))
            {
                return;
            }

            if (!IsInitialized || m_needSnap)
            {
                ApplyTransform(targetPosition, targetRotation, true);
                IsInitialized = true;
                m_needSnap = false;
                m_positionVelocity = Vector3.zero;
                return;
            }

            var smoothTime = Mathf.Max(0.0001f, PositionSmoothTime);
            m_currentPosition = Vector3.SmoothDamp(m_currentPosition, targetPosition, ref m_positionVelocity, smoothTime,
                Mathf.Infinity, Mathf.Max(0.0001f, deltaTime));

            if (!IgnoreRotation)
            {
                m_currentRotation = Quaternion.RotateTowards(m_currentRotation, targetRotation,
                    RotationSmoothSpeed * Mathf.Max(0.0001f, deltaTime));
            }

            ApplyTransform(m_currentPosition, m_currentRotation, false);
        }

        /// <summary>
        /// 清空运行时缓存状态。
        /// </summary>
        public void Clear()
        {
            OwnerUnit = null;
            IsInitialized = false;
            IgnoreRotation = false;
            m_currentPosition = Vector3.zero;
            m_currentRotation = Quaternion.identity;
            m_positionVelocity = Vector3.zero;
            m_needSnap = false;
        }

        /// <summary>
        /// 从逻辑单位当前快照中提取目标位置与目标朝向。
        /// </summary>
        /// <param name="targetPosition">输出目标位置。</param>
        /// <param name="targetRotation">输出目标旋转。</param>
        /// <returns>提取成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        private bool TryGetTarget(out Vector3 targetPosition, out Quaternion targetRotation)
        {
            targetPosition = Vector3.zero;
            targetRotation = Quaternion.identity;

            var owner = OwnerUnit;
            var logicUnit = owner?.LogicUnit;
            var renderTransform = owner?.UnitRootTransform;
            if (logicUnit == null || renderTransform == null)
            {
                return false;
            }

            var snapshot = logicUnit.StateSync != null ? logicUnit.StateSync.Snapshot : default;
            targetPosition = snapshot.Position.ToVector3() + logicUnit.TranslatePos.ToVector3();

            var forward = snapshot.MoveForward.ToVector3();
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = logicUnit.MoveForward.ToVector3();
            }
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = logicUnit.Forward.ToVector3();
            }
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = renderTransform.forward.sqrMagnitude > 0.0001f ? renderTransform.forward : Vector3.forward;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            targetRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            return true;
        }

        /// <summary>
        /// 将计算结果应用到渲染根节点，并刷新内部缓存状态。
        /// </summary>
        /// <param name="position">目标位置。</param>
        /// <param name="rotation">目标旋转。</param>
        /// <param name="snap">是否为吸附同步。</param>
        private void ApplyTransform(Vector3 position, Quaternion rotation, bool snap)
        {
            var trans = OwnerUnit?.UnitRootTransform;
            if (trans == null)
            {
                return;
            }

            trans.position = position;
            if (!IgnoreRotation)
            {
                trans.rotation = rotation;
            }

            m_currentPosition = position;
            if (!IgnoreRotation || snap)
            {
                m_currentRotation = rotation;
            }
        }
    }
}