using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// A command's standard streams, as a process has them: what the client sends it, where its
    /// output goes, and where its errors go. Also what <see cref="CommandContext"/> reads from the
    /// request: the arguments without the executor's options, and the log level.
    /// </summary>
    /// <remarks>
    /// Out takes output that isn't a result, such as text the command lays out itself. A result is
    /// written through <see cref="CommandExecutionContext.Result"/>, never both in one command.
    /// </remarks>
    sealed class StdStreams
    {
        readonly object m_WriteLock = new object();

        /// <summary>
        /// Where bytes for Out and Error go as they are; null for a stream that carries only text,
        /// whose bytes are decoded as UTF-8 by <see cref="m_OutDecoder"/> and <see cref="m_ErrorDecoder"/>.
        /// </summary>
        readonly IByteWriter m_OutBytes;
        readonly IByteWriter m_ErrorBytes;
        readonly Decoder m_OutDecoder = Encoding.UTF8.GetDecoder();
        readonly Decoder m_ErrorDecoder = Encoding.UTF8.GetDecoder();

        /// <summary>What Out and Error keep, for streams made by <see cref="Kept"/>; null otherwise.</summary>
        readonly CappedWriter m_KeptOut;
        readonly CappedWriter m_KeptError;

        /// <summary>Whether <see cref="Log"/> formats lines for the client; false where Error is read back as data.</summary>
        readonly bool m_FormatsLogs;

        /// <summary>Set by <see cref="Close"/>: what the command writes after it is dropped.</summary>
        volatile bool m_Closed;

        /// <param name="request">The request whose streams these are.</param>
        /// <exception cref="ArgumentException">The request's executor options can't be read.</exception>
        public StdStreams(CommandRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            In = request.In;
            m_OutBytes = request.OutBytes;
            m_ErrorBytes = request.ErrorBytes;
            Out = new GatedWriter(request.Out, this, notesError: false);
            Error = new GatedWriter(request.Error, this, notesError: true);
            OutIsTerminal = request.OutIsTerminal;
            ErrorIsTerminal = request.ErrorIsTerminal;
            m_FormatsLogs = request.FormatsLogs;
            var arguments = CommandRequest.ReadOptions(request.Arguments, out _, out _, out _, out _, out var logLevel, out var logFormat);
            // A grouped invocation (test run --x 1) leads with the rest of the command's name; the
            // command sees only what follows, the same as when its name came whole.
            arguments.RemoveRange(0, Math.Min(request.NamingArguments, arguments.Count));
            Arguments = arguments;
            LogLevel = logLevel;
            LogFormat = logFormat;
        }

        StdStreams(CommandLogLevel logLevel, int maxKeptChars, CommandLogFormat? logFormat)
        {
            m_FormatsLogs = logFormat.HasValue;
            LogFormat = logFormat ?? CommandLogFormat.Human;
            // Both lock m_WriteLock, the lock KeptOut and KeptError read under.
            m_KeptOut = new CappedWriter(maxKeptChars, m_WriteLock);
            m_KeptError = new CappedWriter(maxKeptChars, m_WriteLock);
            In = CommandInput.Empty;
            Out = m_KeptOut;
            Error = m_KeptError;
            Arguments = Array.Empty<string>();
            LogLevel = logLevel;
        }

        /// <summary>
        /// How many characters <see cref="Kept"/> streams hold of each of Out and Error by default:
        /// 64K, well past the 16 KB a <c>job batch</c> op keeps of either, so a cut there still
        /// shows the text was longer, while a command that writes megabytes doesn't hold them all.
        /// </summary>
        internal const int MaxKeptChars = 64 * 1024;

        /// <summary>
        /// Streams that keep what a command run by another one writes, rather than send it to the
        /// client, whose Out takes the outer command's result. Such a command reads nothing, and has
        /// no arguments of its own since its parameters come by name.
        /// </summary>
        /// <param name="logLevel">How much it logs: the outer command's level.</param>
        /// <param name="maxKeptChars">How much of each stream is kept; what follows is dropped.</param>
        internal static StdStreams Kept(CommandLogLevel logLevel, int maxKeptChars = MaxKeptChars) =>
            new StdStreams(logLevel, maxKeptChars, logFormat: null);

        /// <summary>
        /// Like <see cref="Kept"/>, but Error keeps each log line as the client would get it in
        /// <paramref name="logFormat"/> rather than the message alone, for a caller that reads the
        /// levels back, as tests do.
        /// </summary>
        internal static StdStreams KeptFormatted(CommandLogLevel logLevel, CommandLogFormat logFormat, int maxKeptChars = MaxKeptChars) =>
            new StdStreams(logLevel, maxKeptChars, logFormat);

        /// <summary>What Out kept, for streams made by <see cref="Kept"/>; null otherwise.</summary>
        internal string KeptOut
        {
            get
            {
                return m_KeptOut?.ToString();
            }
        }

        /// <summary>What Error kept, for streams made by <see cref="Kept"/>; null otherwise.</summary>
        internal string KeptError
        {
            get
            {
                return m_KeptError?.ToString();
            }
        }

        /// <summary>What the client sends the command.</summary>
        public CommandInput In { get; }

        /// <summary>Takes the command's output.</summary>
        public TextWriter Out { get; }

        /// <summary>Takes why the command failed.</summary>
        public TextWriter Error { get; }

        /// <summary>
        /// Whether Error was told why the command failed: an error logged, or anything written to it
        /// directly. A warning, info or debug line doesn't count.
        /// </summary>
        internal bool WroteError { get; private set; }

        /// <summary>
        /// Ends the streams to the client once the command's run is over: the exit was sent, so
        /// what a command still running writes after it is dropped rather than sent after it.
        /// </summary>
        internal void Close()
        {
            m_Closed = true;
        }

        /// <summary>Whether the client's Out is a terminal, so output may be colored.</summary>
        public bool OutIsTerminal { get; }

        /// <summary>Whether the client's Error is a terminal, so logs may be colored.</summary>
        public bool ErrorIsTerminal { get; }

        /// <summary>The arguments passed to the command, without its name or the executor's own options.</summary>
        public IReadOnlyList<string> Arguments { get; }

        /// <summary>How much the command logs to Error.</summary>
        public CommandLogLevel LogLevel { get; }

        /// <summary>How the command's logs are written to Error.</summary>
        public CommandLogFormat LogFormat { get; }

        /// <summary>
        /// Writes <paramref name="message"/> to Error when <see cref="LogLevel"/> includes <paramref name="level"/>.
        /// Where Error is read back as data (an HTTP response, a nested run's kept text), the line is the
        /// message alone. Where it goes to the client: with <see cref="CommandLogFormat.Json"/>, one object with its <c>level</c> and <c>message</c> per line; otherwise the message, after
        /// a label naming its level for every level but info, such as <c>error: </c>, colored when
        /// <see cref="ErrorIsTerminal"/>.
        /// </summary>
        public void Log(CommandLogLevel level, string message)
        {
            if (level > LogLevel)
                return;
            string line = m_FormatsLogs ? FormatLine(level, message) : message + "\n";

            // Only an error explains a failure; a warning before it doesn't.
            if (level != CommandLogLevel.Error && Error is GatedWriter gated)
                gated.WriteUnnoted(line);
            else
                Error.Write(line);
        }

        /// <summary>The line <see cref="Log"/> writes to the client: JSON, or labeled and maybe colored.</summary>
        string FormatLine(CommandLogLevel level, string message)
        {
            string line;
            if (LogFormat == CommandLogFormat.Json)
                line = new JObject { ["level"] = LevelName(level), ["message"] = message }.ToString(Formatting.None) + "\n";
            else if (level == CommandLogLevel.Info)
                line = message + "\n";
            else if (ErrorIsTerminal)
                line = LabelColor(level) + LevelName(level) + ":" + k_Reset + " " + message + "\n";
            else
                line = LevelName(level) + ": " + message + "\n";
            return line;
        }

        const string k_Reset = "\x1b[0m";

        /// <summary>The color of <paramref name="level"/>'s label: bold red, yellow or dim.</summary>
        static string LabelColor(CommandLogLevel level)
        {
            switch (level)
            {
                case CommandLogLevel.Error: return "\x1b[1;31m";
                case CommandLogLevel.Warning: return "\x1b[33m";
                default: return "\x1b[2m";
            }
        }

        /// <summary>The name <see cref="CommandRequest.LogLevelOption"/> gives <paramref name="level"/>.</summary>
        static string LevelName(CommandLogLevel level)
        {
            switch (level)
            {
                case CommandLogLevel.Error: return "error";
                case CommandLogLevel.Warning: return "warning";
                case CommandLogLevel.Info: return "info";
                default: return "debug";
            }
        }

        /// <summary>
        /// The writer for <paramref name="stream"/>, to write text to it. On a stream that carries
        /// only text, a character that bytes written before left unfinished is ended first, as
        /// U+FFFD, so the text follows those bytes rather than landing inside them.
        /// </summary>
        public TextWriter Writer(CommandStream stream)
        {
            var writer = stream == CommandStream.Out ? Out : Error;
            if ((stream == CommandStream.Out ? m_OutBytes : m_ErrorBytes) != null)
                return writer;

            lock (m_WriteLock)
            {
                var decoder = stream == CommandStream.Out ? m_OutDecoder : m_ErrorDecoder;
                Span<char> pending = stackalloc char[2];
                var count = decoder.GetChars(ReadOnlySpan<byte>.Empty, pending, flush: true);
                if (count > 0)
                    writer.Write(pending.Slice(0, count));
            }
            return writer;
        }

        /// <summary>
        /// Writes <paramref name="data"/> to <paramref name="stream"/>, in order with its text: as
        /// they are when the client's stream carries bytes, decoded as UTF-8 when it carries only
        /// text (HTTP, or a command run by another one), a character split across calls kept whole.
        /// </summary>
        public void Write(CommandStream stream, ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty || m_Closed)
                return;
            if (stream == CommandStream.Error)
                WroteError = true;

            var bytes = stream == CommandStream.Out ? m_OutBytes : m_ErrorBytes;
            if (bytes != null)
            {
                bytes.Write(data);
                return;
            }

            lock (m_WriteLock)
            {
                var decoder = stream == CommandStream.Out ? m_OutDecoder : m_ErrorDecoder;
                var chars = System.Buffers.ArrayPool<char>.Shared.Rent(decoder.GetCharCount(data, flush: false));
                try
                {
                    var count = decoder.GetChars(data, chars, flush: false);
                    if (count > 0)
                        (stream == CommandStream.Out ? Out : Error).Write(chars, 0, count);
                }
                finally
                {
                    System.Buffers.ArrayPool<char>.Shared.Return(chars);
                }
            }
        }

        /// <summary>
        /// Reads a line of In, waiting for it without a thread. Null
        /// once the input ended, as it does once the command is cancelled or ends.
        /// </summary>
        public ValueTask<string> ReadLineAsync() => In.ReadLineAsync();

        /// <summary>Reads the bytes of In as the client sent them; 0 once it ended.</summary>
        public ValueTask<int> ReadAsync(Memory<byte> buffer) => In.ReadAsync(buffer);

        /// <summary>
        /// Keeps its first characters and drops the rest, never splitting a surrogate pair. Only
        /// the three core writes are overridden: <see cref="TextWriter"/> routes every other one
        /// (spans, <see cref="StringBuilder"/>, lines) through them, so none gets past the cap.
        /// Locks the owner's lock, which <see cref="KeptOut"/> and <see cref="KeptError"/> read under.
        /// </summary>
        sealed class CappedWriter : TextWriter
        {
            readonly StringBuilder m_Text = new StringBuilder();
            readonly int m_Max;
            readonly object m_Lock;

            public CappedWriter(int max, object @lock)
            {
                m_Max = max;
                m_Lock = @lock;
            }

            public override Encoding Encoding => Encoding.Unicode;

            public override void Write(char value)
            {
                lock (m_Lock)
                {
                    // A high surrogate needs room for its low one.
                    var room = m_Max - m_Text.Length;
                    if (room > 1 || (room == 1 && !char.IsHighSurrogate(value)))
                        m_Text.Append(value);
                }
            }

            public override void Write(char[] buffer, int index, int count)
            {
                if (buffer == null || count <= 0)
                    return;
                lock (m_Lock)
                    m_Text.Append(buffer, index, Fit(count, i => buffer[index + i]));
            }

            public override void Write(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return;
                lock (m_Lock)
                    m_Text.Append(value, 0, Fit(value.Length, i => value[i]));
            }

            /// <summary>How many of <paramref name="count"/> characters fit, without a pair cut in two.</summary>
            int Fit(int count, Func<int, char> at)
            {
                var length = Math.Min(count, Math.Max(m_Max - m_Text.Length, 0));
                if (length > 0 && length < count && char.IsHighSurrogate(at(length - 1)))
                    length--;
                return length;
            }

            public override string ToString()
            {
                lock (m_Lock)
                    return m_Text.ToString();
            }
        }

        /// <summary>
        /// Passes writes to the client's stream until the streams are closed, then drops them.
        /// For Error, notes that the command said why it failed.
        /// </summary>
        sealed class GatedWriter : TextWriter
        {
            readonly TextWriter m_Inner;
            readonly StdStreams m_Owner;
            readonly bool m_NotesError;

            public GatedWriter(TextWriter inner, StdStreams owner, bool notesError)
            {
                m_Inner = inner ?? TextWriter.Null;
                m_Owner = owner;
                m_NotesError = notesError;
            }

            public override Encoding Encoding => m_Inner.Encoding;

            public override void Write(char value)
            {
                if (m_Owner.m_Closed)
                    return;
                Note();
                m_Inner.Write(value);
            }

            public override void Write(char[] buffer, int index, int count)
            {
                if (m_Owner.m_Closed || count <= 0)
                    return;
                Note();
                m_Inner.Write(buffer, index, count);
            }

            public override void Write(string value)
            {
                if (m_Owner.m_Closed || string.IsNullOrEmpty(value))
                    return;
                Note();
                m_Inner.Write(value);
            }

            /// <summary>Writes a log line that doesn't explain a failure, such as a warning.</summary>
            public void WriteUnnoted(string value)
            {
                if (!m_Owner.m_Closed)
                    m_Inner.Write(value);
            }

            public override void Flush()
            {
                if (!m_Owner.m_Closed)
                    m_Inner.Flush();
            }

            void Note()
            {
                if (m_NotesError)
                    m_Owner.WroteError = true;
            }
        }
    }
}
