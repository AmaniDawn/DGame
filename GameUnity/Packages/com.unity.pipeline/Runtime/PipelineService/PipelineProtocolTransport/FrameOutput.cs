using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;
using FrameType = Unity.Pipeline.PipelineProtocol.FrameType;

namespace Unity.Pipeline
{
    /// <summary>
    /// What a command writes to its Out and Error, sent to the client as Stdout and Stderr frames
    /// in the order it was written, and its progress, sent as Progress frames. Writing only
    /// buffers, so a command never waits on its client; <see cref="SendAsync"/> sends what was
    /// buffered, from a loop of its own, the one writer of the stream.
    /// </summary>
    /// <remarks>
    /// The buffer is bounded, so a client that stays connected without reading can't grow it
    /// without limit: once it holds <c>maxBufferedBytes</c>, further writes are dropped, and a line
    /// on Stderr marks where, until the sender takes what was buffered. A write is taken whole
    /// while the buffer is under the bound, so a command's result, written at once, always is.
    /// Text is encoded as UTF-8 as it is written; bytes written through <see cref="IByteWriter"/> are
    /// sent as they are, in order with the text. Thread-safe.
    /// </remarks>
    sealed class FrameOutput
    {
        sealed class Chunk
        {
            public FrameType Type;
            public readonly MemoryStream Bytes = new MemoryStream();
        }

        /// <summary>How many bytes it buffers before dropping writes, by default: 16 MB.</summary>
        public const int DefaultMaxBufferedBytes = 16 * 1024 * 1024;

        /// <summary>Written on Stderr where writes start being dropped.</summary>
        internal const string DroppedMarker = "Pipeline: Output dropped here: the client isn't reading it.\n";

        readonly object m_Lock = new object();
        static readonly UTF8Encoding s_Utf8 = new UTF8Encoding(false);
        static readonly byte[] s_DroppedMarker = s_Utf8.GetBytes(DroppedMarker);

        readonly int m_MaxBufferedBytes;

        // Stateful, so a surrogate pair split across two writes still encodes as one character.
        readonly Encoder m_OutEncoder = s_Utf8.GetEncoder();
        readonly Encoder m_ErrorEncoder = s_Utf8.GetEncoder();
        readonly Queue<Chunk> m_Pending = new Queue<Chunk>();
        readonly SemaphoreSlim m_Available = new SemaphoreSlim(0);
        Chunk m_Last;

        /// <summary>The bytes in <see cref="m_Pending"/>.</summary>
        int m_BufferedBytes;

        /// <summary>Whether writes are dropped until the sender takes what was buffered.</summary>
        bool m_Dropping;

        /// <summary>The Progress body not sent yet; null when the client has the latest.</summary>
        byte[] m_Progress;

        bool m_Completed;

        /// <param name="maxBufferedBytes">How many bytes it buffers before dropping writes.</param>
        public FrameOutput(int maxBufferedBytes = DefaultMaxBufferedBytes)
        {
            if (maxBufferedBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxBufferedBytes), maxBufferedBytes, "Must be positive.");
            m_MaxBufferedBytes = maxBufferedBytes;
            Out = new FrameWriter(this, FrameType.Stdout);
            Error = new FrameWriter(this, FrameType.Stderr);
        }

        /// <summary>Takes what the command produces, sent as Stdout frames; also an <see cref="IByteWriter"/>.</summary>
        public TextWriter Out { get; }

        /// <summary>Takes why the command failed, sent as Stderr frames; also an <see cref="IByteWriter"/>.</summary>
        public TextWriter Error { get; }

        /// <summary>
        /// Sends <paramref name="body"/> as a Progress frame, after what was written before it. Only
        /// the latest is kept: one not sent yet is replaced.
        /// </summary>
        public void ReportProgress(byte[] body)
        {
            lock (m_Lock)
            {
                if (m_Completed)
                    return;

                var wasEmpty = IsEmpty;
                m_Progress = body;
                if (!wasEmpty)
                    return;
            }
            m_Available.Release();
        }

        /// <summary>Whether nothing waits to be sent. Read under the lock.</summary>
        bool IsEmpty => m_Pending.Count == 0 && m_Progress == null;

        /// <summary>
        /// Ends the output: <see cref="SendAsync"/> sends what is buffered and returns, and anything
        /// written after is dropped.
        /// </summary>
        public void Complete()
        {
            lock (m_Lock)
            {
                if (m_Completed)
                    return;
                // A high surrogate never followed by its low one is sent as U+FFFD.
                Encode(FrameType.Stdout, ReadOnlySpan<char>.Empty, flush: true);
                Encode(FrameType.Stderr, ReadOnlySpan<char>.Empty, flush: true);
                m_Completed = true;
            }
            m_Available.Release();
        }

