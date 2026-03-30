using System;
using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    /// <summary>
    /// 5 - 创建热更资源下载器
    /// </summary>
    public class CreateDownloaderProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        private int m_curTryCnt;
        private const int MAX_TRY_COUNT = 3;
        private ResourceDownloaderOperation m_downloader;
        private int m_totalDownloadCnt;
        private float m_totalSizeMb;

        public override void OnEnter()
        {
            DLogger.Info("======== 5-创建热更资源下载器 ========");
            // 创建补丁下载器...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Create_Downloader_Begin_Tips);
            CreateDownloader().Forget();
        }

        private async UniTaskVoid CreateDownloader()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            m_downloader = m_resourceModule.CreateResourceDownloader();

            if (m_downloader.TotalDownloadCount == 0)
            {
                DLogger.Info("======== 没有资源需要下载 ========");
                SwitchState<DownloadOverProcedure>();
            }
            else
            {
                DLogger.Info($"======== 总共找到了 {m_downloader.TotalDownloadCount} 个资源文件需要下载 ========");

                // 需要下载新文件 则挂起流程系统
                // 开发者需要在下载前检查磁盘空间
                m_totalDownloadCnt = m_downloader.TotalDownloadCount;
                long totalDownloadBytes = m_downloader.TotalDownloadBytes;
                // 1 MB = 1024 KB = 1024 × 1024 = 1,048,576 Bytes
                const float BYTES_PER_MB = 1024f * 1024f;
                float sizeMB = totalDownloadBytes / BYTES_PER_MB;
                m_totalSizeMb = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
                // 总共找到了 {0} 个补丁文件, 总大小为 {1}MB
                LauncherMgr.ShowMessageBox(Utility.StringUtil.Format(
                        UpdateUIDefine.Instance.Create_Downloader_Total_Download_files_Tips, m_totalDownloadCnt, m_totalSizeMb),
                    StartDownloadFiles, Application.Quit);
            }
        }

        private void StartDownloadFiles()
        {
            SwitchState<DownloadFileProcedure>();
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
        }

        public override void OnDestroy()
        {
        }
    }
}