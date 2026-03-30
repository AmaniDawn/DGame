using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    [CreateAssetMenu(menuName = "DGame/ProcedureSettings", fileName = "ProcedureSettings")]
    public sealed class ProcedureSettings : ScriptableObject
    {
        private IProcedureModule m_procedureModule = null;
        private ProcedureBase m_startProcedure = null;

        [SerializeField] public string[] availableProcedureTypeNames = null;
        [SerializeField] public string startProcedureTypeName = null;

        public ProcedureBase CurrentProcedure => m_procedureModule == null ? null : m_procedureModule.CurProcedure;

        public float CurrentProcedureTime => m_procedureModule == null ? 0f : m_procedureModule.CurProcedureTime;

        public async UniTask StartProcedure()
        {
            if (m_procedureModule == null)
            {
                m_procedureModule = ModuleSystem.GetModule<IProcedureModule>();
            }

            if (m_procedureModule == null)
            {
                DLogger.Fatal("流程模块无效");
                return;
            }

            ProcedureBase[] procedures = new ProcedureBase[availableProcedureTypeNames.Length];

            for (int i = 0; i < availableProcedureTypeNames.Length; i++)
            {
                Type procedureType = Utility.AssemblyUtil.GetType(availableProcedureTypeNames[i]);

                if (procedureType == null)
                {
                    DLogger.Error($"无法找到流程状态类型: {availableProcedureTypeNames[i]}");
                    return;
                }

                procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);

                if (procedures[i] == null)
                {
                    DLogger.Error($"无法创建流程状态实例对象: {availableProcedureTypeNames[i]}");
                    return;
                }

                if (startProcedureTypeName == availableProcedureTypeNames[i])
                {
                    m_startProcedure = procedures[i];
                }
            }

            if (m_startProcedure == null)
            {
                DLogger.Error("流程启动状态无效");
                return;
            }

            m_procedureModule.Initialize(ModuleSystem.GetModule<IFsmModule>(), procedures);
            await UniTask.Yield();
            m_procedureModule.StartProcedure(m_startProcedure.GetType());
        }
    }
}