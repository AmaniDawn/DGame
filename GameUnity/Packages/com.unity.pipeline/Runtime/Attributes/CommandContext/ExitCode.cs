namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// How a command ended, as it returns it from a method that takes a <see cref="CommandContext"/>.
    /// The numbers are the <c>unity</c> CLI's exit codes, the ones of the Pipeline protocol's exit
    /// frame.
    /// </summary>
    /// <remarks>
    /// Only the codes a command decides. The pipeline decides the rest itself: a server that can't
    /// run commands now, and a modal dialog that blocks the editor. It also reports the request's
    /// own timeout as <see cref="TimedOut"/>, which a command returns for a time limit of its own.
    /// </remarks>
    public enum ExitCode
    {
        /// <summary>The command did what it was asked.</summary>
        Success = 0,

        /// <summary>
        /// The arguments are wrong in a way the parser can't tell, such as two flags that exclude each
        /// other. Log why with <see cref="CommandLog.Error"/>.
        /// </summary>
        InvalidUsage = 2,

        /// <summary>The command failed. Log why with <see cref="CommandLog.Error"/>.</summary>
        Failed = 6,

        /// <summary>What the command was asked to work on doesn't exist, such as an asset path.</summary>
        NotFound = 13,

        /// <summary>
        /// The command ran out of a time limit of its own, such as a batch's timeout. Log why with
        /// <see cref="CommandLog.Error"/>. The request's own timeout is the pipeline's to report.
        /// </summary>
        TimedOut = 14,

        /// <summary>
        /// The user stopped the command, such as by cancelling a dialog it opened. For a cancellation
        /// through <see cref="CommandContext.CancellationToken"/>, let the
        /// <see cref="System.OperationCanceledException"/> propagate instead.
        /// </summary>
        Cancelled = 130
    }
}
