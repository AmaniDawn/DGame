using System;

namespace DGame
{
    internal sealed class ProcedureModule : Module, IProcedureModule
    {
        private IFsmModule m_fsmModule = null;
        private IFsm<IProcedureModule> m_procedureFsm = null;

        public ProcedureBase CurProcedure
            => m_procedureFsm == null
            ? throw new DGameException("必须先初始化流程模块状态机")
            : (ProcedureBase)m_procedureFsm.CurrentState;

        public float CurProcedureTime
            => m_procedureFsm == null
            ? throw new DGameException("必须先初始化流程模块状态机")
            : m_procedureFsm.CurrentStateTime;

        public override int Priority => -2;

        public void Initialize(IFsmModule fsmModule, params ProcedureBase[] procedures)
        {
            if (fsmModule == null)
            {
                throw new DGameException("状态机模块无效");
            }
            m_fsmModule = fsmModule;
            m_procedureFsm = m_fsmModule.CreateFsm(this, procedures);
        }

        public void StartProcedure<T>() where T : ProcedureBase
        {
            if (m_procedureFsm == null)
            {
                throw new DGameException("必须先初始化流程模块状态机");
            }
            m_procedureFsm?.Start<T>();
        }

        public void StartProcedure(Type procedureType)
        {
            if (m_procedureFsm == null)
            {
                throw new DGameException("必须先初始化流程模块状态机");
            }
            m_procedureFsm?.Start(procedureType);
        }

        public bool ContainsProcedure<T>() where T : ProcedureBase
        {
            if (m_procedureFsm == null)
            {
                throw new DGameException("必须先初始化流程模块状态机");
            }
            return m_procedureFsm.ContainsFsmState<T>();
        }

        public bool ContainsProcedure(Type procedureType)
        {
            if (m_procedureFsm == null)
            {
                throw new DGameException("必须先初始化流程模块状态机");
            }
            return m_procedureFsm.ContainsFsmState(procedureType);
        }

        public ProcedureBase GetProcedure<T>() where T : ProcedureBase
        {
            if (m_procedureFsm == null)
            {
                throw new DGameException("必须先初始化流程模块状态机");
            }
            return (ProcedureBase)m_procedureFsm.GetState<T>();
        }

        public ProcedureBase GetProcedure(Type procedureType)
        {
            if (m_procedureFsm == null)
            {
                throw new DGameException("必须先初始化流程模块状态机");
            }
            return (ProcedureBase)m_procedureFsm.GetState(procedureType);
        }

        public bool RestartProcedure(params ProcedureBase[] procedures)
        {
            if (procedures == null || procedures.Length == 0)
            {
                throw new DGameException("重启流程失败");
            }

            if (m_fsmModule == null)
            {
                throw new DGameException("状态机模块无效");
            }

            if (!m_fsmModule.DestroyFsm<IProcedureModule>())
            {
                return false;
            }
            Initialize(m_fsmModule, procedures);
            StartProcedure(procedures[0].GetType());
            return true;
        }

        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
            if (m_fsmModule != null)
            {
                if (m_procedureFsm != null)
                {
                    m_fsmModule.DestroyFsm(m_procedureFsm);
                    m_procedureFsm = null;
                }
                m_fsmModule = null;
            }
        }
    }
}