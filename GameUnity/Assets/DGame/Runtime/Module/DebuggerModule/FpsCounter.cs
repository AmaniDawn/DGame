namespace DGame
{
    public partial class DebuggerDriver
    {
        public sealed class FpsCounter
        {
            private float m_updateInterval;
            private float m_currentFps;
            private float m_frames;
            private float m_accumulator;
            private float m_timeLeft;

            public float CurrentFps => m_currentFps;

            public float UpdateInterval
            {
                get => m_updateInterval;
                set
                {
                    if (value <= 0)
                    {
                        DLogger.Error("更新间隔无效");
                        return;
                    }
                    m_updateInterval = value;
                    Reset();
                }
            }

            public FpsCounter(float updateInterval)
            {
                if (updateInterval < 0)
                {
                    DLogger.Error("更新间隔无效");
                    return;
                }
                m_updateInterval = updateInterval;
                Reset();
            }

            public void Update(float elapsedTime, float realElapsedSeconds)
            {
                m_frames++;
                m_accumulator += realElapsedSeconds;
                m_timeLeft -= realElapsedSeconds;

                if (m_timeLeft <= 0)
                {
                    m_currentFps = m_accumulator > 0f ? m_frames / m_accumulator : 0f;
                    m_frames = 0;
                    m_accumulator = 0f;
                    m_timeLeft += m_updateInterval;
                }
            }

            private void Reset()
            {
                m_currentFps = 0f;
                m_frames = 0f;
                m_accumulator = 0f;
                m_timeLeft = 0f;
            }
        }
    }
}