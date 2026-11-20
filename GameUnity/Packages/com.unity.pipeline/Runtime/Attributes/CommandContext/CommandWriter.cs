using System;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// One of a command's output streams, Out or Error. Text is written as UTF-8 and bytes as they
    /// are, in the order they are written.
    /// </summary>
    public readonly struct CommandWriter
    {
        readonly ICommandHost m_Host;
        readonly CommandStream m_Stream;

        internal CommandWriter(ICommandHost host, CommandStream stream)
        {
            m_Host = host;
            m_Stream = stream;
        }

        /// <summary>Writes <paramref name="text"/>.</summary>
        /// <param name="text">The text to write.</param>
        public void Write(string text)
        {
            var host = Host;
            // Taking the writer ends a character that bytes left unfinished; no text, nothing to end it for.
            if (!string.IsNullOrEmpty(text))
                host.Writer(m_Stream).Write(text);
        }

        /// <summary>Writes <paramref name="text"/> and a line break.</summary>
        /// <param name="text">The text to write.</param>
        public void WriteLine(string text) => Host.Writer(m_Stream).Write(text + "\n");

        /// <summary>
        /// Writes <paramref name="data"/> as it is, such as an image piped on to another program.
        /// A client that takes only text, such as one calling over HTTP, gets it decoded as UTF-8.
        /// </summary>
        /// <param name="data">The bytes to write.</param>
        public void Write(ReadOnlySpan<byte> data) => Host.Write(m_Stream, data);

        ICommandHost Host => m_Host ?? throw CommandContext.NotFromPipeline();
    }
}