        /// <summary>
        /// Sends what is written, as it is written, until <see cref="Complete"/> and everything
        /// before it was sent. Call once.
        /// </summary>
        /// <exception cref="IOException">The client went away.</exception>
        public async Task SendAsync(Stream stream, CancellationToken cancellation)
        {
            var chunks = new List<Chunk>();

            while (true)
            {
                await m_Available.WaitAsync(cancellation);

                bool completed;
                byte[] progress;
                lock (m_Lock)
                {
                    chunks.AddRange(m_Pending);
                    m_Pending.Clear();
                    m_Last = null;
                    m_BufferedBytes = 0;
                    m_Dropping = false;
                    progress = m_Progress;
                    m_Progress = null;
                    completed = m_Completed;
                }

                foreach (var chunk in chunks)
                {
                    var bytes = chunk.Bytes.GetBuffer();
                    var size = (int)chunk.Bytes.Length;
                    for (var offset = 0; offset < size; offset += PipelineProtocol.MaxBodySize)
                    {
                        var count = Math.Min(PipelineProtocol.MaxBodySize, size - offset);
                        await PipelineProtocol.WriteFrameAsync(stream, chunk.Type, bytes, offset, count, cancellation);
                    }
                }
                chunks.Clear();

                // Ends with Exit, so a client clears it then; one buffered past the end is stale.
                if (progress != null && !completed)
                    await PipelineProtocol.WriteFrameAsync(stream, FrameType.Progress, progress, cancellation);

                if (completed)
                    return;
            }
        }

        void Append(FrameType type, ReadOnlySpan<char> text, ReadOnlySpan<byte> bytes)
        {
            lock (m_Lock)
            {
                if (m_Completed || m_Dropping)
                    return;

                var wasEmpty = IsEmpty;
                if (m_BufferedBytes >= m_MaxBufferedBytes)
                {
                    // Marked once, on Stderr, past the bound: it has to reach the client to mean anything.
                    m_Dropping = true;
                    // What the encoders hold belongs to text now dropped, so it never reaches the client.
                    m_OutEncoder.Reset();
                    m_ErrorEncoder.Reset();
                    Buffer(FrameType.Stderr).Write(s_DroppedMarker, 0, s_DroppedMarker.Length);
                    return;
                }

                if (bytes.IsEmpty)
                    Encode(type, text, flush: false);
                else
                {
                    // Bytes follow the text before them whole: a dangling high surrogate is sent as U+FFFD.
                    Encode(type, ReadOnlySpan<char>.Empty, flush: true);
                    Buffer(type).Write(bytes);
                    m_BufferedBytes += bytes.Length;
                }

                if (!wasEmpty || IsEmpty)
                    return;
            }
            m_Available.Release();
        }

        /// <summary>Encodes <paramref name="text"/> into <paramref name="type"/>'s chunk. Called under the lock.</summary>
        void Encode(FrameType type, ReadOnlySpan<char> text, bool flush)
        {
            var encoder = type == FrameType.Stdout ? m_OutEncoder : m_ErrorEncoder;
            // Called even when it yields no bytes: a lone high surrogate has to reach the encoder's state.
            var bytes = System.Buffers.ArrayPool<byte>.Shared.Rent(encoder.GetByteCount(text, flush));
            try
            {
                var count = encoder.GetBytes(text, bytes, flush);
                if (count == 0)
                    return;
                Buffer(type).Write(bytes, 0, count);
                m_BufferedBytes += count;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        /// <summary>The chunk to append <paramref name="type"/>'s bytes to. Called under the lock.</summary>
        MemoryStream Buffer(FrameType type)
        {
            if (m_Last == null || m_Last.Type != type)
            {
                m_Last = new Chunk { Type = type };
                m_Pending.Enqueue(m_Last);
            }
            return m_Last.Bytes;
        }

        sealed class FrameWriter : TextWriter, IByteWriter
        {
            readonly FrameOutput m_Output;
            readonly FrameType m_Type;

            public FrameWriter(FrameOutput output, FrameType type)
            {
                m_Output = output;
                m_Type = type;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value) => m_Output.Append(m_Type, stackalloc char[] { value }, default);

            public override void Write(char[] buffer, int index, int count)
            {
                if (count > 0)
                    m_Output.Append(m_Type, buffer.AsSpan(index, count), default);
            }

            public override void Write(string value)
            {
                if (!string.IsNullOrEmpty(value))
                    m_Output.Append(m_Type, value.AsSpan(), default);
            }

            public void Write(ReadOnlySpan<byte> bytes)
            {
                if (!bytes.IsEmpty)
                    m_Output.Append(m_Type, default, bytes);
            }
        }
    }
}
