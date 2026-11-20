namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// How much a command logs to Error, as the <c>--log-level</c> option picks it. Each
    /// level includes the ones before it.
    /// </summary>
    enum CommandLogLevel
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Debug = 3
    }
}
