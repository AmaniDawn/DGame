using System;
using System.Collections.Generic;
using System.Threading;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Everything about a command's invocation: the arguments it was called with, its result, a log
    /// that follows the log level the user picked, its standard streams, and a token that signals
    /// cancellation. A command takes it as a parameter of its method; the pipeline passes it.
    /// </summary>
    /// <remarks>
    /// Holds only a handle to the execution, which the pipeline owns, so it is cheap to copy. A
    /// default instance, which the pipeline never passes, throws on use.
    /// </remarks>
    public readonly struct CommandContext
    {
        readonly ICommandHost m_Host;

        internal CommandContext(ICommandHost host)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>The arguments the command was called with, without the pipeline's own options.</summary>
        public IReadOnlyList<string> Arguments => Host.Arguments;

        /// <summary>Writes the command's result in the format the user picked.</summary>
        public CommandResult Result => new CommandResult(Host);

        /// <summary>Logs to Error, as much as the user's <c>--log-level</c> picks.</summary>
        public CommandLog Log => new CommandLog(Host);

        /// <summary>The command's standard streams.</summary>
        public CommandStreams Std => new CommandStreams(Host);

        /// <summary>Cancelled once the user stops the command: Ctrl-C, SIGTERM or its timeout.</summary>
        public CancellationToken CancellationToken => Host.CancellationToken;

        ICommandHost Host => m_Host ?? throw NotFromPipeline();

        internal static InvalidOperationException NotFromPipeline() =>
            new InvalidOperationException("This CommandContext wasn't passed by the pipeline; a command takes the one it is called with.");
    }
}
