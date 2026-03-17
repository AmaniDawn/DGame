using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
    public class FuncOpenMgr : DataCenterModule<FuncOpenMgr>
    {
        private bool m_init;
        private List<int> m_openFuncList = new List<int>();

        #region Override

        public override void OnRoleLogout()
        {
            m_init = false;
            m_openFuncList.Clear();
        }

        #endregion

        #region CheckFuncOpen

        public bool CheckFuncOpen(FuncType funcType, bool showTips = false)
            => CheckFuncOpen((int)funcType, showTips);

        public bool CheckFuncOpen(int funcType, bool showTips = false)
        {
            bool isOpen = m_openFuncList.Contains(funcType);

            if (!isOpen && showTips && TbFuncOpenConfig.TryGetValue(funcType, out var cfg))
            {
                UIModule.Instance.ShowTipsUI((uint)cfg.NoOpenTipsId);
            }
            return isOpen;
        }

        #endregion
    }
}