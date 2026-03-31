using System.Collections.Generic;
using System.Linq;
using DGame;
using UnityEngine;
using GameProto;

namespace GameLogic
{
    public class FuncOpenMgr : DataCenterModule<FuncOpenMgr>
    {
        private bool m_init;
        private readonly HashSet<int> m_openFuncSet = new HashSet<int>();
        private readonly List<int> m_newOpenFuncBuffer = new List<int>();
        private readonly Dictionary<int, Transform> m_funcTargetTransform = new Dictionary<int, Transform>();

        #region Override

        public override void OnRoleLogout()
        {
            Clear();
        }

        #endregion

        public void Clear()
        {
            m_init = false;
            m_openFuncSet.Clear();
            m_newOpenFuncBuffer.Clear();
            m_funcTargetTransform.Clear();
        }

        public void Initialize(IEnumerable<int> openFuncList)
        {
            m_openFuncSet.Clear();

            if (openFuncList != null)
            {
                foreach (var funcId in openFuncList)
                {
                    if (funcId > 0)
                    {
                        m_openFuncSet.Add(funcId);
                    }
                }
            }

            m_init = true;
            GameEvent.Get<IFuncOpenLogic>().OnFuncOpenDataChange();
        }

        public void SyncOpenFuncList(IEnumerable<int> openFuncList, bool broadcastNewOpenEvent)
        {
            if (!m_init)
            {
                Initialize(openFuncList);
                return;
            }

            m_newOpenFuncBuffer.Clear();

            if (openFuncList != null)
            {
                foreach (var funcId in openFuncList)
                {
                    if (funcId <= 0)
                    {
                        continue;
                    }

                    if (m_openFuncSet.Add(funcId))
                    {
                        m_newOpenFuncBuffer.Add(funcId);
                    }
                }
            }

            if (m_newOpenFuncBuffer.Count == 0)
            {
                return;
            }

            GameEvent.Get<IFuncOpenLogic>().OnFuncOpenDataChange();

            if (!broadcastNewOpenEvent)
            {
                return;
            }

            foreach (var funcId in m_newOpenFuncBuffer.OrderBy(static id => id))
            {
                GameEvent.Get<IFuncOpenLogic>().OnFuncOpen((FuncType)funcId);
            }
        }

        #region CheckFuncOpen

        public bool CheckFuncOpen(FuncType funcType, bool showTips = false)
            => CheckFuncOpen((int)funcType, showTips);

        public bool CheckFuncOpen(int funcType, bool showTips = false)
        {
            bool isOpen = m_openFuncSet.Contains(funcType);

            if (!isOpen && showTips && FuncOpenConfigMgr.Instance.TryGetValue(funcType, out var cfg))
            {
                GameModule.UIModule.ShowTipsUI((uint)cfg.NoOpenTipsID);
            }

            return isOpen;
        }

        #endregion

        public void RegisterTargetTransform(FuncType funcType, Transform transform)
        {
            if (transform == null)
            {
                return;
            }

            m_funcTargetTransform[(int)funcType] = transform;
        }

        public void UnregisterTargetTransform(FuncType funcType)
        {
            m_funcTargetTransform.Remove((int)funcType);
        }

        public Transform GetTargetTransform(FuncType funcType)
            => m_funcTargetTransform.TryGetValue((int)funcType, out var transform) ? transform : null;
    }
}