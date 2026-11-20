using System;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// What a command run by another one produced (<see cref="CommandExecutionContext.DispatchOnMainThread"/>):
    /// its result, and for a command that takes a <see cref="CommandContext"/>, what it wrote to its
    /// streams, which are kept rather than sent to the client.
    /// </summary>
    sealed class NestedRun
    {
        internal NestedRun(object value, string output = null, string log = null)
        {
            Value = value;
            Output = string.IsNullOrEmpty(output) ? null : output;
            Log = string.IsNullOrEmpty(log) ? null : log;
        }

        /// <summary>
        /// What the command returned, or for a command that returns its <see cref="ExitCode"/>, the
        /// result it wrote: its single result, or its records as a list. Null for none.
        /// </summary>
        public object Value { get; }

        /// <summary>What the command wrote to Out; null for nothing.</summary>
        public string Output { get; }

        /// <summary>What the command wrote to Error, its log among it; null for nothing.</summary>
        public string Log { get; }
    }

    /// <summary>
    /// A command run by another one that returns its <see cref="ExitCode"/> returned one other than
    /// <see cref="ExitCode.Success"/>, or threw. The message is why, as the command logged it to
    /// Error, where a thrown exception's message is logged too.
    /// </summary>
    sealed class NestedCommandFailedException : Exception
    {
        /// <param name="thrown">What it threw; null when it returned <paramref name="code"/>.</param>
        internal NestedCommandFailedException(string command, ProtocolExitCode code, NestedRun run, Exception thrown = null)
            : base(Describe(command, code, run.Log, thrown != null), thrown)
        {
            Code = code;
            Run = run;
            Summary = Describe(command, code, null, thrown != null);
        }

        /// <summary>The message without what the command logged, for when there is no room for it.</summary>
        public string Summary { get; }

        /// <summary>The exit code the command returned, or the one its exception ends it with.</summary>
        public ProtocolExitCode Code { get; }

        /// <summary>What it produced before it ended.</summary>
        public NestedRun Run { get; }

        static string Describe(string command, ProtocolExitCode code, string log, bool threw)
        {
            var ended = threw ? $"Command '{command}' threw ({code})" : $"Command '{command}' returned {code}";
            var why = log?.Trim();
            return string.IsNullOrEmpty(why) ? ended + "." : $"{ended}: {why}";
        }
    }
}
