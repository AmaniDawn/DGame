using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Pipeline
{
    /// <summary>
    /// Read-only stream wrapper that enforces a maximum number of bytes read from an inner stream.
    /// Once cumulative reads exceed <c>maxBytes</c>, the next read throws
    /// <see cref="RequestTooLargeException"/>. Used to cap HTTP request bodies even when the
    /// Content-Length header is absent or untruthful (e.g. chunked transfer-encoding), so an
    /// oversized body cannot be buffered into memory.
    /// </summary>
    internal sealed class MaxLengthStream : Stream
    {
        private readonly Stream m_Inner;
        private readonly long m_MaxBytes;
        private readonly bool m_LeaveOpen;
        private long m_TotalRead;

        /// <param name="leaveOpen">
        /// When true, disposing this wrapper does not dispose <paramref name="inner"/>. The server
        /// uses this so it can keep reading (draining) the raw request stream after the wrapper is
        /// disposed on an over-limit read.
        /// </param>
        public MaxLengthStream(Stream inner, long maxBytes, bool leaveOpen = false)
        {
            m_Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            m_MaxBytes = maxBytes;
            m_LeaveOpen = leaveOpen;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = m_Inner.Read(buffer, offset, count);
            Account(read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await m_Inner.ReadAsync(buffer, offset, count, cancellationToken);
            Account(read);
            return read;
        }

        // Throw as soon as the running total exceeds the cap. Exactly maxBytes is allowed.
        private void Account(int read)
        {
            if (read <= 0)
                return;

            m_TotalRead += read;
            if (m_TotalRead > m_MaxBytes)
                throw new RequestTooLargeException(m_MaxBytes);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => m_TotalRead;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // Own the inner stream's lifetime (matching the previous `using (StreamReader(InputStream))`)
            // unless the caller asked to leave it open so it can keep reading from it afterwards.
            if (disposing && !m_LeaveOpen)
                m_Inner.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Thrown by <see cref="MaxLengthStream"/> when a read would exceed the configured byte cap.
    /// </summary>
    internal sealed class RequestTooLargeException : Exception
    {
        public long MaxBytes { get; }

        public RequestTooLargeException(long maxBytes)
            : base($"Request body exceeds the maximum allowed size of {maxBytes} bytes.")
        {
            MaxBytes = maxBytes;
        }
    }
}
