using System;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Writes a command's result to Out in the format the user picked: a single result with
    /// <see cref="Write{T}"/>, or one record per item with <see cref="WriteRecord{T}"/>, as each is
    /// ready, so output streams while the command runs.
    /// </summary>
    /// <remarks>
    /// A command writes its result one way only, and its single result once; the pipeline throws
    /// <see cref="InvalidOperationException"/> otherwise. Don't also write to Out directly.
    /// </remarks>
    public readonly struct CommandResult
    {
        readonly ICommandHost m_Host;

        internal CommandResult(ICommandHost host)
        {
            m_Host = host;
        }

        /// <summary>Writes the command's single result, such as a status or a summary.</summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The result object.</param>
        public void Write<T>(T obj) => Host.WriteResult(obj);

        /// <summary>Writes one record of the command's result.</summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The record object.</param>
        public void WriteRecord<T>(T obj) => Host.WriteRecord(obj);

        ICommandHost Host => m_Host ?? throw CommandContext.NotFromPipeline();
    }
}
