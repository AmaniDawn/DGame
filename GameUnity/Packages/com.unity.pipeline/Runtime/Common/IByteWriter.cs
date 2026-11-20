using System;

namespace Unity.Pipeline
{
    /// <summary>
    /// An output stream that takes bytes as they are, in order with the text written to it, such as
    /// the Pipeline protocol transport's Stdout and Stderr. A command's bytes reach a client whose
    /// stream is one unchanged; any other stream gets them decoded as UTF-8.
    /// </summary>
    interface IByteWriter
    {
        void Write(ReadOnlySpan<byte> bytes);
    }
}
