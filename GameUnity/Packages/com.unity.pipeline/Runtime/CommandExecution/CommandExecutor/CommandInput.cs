using System;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// A command's In: the bytes the client sends it, as a transport feeds them in (the Pipeline
    /// protocol's Stdin frames), or <see cref="Empty"/> for a transport that sends none. A read waits
    /// for the client's next bytes, and ends once the client ends its input or goes away.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It keeps the bytes as the client sent them: <see cref="ReadAsync"/> returns them as they are,
    /// and <see cref="ReadLineAsync"/> decodes a line of them as UTF-8. Both take from one position,
    /// so a command may mix them. Lines are found in the bytes, since <c>\n</c> and <c>\r</c> never
    /// occur inside a multi-byte UTF-8 sequence, so a character split across appends is decoded whole.
    /// </para>
    /// <para>
    /// Thread-safe. Reads wait without a thread. The transport ends the input once the command is
    /// cancelled or ends, so a read never outlasts either.
    /// </para>
    /// </remarks>
    sealed class CommandInput
    {
        /// <summary>
        /// How much input it holds unread, in bytes, by default: 16 MB, as much output as the Pipeline
        /// protocol transport holds for the client.
        /// </summary>
        public const int DefaultMaxBufferedBytes = 16 * 1024 * 1024;

        static readonly UTF8Encoding s_Utf8 = new UTF8Encoding(false);

        /// <summary>No input: ended, with nothing in it. Shared, since nothing changes an ended input.</summary>
        public static readonly CommandInput Empty = FromText(null);

        readonly object m_Lock = new object();
        readonly int m_MaxBufferedBytes;

        /// <summary>The unread bytes: <see cref="m_Count"/> of them from <see cref="m_Start"/>.</summary>
        byte[] m_Buffer = Array.Empty<byte>();
        int m_Start;
        int m_Count;
        bool m_Ended;

        /// <summary>
        /// How many bytes at the start of the buffer <see cref="TryTakeLine"/> already found no line
        /// break in, so a line arriving over many frames is scanned once, not once per frame.
        /// </summary>
        int m_LineScanned;

        /// <summary>Completed once more input arrives or it ends; null while nobody waits.</summary>
        TaskCompletionSource<bool> m_Arrived;

        public CommandInput(int maxBufferedBytes = DefaultMaxBufferedBytes)
        {
            m_MaxBufferedBytes = maxBufferedBytes;
        }

        /// <summary>An input that already ended, holding <paramref name="text"/> as UTF-8: a test's input.</summary>
        public static CommandInput FromText(string text)
        {
            var input = new CommandInput(int.MaxValue);
            if (!string.IsNullOrEmpty(text))
                input.Append(s_Utf8.GetBytes(text));
            input.End();
            return input;
        }

        /// <summary>Adds bytes the client sent, such as a Stdin frame's body, which may split a character.</summary>
        /// <returns>
        /// False when the input left unread grew past the bound: the client sends more than the
        /// command reads, and the input ended.
        /// </returns>
        public bool Append(byte[] bytes)
        {
            lock (m_Lock)
            {
                if (m_Ended)
                    return true;
                if (bytes.Length > m_MaxBufferedBytes - m_Count)
                {
                    m_Ended = true;
                    Signal();
                    return false;
                }
                if (bytes.Length == 0)
                    return true;

                if (m_Buffer.Length - m_Start - m_Count < bytes.Length)
                    MakeRoom(m_Count + bytes.Length);
                Buffer.BlockCopy(bytes, 0, m_Buffer, m_Start + m_Count, bytes.Length);
                m_Count += bytes.Length;
                Signal();
                return true;
            }
        }

        /// <summary>
        /// Makes the buffer hold <paramref name="needed"/> bytes from its start. Moves the unread bytes
        /// down only when as many were read before them, so each byte is moved at most once on
        /// average; grows otherwise, doubling up to the bound. Must hold the lock.
        /// </summary>
        void MakeRoom(int needed)
        {
            var target = m_Buffer;
            if (needed > m_Buffer.Length || m_Start < m_Count)
            {
                var capacity = Math.Max(needed, (int)Math.Min(m_Buffer.Length * 2L, m_MaxBufferedBytes));
                if (capacity > m_Buffer.Length)
                    target = new byte[capacity];
            }
            Buffer.BlockCopy(m_Buffer, m_Start, target, 0, m_Count);
            m_Buffer = target;
            m_Start = 0;
        }

        /// <summary>Ends the input: reads take what is left, then end.</summary>
        public void End()
        {
            lock (m_Lock)
            {
                if (m_Ended)
                    return;
                m_Ended = true;
                Signal();
            }
        }

        /// <summary>
        /// Reads the bytes the client sent, as they are, into <paramref name="buffer"/>: as many as
        /// have arrived, up to its length, waiting while none has.
        /// </summary>
        /// <returns>How many bytes were read; 0 once the input ended and everything in it was read.</returns>
        public async ValueTask<int> ReadAsync(Memory<byte> buffer)
        {
            if (buffer.IsEmpty)
                return 0;
            while (true)
            {
                Task arrived;
                lock (m_Lock)
                {
                    if (m_Count > 0)
                    {
                        var read = Math.Min(buffer.Length, m_Count);
                        m_Buffer.AsSpan(m_Start, read).CopyTo(buffer.Span);
                        Advance(read);
                        return read;
                    }
                    if (m_Ended)
                        return 0;
                    arrived = WaitTask();
                }

                // The caller's own await resumes on its context; this one needs none.
                await arrived.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads the next line, decoded as UTF-8, without its line break: up to <c>\n</c>,
        /// <c>\r</c> or <c>\r\n</c>, or what is left once the input ended. Waits while the client
        /// hasn't sent it.
        /// </summary>
        /// <returns>The line, or null once the input ended and everything in it was read.</returns>
        public async ValueTask<string> ReadLineAsync()
        {
            while (true)
            {
                Task arrived;
                lock (m_Lock)
                {
                    if (TryTakeLine(out var line))
                        return line;
                    arrived = WaitTask();
                }

                // The caller's own await resumes on its context; this one needs none.
                await arrived.ConfigureAwait(false);
            }
        }

        /// <summary>Takes the next whole line out of the buffer. Must hold the lock.</summary>
        /// <param name="line">The line; null once the input ended and everything in it was read.</param>
        /// <returns>False while no whole line arrived yet.</returns>
        bool TryTakeLine(out string line)
        {
            line = null;
            for (var i = m_LineScanned; i < m_Count; i++)
            {
                var b = m_Buffer[m_Start + i];
                if (b != (byte)'\n' && b != (byte)'\r')
                    continue;

                // A \r last: a \n may follow in the next frame, and \r\n is one break. The next
                // scan starts at the \r.
                if (b == (byte)'\r' && i == m_Count - 1 && !m_Ended)
                {
                    m_LineScanned = i;
                    return false;
                }

                line = s_Utf8.GetString(m_Buffer, m_Start, i);
                var breakLength = b == (byte)'\r' && i + 1 < m_Count && m_Buffer[m_Start + i + 1] == (byte)'\n' ? 2 : 1;
                Advance(i + breakLength);
                return true;
            }

            if (!m_Ended)
            {
                m_LineScanned = m_Count;
                return false;
            }
            if (m_Count > 0)
            {
                line = s_Utf8.GetString(m_Buffer, m_Start, m_Count);
                Advance(m_Count);
            }
            return true;
        }

        /// <summary>Drops the first <paramref name="count"/> bytes. Must hold the lock.</summary>
        void Advance(int count)
        {
            m_Start += count;
            m_Count -= count;
            if (m_Count == 0)
                m_Start = 0;
            m_LineScanned = Math.Max(0, m_LineScanned - count);
        }

        /// <summary>The task the next arrival completes. Must hold the lock.</summary>
        Task WaitTask()
        {
            m_Arrived ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return m_Arrived.Task;
        }

        /// <summary>Wakes the reads waiting for input. Must hold the lock.</summary>
        void Signal()
        {
            m_Arrived?.TrySetResult(true);
            m_Arrived = null;
        }
    }
}
