namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// A command's standard streams: its input, and its output that isn't a result. Write a result
    /// with <see cref="CommandContext.Result"/>; Out is for output that isn't one, such as a file's
    /// contents.
    /// </summary>
    public readonly struct CommandStreams
    {
        readonly ICommandHost m_Host;

        internal CommandStreams(ICommandHost host)
        {
            m_Host = host;
        }

        /// <summary>What the client sends the command; reads are async, so they never hold the Editor.</summary>
        public CommandReader In => new CommandReader(m_Host);

        /// <summary>Takes the command's output that isn't a result.</summary>
        public CommandWriter Out => new CommandWriter(m_Host, CommandStream.Out);

        /// <summary>Takes diagnostics; prefer <see cref="CommandContext.Log"/>, which follows the log level.</summary>
        public CommandWriter Error => new CommandWriter(m_Host, CommandStream.Error);
    }
}
