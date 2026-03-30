using System.Diagnostics;
using DGame;

namespace GameLogic
{
    public class GameTickWatcher
    {
        private readonly Stopwatch m_stopwatch = new Stopwatch();

        public GameTickWatcher() => m_stopwatch.Start();

        public void Restart() => m_stopwatch.Restart();

        /// <summary>
        /// 获取用时。
        /// </summary>
        /// <returns></returns>
        public float ElapseTime() => (float)m_stopwatch.Elapsed.TotalSeconds;

        /// <summary>
        /// 输出用时。
        /// </summary>
        public void LogUsedTime() => DLogger.Info($"Used Time: {this.ElapseTime()}");

        public override string ToString() => $"Used Time: {this.ElapseTime()}";
    }
}