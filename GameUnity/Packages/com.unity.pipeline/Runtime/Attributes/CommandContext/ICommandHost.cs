using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// The execution behind a <see cref="CommandContext"/>, as the context reaches it. The pipeline
    /// implements it in Unity.Pipeline; the context types live here, in the unconstrained attributes
    /// assembly, so a command that takes one compiles without referencing the pipeline.
    /// </summary>
    interface ICommandHost
    {
        /// <summary>The arguments the command was called with, without the pipeline's own options.</summary>
        IReadOnlyList<string> Arguments { get; }

        /// <summary>Cancelled once the user stops the command.</summary>
        CancellationToken CancellationToken { get; }

        /// <summary>Writes the command's single result.</summary>
        void WriteResult(object result);

        /// <summary>Writes one record of the command's result.</summary>
        void WriteRecord(object record);

        /// <summary>Logs to Error, if <paramref name="level"/> is within the level the user picked.</summary>
        void Log(CommandLogLevel level, string message);

        /// <summary>The output stream as a <see cref="TextWriter"/>.</summary>
        TextWriter Writer(CommandStream stream);

        /// <summary>Writes bytes to the output stream, as they are.</summary>
        void Write(CommandStream stream, ReadOnlySpan<byte> data);

        /// <summary>Reads the next line of input, or null once it ended.</summary>
        ValueTask<string> ReadLineAsync();

        /// <summary>Reads the bytes of the input as the client sent them; 0 once it ended.</summary>
        ValueTask<int> ReadAsync(Memory<byte> buffer);
    }
}
