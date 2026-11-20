using System;
using System.Collections.Generic;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Where a command's <see cref="CommandResult"/> goes: the request's Out for a command the queue
    /// runs (<see cref="ResultWriter"/>), or a value for a command another one runs (<see cref="ResultCollector"/>).
    /// </summary>
    interface IResultTarget
    {
        /// <summary>Takes the command's single result.</summary>
        /// <exception cref="InvalidOperationException">The command already wrote its result, or a record.</exception>
        void Write(object result);

        /// <summary>Takes one record of the command's result.</summary>
        /// <exception cref="InvalidOperationException">The command already wrote a single result.</exception>
        void WriteRecord(object record);
    }

    /// <summary>
    /// A command's result kept as a value, for a command run by another one, such as through
    /// <c>job batch</c>, which reads it rather than writes it. Holds the same rules as
    /// <see cref="ResultWriter"/>: a single result once, or records, never both. Thread-safe.
    /// </summary>
    sealed class ResultCollector : IResultTarget
    {
        readonly object m_Lock = new object();
        bool m_WroteResult;
        object m_Result;
        List<object> m_Records;

        /// <summary>The single result, or the records as a list; null when it wrote neither.</summary>
        public object Value
        {
            get
            {
                lock (m_Lock)
                    return m_Records ?? m_Result;
            }
        }

        public void Write(object result)
        {
            lock (m_Lock)
            {
                if (m_WroteResult)
                    throw new InvalidOperationException("A command writes its single result once.");
                if (m_Records != null)
                    throw new InvalidOperationException("A command writes its result with Write or with WriteRecord, not both.");
                m_WroteResult = true;
                m_Result = result;
            }
        }

        public void WriteRecord(object record)
        {
            lock (m_Lock)
            {
                if (m_WroteResult)
                    throw new InvalidOperationException("A command writes its result with Write or with WriteRecord, not both.");
                (m_Records ??= new List<object>()).Add(record);
            }
        }
    }
}
