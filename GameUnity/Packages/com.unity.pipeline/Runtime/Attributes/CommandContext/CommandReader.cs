using System;
using System.Threading.Tasks;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// A command's input: what the client sends it, such as text or a file piped to the <c>unity</c> CLI.
    /// </summary>
    /// <remarks>
    /// Reads wait for the client without holding the Editor, so await them from a command that
    /// returns <c>Task&lt;ExitCode&gt;</c>. <see cref="ReadAsync"/> reads the bytes the client sent,
    /// as they are; <see cref="ReadLineAsync"/> reads a line of them as UTF-8. Both read from one
    /// position, so a command may mix them, such as a header line followed by binary. The input ends
    /// once the client ends it, or once the command is cancelled or ends, so a read never outlasts
    /// the command: after reading to the end, check the cancellation token. A command run over
    /// HTTP, or by another command such as <c>job batch</c>, has no input.
    /// </remarks>
    public readonly struct CommandReader
    {
        readonly ICommandHost m_Host;

        internal CommandReader(ICommandHost host)
        {
            m_Host = host;
        }

        /// <summary>
        /// Reads the next line, decoded as UTF-8, without its line break (<c>\n</c>, <c>\r</c> or
        /// <c>\r\n</c>). Waits while the client hasn't sent it. A line that ends in <c>\r</c> waits
        /// for the next byte, to tell <c>\r</c> from <c>\r\n</c>, and takes it when it is <c>\n</c>:
        /// end a line that binary follows with <c>\n</c>.
        /// </summary>
        /// <returns>The line, or null once the input ended and everything in it was read.</returns>
        public ValueTask<string> ReadLineAsync() => Host.ReadLineAsync();

        /// <summary>
        /// Reads the bytes the client sent, as they are, into <paramref name="buffer"/>: as many as
        /// have arrived, up to its length. Waits while none has.
        /// </summary>
        /// <param name="buffer">Where the bytes go.</param>
        /// <returns>How many bytes were read; 0 once the input ended and everything in it was read.</returns>
        public ValueTask<int> ReadAsync(Memory<byte> buffer) => Host.ReadAsync(buffer);

        ICommandHost Host => m_Host ?? throw CommandContext.NotFromPipeline();
    }
}
