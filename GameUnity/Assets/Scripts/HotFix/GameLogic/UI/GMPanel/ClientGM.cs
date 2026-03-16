using System;
using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public delegate void HandleGM(List<string> @params);

    public class ClientGM : Singleton<ClientGM>, IUpdate
    {
        private readonly Dictionary<string, HandleGM> m_cmdDict = new Dictionary<string, HandleGM>();

        private readonly List<CacheGm> m_commendCfgList = new List<CacheGm>();

        /// <summary>
        /// 缓存输入过的命令
        /// </summary>
        private readonly List<string> m_commendList = new List<string>();

        private readonly string[] m_gmTextSplitArr = new[] { " " };

        private GmCmdHandler m_cmdHandler;

        public bool debugLowFps = false;
        public bool debugNotchScreen = false;

        protected override void OnInit()
        {
            m_cmdHandler = new GmCmdHandler();
            m_cmdHandler.Init();
        }

        public void HandleClientGm(string gmText)
        {
            gmText = gmText.Trim();
            DLogger.Info($"type: {gmText.GetType().FullName}");
            string[] strArr = gmText.Split(m_gmTextSplitArr, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
            DLogger.Info($"split array length: {strArr.Length}");
            List<string> gmParams = new List<string>();
            string cmd = strArr[0];

            for (int i = 1; i < strArr.Length; i++)
            {
                string param1 = strArr[i].Trim();

                if (string.IsNullOrEmpty(param1))
                {
                    continue;
                }
                gmParams.Add(param1);
            }
            DLogger.Warning($"ClientGM: {gmText} param count: {gmParams.Count}");

            if (m_cmdDict.TryGetValue(cmd, out HandleGM handler))
            {
                handler(gmParams);
            }
        }

        public void RegisterGmCmd(string cmd, HandleGM handler)
        {
            if (!m_cmdDict.TryAdd(cmd, handler))
            {
                DLogger.Error($"repeat GmCmd: {cmd}");
            }
        }

        public bool TryGetCommendByIndex(int index, out string commend)
        {
            bool inRange = false;
            commend = string.Empty;

            if (index >= 0 && index < m_commendList.Count)
            {
                inRange = true;
                commend = m_commendList[index];
            }

            if (index >= m_commendList.Count && m_commendList.Count > 0)
            {
                commend = m_commendList[0];
                inRange = false;
            }
            return inRange;
        }

        public void AddCommend(string commend)
        {
            m_commendList.Insert(0, commend);
        }

        public bool GetCommendCfgByIndex(int index, out CacheGm commend)
        {
            var inRange = false;
            commend = null;

            if (index >= 0 && index < m_commendCfgList.Count)
            {
                inRange = true;
                commend = m_commendCfgList[index];
            }

            if (index >= m_commendCfgList.Count && m_commendCfgList.Count > 0)
            {
                commend = m_commendCfgList[0];
                inRange = false;
            }

            return inRange;
        }

        public void AddCommendCfg(CacheGm commendCfg)
        {
            m_commendCfgList.Insert(0, commendCfg);
        }

        public void OnUpdate()
        {
            if (!DGame.Utility.PlatformUtil.IsPcOrEditorPlatform())
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                UIModule.Instance.ShowWindowAsync<GMPanel>();
            }
        }
    }
}