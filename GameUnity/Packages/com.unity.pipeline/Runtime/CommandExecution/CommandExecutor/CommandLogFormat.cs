namespace Unity.Pipeline.Commands
{
    /// <summary>How a command's logs are written to Error, as <see cref="CommandRequest.LogFormatOption"/> picks it.</summary>
    enum CommandLogFormat
    {
        /// <summary>One line per message, labelled with its level for every level but info, such as <c>error: </c>.</summary>
        Human = 0,

        /// <summary>One JSON object per line, with the message's <c>level</c> and <c>message</c>.</summary>
        Json = 1
    }
}
