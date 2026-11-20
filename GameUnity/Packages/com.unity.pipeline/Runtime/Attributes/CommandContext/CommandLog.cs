namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Logs to Error, as much as the user's <c>--log-level</c> picks (<c>warning</c> by default). The
    /// level never changes what goes to Out.
    /// </summary>
    public readonly struct CommandLog
    {
        readonly ICommandHost m_Host;

        internal CommandLog(ICommandHost host)
        {
            m_Host = host;
        }

        /// <summary>The command failed. Pair it with a non-zero exit code, and say what the user can do about it.</summary>
        /// <param name="message">The error message.</param>
        public void Error(string message) => Host.Log(CommandLogLevel.Error, message);

        /// <summary>Something unexpected happened, but the result is still valid.</summary>
        /// <param name="message">The warning message.</param>
        public void Warning(string message) => Host.Log(CommandLogLevel.Warning, message);

        /// <summary>What the command did, at the level of milestones rather than individual items.</summary>
        /// <param name="message">The informational message.</param>
        public void Info(string message) => Host.Log(CommandLogLevel.Info, message);

        /// <summary>Decisions and internal state that help diagnose a problem.</summary>
        /// <param name="message">The diagnostic message.</param>
        public void Debug(string message) => Host.Log(CommandLogLevel.Debug, message);

        ICommandHost Host => m_Host ?? throw CommandContext.NotFromPipeline();
    }
}
