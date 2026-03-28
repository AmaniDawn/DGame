using Fantasy.Entitas;
using GameBattle;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace GameLogic
{
    /// <summary>
    /// 渲染层单位基类。
    /// </summary>
    public class RenderUnit : Entity, IRenderUnit
    {
        /// <summary>
        /// 单位名称。
        /// </summary>
        public string UnitName { get; private set; }

        /// <summary>
        /// 单位唯一标识。
        /// </summary>
        public ulong UnitID { get; private set; }

        /// <summary>
        /// 单位类型。
        /// </summary>
        public UnitType UnitType { get; private set; }

        /// <summary>
        /// 绑定的逻辑层单位。
        /// </summary>
        public LogicUnit LogicUnit { get; private set; }

        /// <summary>
        /// 渲染层单位是否已销毁。
        /// </summary>
        public bool IsDestroyed { get; protected set; }

        /// <summary>
        /// 延迟销毁计时。
        /// </summary>
        public float WaitDestroyTime { get; set; } = 0f;

        /// <summary>
        /// 渲染对象根节点。
        /// </summary>
        public GameObject gameObject { get; protected set; }

        /// <summary>
        /// 渲染对象根变换。
        /// </summary>
        public Transform transform { get; protected set; }

        /// <summary>
        /// 当前世界坐标位置。
        /// </summary>
        public Vector3 Position => transform != null ? transform.position : Vector3.zero;

        /// <summary>
        /// 当前世界朝向。
        /// </summary>
        public Vector3 Forward => transform != null ? transform.forward : Vector3.forward;

        /// <summary>
        /// 当前世界旋转。
        /// </summary>
        public Quaternion Rotation => transform != null ? transform.rotation : Quaternion.identity;

        /// <summary>
        /// 渲染对象当前是否可见。
        /// </summary>
        public bool Visible { get; protected set; } = true;

        #region 初始化相关

        /// <summary>
        /// 初始化渲染层单位并绑定对应的逻辑层单位。
        /// </summary>
        /// <param name="logicUnit">要绑定的逻辑层单位。</param>
        /// <returns>初始化成功时返回 <see langword="true"/>。</returns>
        public bool Init(LogicUnit logicUnit)
        {
            LogicUnit = logicUnit;
            UnitType = logicUnit.UnitType;
            UnitName = logicUnit.UnitName;
            InternalInit();
            if (!OnInit(logicUnit))
            {
                return false;
            }

            return InternalAfterInit();
        }

        private bool InternalAfterInit()
        {
            if (gameObject != null)
            {
                gameObject.name = GetGameObjectName();
            }

            return true;
        }

        protected virtual void InternalInit() { }

        protected virtual bool OnInit(LogicUnit logicUnit)
        {
            return true;
        }

        #endregion

        protected virtual void OnDestroy() { }

        private void DestroyAllGameTimer() { }

        /// <summary>
        /// 销毁渲染层单位及其关联对象。
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            OnDestroy();

            if (gameObject != null)
            {
                Object.Destroy(gameObject);
                gameObject = null;
                transform = null;
            }

            DestroyAllGameTimer();
            IsDestroyed = true;
            UnitID = 0;
            LogicUnit = null;
            UnitType = UnitType.None;
            Visible = true;
            WaitDestroyTime = 0;
        }

        /// <summary>
        /// 指示当前渲染单位是否需要绑定逻辑层单位。
        /// </summary>
        /// <returns>默认返回 <see langword="true"/>。</returns>
        public virtual bool NeedBindLogicUnit() => true;

        /// <summary>
        /// 指示当前单位是否属于 Boss。
        /// </summary>
        /// <returns>默认返回 <see langword="false"/>。</returns>
        public virtual bool IsBoss() => false;

        /// <summary>
        /// 处理来自逻辑层的单位事件。
        /// </summary>
        /// <param name="eventId">事件标识。</param>
        public virtual void OnUnitEvent(int eventId) { }
        
        /// <summary>
        /// 获取在编辑器中展示的游戏对象名称。
        /// </summary>
        /// <returns>编辑器下返回包含单位信息的名称，运行时返回通用名称。</returns>
        public string GetGameObjectName()
        {
            if (DGame.Utility.PlatformUtil.IsEditorPlatform())
            {
                return $"[{UnitID}][{LogicUnit.UnitType}][{UnitName}]";
            }

            return "RenderUnit";
        }
    }
}