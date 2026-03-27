using Fantasy.Entitas;
using GameBattle;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global

namespace GameLogic
{
    /// <summary>
    /// 渲染层对象
    /// </summary>
    public abstract class RenderUnit : Entity, IRenderUnit
    {
        public string UnitName { get; private set; }
        
        public ulong UnitID { get; private set; }
        
        public UnitType UnitType { get; private set; }
        
        /// <summary>
        /// 逻辑层对象
        /// </summary>
        public LogicUnit LogicUnit { get; private set; }
        
        /// <summary>
        /// 是否已销毁
        /// </summary>
        public bool IsDestroyed { get; protected set; }
        
        public float WaitDestroyTime { get; set; } = 0f;
        
        /// <summary>
        /// 渲染层 GameObject
        /// </summary>
        public GameObject gameObject { get; protected set; }
        
        /// <summary>
        /// 渲染层 Transform
        /// </summary>
        public Transform transform { get; protected set; }

        public Vector3 Position => transform != null ? transform.position : Vector3.zero;
        
        public Vector3 Forward => transform != null ? transform.forward : Vector3.forward;
        
        public Quaternion Rotation => transform != null ? transform.rotation : Quaternion.identity;
        
        public bool Visible { get; protected set; } = true;

        #region 初始化相关

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

        public ulong GetPlayerID()
        {
            if (LogicUnit != null)
            {
                return LogicUnit.UnitID;
            }
            return 0;
        }

        public virtual bool NeedBindLogicUnit() => true;

        public string GetGameObjectName()
        {
            if (DGame.Utility.PlatformUtil.IsEditorPlatform())
            {
                return $"[{UnitID}][{LogicUnit.UnitType}][{UnitName}]";
            }
            return "RenderUnit";
        }
        
        public virtual bool IsBoss() => false;
        
        public void OnUnitEvent(int eventId) { }
        
        public bool IsSameUnit(RenderUnit other)
            => other != null && RuntimeId != 0 && other.RuntimeId != 0 && RuntimeId == other.RuntimeId;
    }
}
