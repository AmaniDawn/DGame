using System;

#pragma warning disable UAC1001

namespace DGame
{
    public delegate void TimerHandler(object[] args);

    [Serializable]
    public class GameTimer
    {
        public int TimerID = 0;
        public float IntervalTime = 0;
        public float TriggerTime = 0;
        public int LoopCount = 0;
        public bool IsLoop = false;
        public bool IsNeedRemove = false;
        public bool IsDestroyed = false;
        public bool IsRunning = false;
        public bool IsUnscaled = false;
        public bool HasLoopCount = false;
        public object[] Args = null;
        public TimerHandler Handler = null;

        public GameTimer(int timerID, float intervalTime, bool isUnscaled, bool isLoop, TimerHandler handler,
            object[] args = null)
        {
            TimerID = timerID;
            IntervalTime = intervalTime;
            Handler = handler;
            IsLoop = isLoop;
            Args = args;
            IsNeedRemove = false;
            IsRunning = true;
            LoopCount = 0;
            IsUnscaled = isUnscaled;
            HasLoopCount = false;
            TriggerTime = IntervalTime;
        }

        public GameTimer(int timerID, float intervalTime, bool isUnscaled, int loopCount, TimerHandler handler,
            object[] args = null)
        {
            TimerID = timerID;
            IntervalTime = intervalTime;
            Handler = handler;
            IsLoop = true;
            LoopCount = loopCount;
            HasLoopCount = true;
            Args = args;
            IsNeedRemove = false;
            IsRunning = true;
            IsUnscaled = isUnscaled;
            TriggerTime = IntervalTime;
        }


        public static bool IsNull(GameTimer timer)
        {
            return timer == null || timer.IsDestroyed;
        }

        internal void Destroy()
        {
            TimerID = 0;
            IntervalTime = 0;
            TriggerTime = 0;
            LoopCount = 0;
            IsLoop = false;
            IsNeedRemove = false;
            IsRunning = false;
            IsUnscaled = false;
            Args = null;
            Handler = null;
            HasLoopCount = false;
            IsDestroyed = true;
        }
    }
}