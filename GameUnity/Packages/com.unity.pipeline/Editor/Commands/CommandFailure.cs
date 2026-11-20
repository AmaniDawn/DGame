using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Editor.Commands
{
    /// <summary>Shared failure helper for commands that return their exit code.</summary>
    static class CommandFailure
    {
        /// <summary>Logs <paramref name="error"/> to the command's Error stream and returns <paramref name="code"/>.</summary>
        internal static ExitCode Fail(CommandContext cmd, ExitCode code, string error)
        {
            cmd.Log.Error(error);
            return code;
        }
    }
}
