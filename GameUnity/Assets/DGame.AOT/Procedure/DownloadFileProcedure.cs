using System;
using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    public class DownloadFileProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        // 上一次采样的下载字节数
        private float m_lastUpdateDownloadSize;
        // 累计速度总和
        private float m_totalSpeed;
        // 采样次数
        private int m_speedSampleCnt;

        private float CurrentSpeed
        {
            get
            {
                // 获取距离上一次调用的时间间隔
                float interval = Mathf.Max(Time.deltaTime, 0.01f);
                // 本次采样期间下载的字节数 = 本次下载的字节数 - 上一次采样下载的字节数
                var sizeDiff = m_resourceModule.Downloader.CurrentDownloadBytes - m_lastUpdateDownloadSize;
                // 记录本次下载的字节数
                m_lastUpdateDownloadSize = m_resourceModule.Downloader.CurrentDownloadBytes;
                // 瞬时速度 = 字节差 / 时间间隔
                var speed = sizeDiff / interval;

                // 累加瞬时速度
                m_totalSpeed += speed;
                // 增加采样计数
                m_speedSampleCnt++;
                // 返回平均速度
                return m_totalSpeed / m_speedSampleCnt;
            }
        }

        public override void OnEnter()
        {
            DLogger.Info("======== 6-开始下载更新资源文件 ========");
            // 开始下载更新文件...
            LauncherMgr.ShowUI<LoadUpdateUI>(UpdateUIDefine.Instance.Download_File_Begin_Tips);
            BeginDownload().Forget();
        }

        private async UniTaskVoid BeginDownload()
        {
            var downloader = m_resourceModule.Downloader;
            downloader.DownloadErrorCallback = OnDownloadErrorCallback;
            downloader.DownloadUpdateCallback = OnDownloadUpdateCallback;
            downloader.BeginDownload();
            await downloader;

            if (downloader.Status != EOperationStatus.Succeed)
            {
                return;
            }
            SwitchState<DownloadOverProcedure>();
        }

        private void OnDownloadUpdateCallback(DownloadUpdateData data)
        {
            // 1 MB = 1024 KB = 1024 × 1024 = 1,048,576 Bytes
            const float BYTES_PER_MB = 1024f * 1024f;
            string currentSizeMb = (data.CurrentDownloadBytes / BYTES_PER_MB).ToString("f1");
            string totalSizeMb = (data.TotalDownloadBytes / BYTES_PER_MB).ToString("f1");
            float progressPercentage = m_resourceModule.Downloader.Progress * 100;
            string speed = Utility.File.GetLengthString((int)CurrentSpeed);
            // 正在更新，已更新 {0}/{1} ({2:F2}%)
            string line1 = Utility.StringUtil.Format(UpdateUIDefine.Instance.Download_File_Update_Line1_Tips, data.CurrentDownloadCount,
                data.TotalDownloadCount, progressPercentage);
            // 已更新大小 {0}MB/{1}MB
            string line2 = Utility.StringUtil.Format(UpdateUIDefine.Instance.Download_File_Update_Line2_Tips, currentSizeMb, totalSizeMb);
            // 当前网速 {0}/s，剩余时间 {1}
            string line3 = Utility.StringUtil.Format(UpdateUIDefine.Instance.Download_File_Update_Line3_Tips, speed,
                GetRemainingTime(data.TotalDownloadBytes, data.CurrentDownloadBytes, CurrentSpeed));

            LauncherMgr.RefreshProgress(m_resourceModule.Downloader.Progress);
            LauncherMgr.ShowUI<LoadUpdateUI>($"{line1}\n{line2}\n{line3}");

            DLogger.Info($"{line1} {line2} {line3}");
        }

        private string GetRemainingTime(long totalDownloadBytes, long currentDownloadBytes, float speed)
        {
            int needTime = 0;
            if (speed > 0)
            {
                // 计算剩余时间（秒） （总需要下载字节 - 当前下载字节） / 下载速度
                needTime = (int)((totalDownloadBytes - currentDownloadBytes) / speed);
            }
            // 将秒数转换为TimeSpan对象
            TimeSpan ts = new TimeSpan(0, 0, needTime);
            // 格式化为 "时:分:秒" 的字符串格式
            return ts.ToString(@"hh\:mm\:ss");
        }

        private void OnDownloadErrorCallback(DownloadErrorData data)
        {
            // 下载补丁文件失败: {0}
            LauncherMgr.ShowMessageBox(Utility.StringUtil.Format(UpdateUIDefine.Instance.Download_File_Error_Tips, data.FileName),
                SwitchState<CreateDownloaderProcedure>, Application.Quit);
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