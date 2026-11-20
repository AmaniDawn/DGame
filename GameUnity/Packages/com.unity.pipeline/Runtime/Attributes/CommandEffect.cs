namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// What running a command does to the Editor and the project. The command catalog serves it as
    /// the <c>readOnly</c>, <c>destructive</c> and <c>idempotent</c> booleans of the command's entry,
    /// so a client such as an MCP bridge can tell a read from a change it cannot take back and ask
    /// before the second kind.
    /// </summary>
    /// <remarks>
    /// Pick the strongest effect any call can have. A command that takes <c>confirm</c>, or runs code
    /// the caller supplies, is <see cref="Destructive"/> even when most calls change nothing.
    /// </remarks>
    public enum CommandEffect
    {
        /// <summary>
        /// Not declared. The catalog serves no hints, and a client decides on its own, usually from
        /// the command's name.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Only reads: changes nothing. Read-only, not destructive, idempotent. The idempotent hint
        /// adds nothing here: MCP clients only weigh it when the command is not read-only.
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Changes something, but repeating the call with the same arguments changes nothing more,
        /// and nothing is lost that was there before (a <c>set</c>). Not read-only, not destructive,
        /// idempotent.
        /// </summary>
        Idempotent,

        /// <summary>
        /// Changes something without losing what was there, but a repeat does not leave it as it is:
        /// it adds or starts again (a <c>create</c>), or acts on whatever its arguments now select
        /// (a rename or reparent by a path the first call changed). Not read-only, not destructive,
        /// not idempotent.
        /// </summary>
        Additive,

        /// <summary>
        /// Can delete, overwrite or otherwise lose work that Undo may not bring back, or runs code the
        /// caller supplies. Not read-only, destructive, not idempotent.
        /// </summary>
        Destructive,
    }
}
