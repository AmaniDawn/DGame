namespace DGame
{
    public interface IGameTimerModule
    {
        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timer"></param>
        void Pause(GameTimer timer);

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timer"></param>
        void Resume(GameTimer timer);

        /// <summary>
        /// 计时器是否在运行中
        /// </summary>
        /// <param name="timer"></param>
        bool IsRunning(GameTimer timer);

        /// <summary>
        /// 计时器剩余时间
        /// </summary>
        /// <param name="timer"></param>
        /// <returns></returns>
        float GetTimerLeft(GameTimer timer);

        /// <summary>
        /// 重置计时器到初始状态
        /// </summary>
        /// <param name="timer"></param>
        void Restart(GameTimer timer);

        /// <summary>
        /// 重置计时器 可重新设置循环、时间缩放、回调
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="interval"></param>
        /// <param name="isLoop"></param>
        /// <param name="isUnscaled"></param>
        /// <param name="handler"></param>
        void Reset(GameTimer timer, float interval, bool isLoop, bool isUnscaled, TimerHandler handler);

        /// <summary>
        /// 重置计时器 可重新设置循环、时间缩放
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="interval"></param>
        /// <param name="isLoop"></param>
        /// <param name="isUnscaled"></param>
        void Reset(GameTimer timer, float interval, bool isLoop, bool isUnscaled);

        /// <summary>
        /// 创建受时间缩放影响的Loop次数计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="loopCount">循环次数。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateLoopCountGameTimer(float interval, int loopCount, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建不受时间缩放影响的Loop次数计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="loopCount">循环次数。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateUnscaledLoopCountGameTimer(float interval, int loopCount, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建受时间缩放影响的Loop计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateLoopGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建受时间缩放影响的一次计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateOnceGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建不受时间缩放影响的Loop计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateUnscaledLoopGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建不受时间缩放影响的一次计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateUnscaledOnceGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建计时器
        /// </summary>
        /// <param name="interval">计时器间隔</param>
        /// <param name="isLoop">是否循环计时器</param>
        /// <param name="isUnscaled">是否受时间缩放影响</param>
        /// <param name="handler">计时器回调</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns></returns>
        // GameTimer CreateGameTimer(float interval, bool isLoop, bool isUnscaled, TimerHandler handler,
        //     object[] args = null);

        /// <summary>
        /// 移除计时器。
        /// </summary>
        /// <param name="timer">计时器Id。</param>
        void DestroyGameTimer(GameTimer timer);

        /// <summary>
        /// 移除所有计时器。
        /// </summary>
        void DestroyAllGameTimer();

        /// <summary>
        /// 创建一个一秒触发一次的Loop系统计时器
        /// </summary>
        /// <param name="callback">计时器回调，触发事件的定时器对象 获取定时器触发时的时间信息</param>
        /// <returns></returns>
        // System.Timers.Timer CreateSystemTimer(Action<object, System.Timers.ElapsedEventArgs> callback);
    }
}