using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// One command a client asked to run, as a process is run: the command as the client named it
    /// and its arguments as argv tokens, and its three text streams. The command may be just the
    /// first of the command's names, the rest leading the arguments. Each time it runs is an
    /// execution of its own, with its own id, which is what cancels it.
    /// </summary>
    /// <remarks>
    /// Out takes what the command's run produced, Error why it failed. The writers are safe on any
    /// thread. The command's result is written through <see cref="ResultWriter"/>, in the format
    /// the executor's own options in the arguments pick.
    /// </remarks>
    sealed class CommandRequest
    {
        /// <summary>
        /// The executor's option that keeps the metadata of a response nested in what the command
        /// returns: when it ran and how long it took, and its null message and errors.
        /// </summary>
        /// <remarks>
        /// Every command takes the executor's options, anywhere before <c>--</c>, and none binds
        /// them to a parameter of its own.
        /// </remarks>
        public const string VerboseOption = "--verbose";

        /// <summary>The executor's option that drops nulls from what the command returns.</summary>
        public const string OmitNullsOption = "--omit-nulls";

        /// <summary>
        /// The executor's option that picks the result's format: <c>human</c> for people and agents,
        /// <c>tsv</c> for tab-separated lines, and <c>json</c> for JSON. It also takes the <c>unity</c>
        /// CLI's <c>github</c> and <c>ndjson</c>, which the CLI passes through, as <c>human</c> and
        /// <c>json</c>.
        /// </summary>
        public const string FormatOption = "--format";

        /// <summary>The executor's option that writes the result as JSON, as <c>--format json</c> does.</summary>
        public const string JsonOption = "--json";

        /// <summary>The values <see cref="FormatOption"/> takes, as a user reads them; the CLI's aliases left out.</summary>
        const string FormatChoices = "human, json, tsv";

        /// <summary>
        /// The executor's option that writes only the fields it names, comma-separated, in that order.
        /// </summary>
        public const string FieldsOption = "--fields";

        /// <summary>
        /// The executor's option that picks how much a command logs to Error: <c>error</c>, <c>warning</c>
        /// (the default), <c>info</c> or <c>debug</c>. It never changes what goes to Out.
        /// </summary>
        public const string LogLevelOption = "--log-level";

        /// <summary>The values <see cref="LogLevelOption"/> takes.</summary>
        const string LogLevelChoices = "error, warning, info, debug";

        /// <summary>
        /// The executor's option that picks how a command's logs are written to Error: <c>human</c> (the
        /// default) or <c>json</c>, one object per line; <c>ndjson</c> is <c>json</c>. Independent of <see cref="FormatOption"/>, which
        /// picks only how the result is written to Out.
        /// </summary>
        public const string LogFormatOption = "--log-format";

        /// <summary>The values <see cref="LogFormatOption"/> takes; <c>ndjson</c>, an alias of <c>json</c>, is left out on purpose.</summary>
        const string LogFormatChoices = "human, json";

        /// <summary>The executor's options, which no command may name a parameter after.</summary>
        internal static readonly IReadOnlyList<string> Options = new[] { VerboseOption, OmitNullsOption, FormatOption, JsonOption, FieldsOption, LogLevelOption, LogFormatOption };

        /// <summary>
        /// Reads the executor's own options in <paramref name="arguments"/>, before any <c>--</c>,
        /// and returns the rest. A token starting with <c>--</c> is never an option's value, so one
        /// taken out was never a command's argument.
        /// </summary>
        public static List<string> ReadOptions(IReadOnlyList<string> arguments, out bool verbose, out bool omitNulls)
        {
            return ReadOptions(arguments, out verbose, out omitNulls, out _, out _);
        }

        /// <summary><see cref="ReadOptions(IReadOnlyList{string}, out bool, out bool)"/>, with the format options.</summary>
        /// <param name="format">The format the options pick; <see cref="OutputFormat.Human"/> for none.</param>
        /// <param name="fields">The fields <see cref="FieldsOption"/> names; null for all of them.</param>
        /// <exception cref="ArgumentException">
        /// <see cref="FormatOption"/> without a value it takes, two options that pick different
        /// formats, or <see cref="FieldsOption"/> without a list.
        /// </exception>
        public static List<string> ReadOptions(IReadOnlyList<string> arguments, out bool verbose, out bool omitNulls,
            out OutputFormat format, out IReadOnlyList<string> fields)
        {
            return ReadOptions(arguments, out verbose, out omitNulls, out format, out fields, out _);
        }

        /// <summary><see cref="ReadOptions(IReadOnlyList{string}, out bool, out bool, out OutputFormat, out IReadOnlyList{string})"/>, with the log level.</summary>
        /// <param name="logLevel">The level <see cref="LogLevelOption"/> picks; <see cref="CommandLogLevel.Warning"/> for none.</param>
        /// <exception cref="ArgumentException">Also <see cref="LogLevelOption"/> without a value it takes.</exception>
        public static List<string> ReadOptions(IReadOnlyList<string> arguments, out bool verbose, out bool omitNulls,
            out OutputFormat format, out IReadOnlyList<string> fields, out CommandLogLevel logLevel)
        {
            return ReadOptions(arguments, out verbose, out omitNulls, out format, out fields, out logLevel, out _);
        }

        /// <summary><see cref="ReadOptions(IReadOnlyList{string}, out bool, out bool, out OutputFormat, out IReadOnlyList{string}, out CommandLogLevel)"/>, with the log format.</summary>
        /// <param name="logFormat">The format <see cref="LogFormatOption"/> picks; <see cref="CommandLogFormat.Human"/> for none.</param>
        /// <exception cref="ArgumentException">Also <see cref="LogFormatOption"/> without a value it takes.</exception>
        public static List<string> ReadOptions(IReadOnlyList<string> arguments, out bool verbose, out bool omitNulls,
            out OutputFormat format, out IReadOnlyList<string> fields, out CommandLogLevel logLevel, out CommandLogFormat logFormat)
        {
            logLevel = CommandLogLevel.Warning;
            logFormat = CommandLogFormat.Human;
            var rest = new List<string>(arguments.Count);
            verbose = false;
            omitNulls = false;
            OutputFormat? picked = null;
            fields = null;
            var endOfOptions = false;
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                if (endOfOptions)
                    rest.Add(argument);
                else if (argument == VerboseOption)
                    verbose = true;
                else if (argument == OmitNullsOption)
                    omitNulls = true;
                else if (argument == JsonOption)
                    picked = Pick(picked, OutputFormat.Json);
                else if (TryReadValue(arguments, ref i, FormatOption, out var value))
                {
                    if (!TryParseFormat(value, out var parsed))
                        throw new ArgumentException($"{FormatOption} takes one of: {FormatChoices}, such as {FormatOption} json.");
                    picked = Pick(picked, parsed);
                }
                else if (TryReadValue(arguments, ref i, FieldsOption, out var list))
                {
                    var names = list?.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0).ToList();
                    if (names == null || names.Count == 0)
                        throw new ArgumentException($"{FieldsOption} takes the fields to write, comma-separated, such as {FieldsOption} name,path.");
                    fields = names;
                }
                else if (TryReadValue(arguments, ref i, LogLevelOption, out var level))
                {
                    if (!TryParseLogLevel(level, out logLevel))
                        throw new ArgumentException($"{LogLevelOption} takes one of: {LogLevelChoices}, such as {LogLevelOption} info.");
                }
                else if (TryReadValue(arguments, ref i, LogFormatOption, out var logFormatValue))
                {
                    if (!TryParseLogFormat(logFormatValue, out logFormat))
                        throw new ArgumentException($"{LogFormatOption} takes one of: {LogFormatChoices}, such as {LogFormatOption} json.");
                }
                else
                {
                    endOfOptions = argument == "--";
                    rest.Add(argument);
                }
            }

            format = picked ?? OutputFormat.Human;
            return rest;
        }

        /// <summary>
        /// Whether <paramref name="arguments"/>[<paramref name="i"/>] is <paramref name="option"/>, as
        /// <c>--option value</c> or <c>--option=value</c>; past the value when it is. The value is null
        /// when the next token is missing or is another option.
        /// </summary>
        static bool TryReadValue(IReadOnlyList<string> arguments, ref int i, string option, out string value)
        {
            var argument = arguments[i];
            value = null;
            if (argument.StartsWith(option + "=", StringComparison.Ordinal))
                value = argument.Substring(option.Length + 1);
            else if (argument != option)
                return false;
            else if (i + 1 < arguments.Count && !arguments[i + 1].StartsWith("--", StringComparison.Ordinal))
                value = arguments[++i];
            return true;
        }

        /// <summary>The level a <see cref="LogLevelOption"/> value names. Case-sensitive, as formats are.</summary>
        static bool TryParseLogLevel(string value, out CommandLogLevel level)
        {
            switch (value)
            {
                case "error":
                    level = CommandLogLevel.Error;
                    return true;
                case "warning":
                    level = CommandLogLevel.Warning;
                    return true;
                case "info":
                    level = CommandLogLevel.Info;
                    return true;
                case "debug":
                    level = CommandLogLevel.Debug;
                    return true;
                default:
                    level = CommandLogLevel.Warning;
                    return false;
            }
        }

        /// <summary>The format a <see cref="LogFormatOption"/> value names. Case-sensitive, as formats are.</summary>
        static bool TryParseLogFormat(string value, out CommandLogFormat format)
        {
            switch (value)
            {
                case "human":
                    format = CommandLogFormat.Human;
                    return true;
                case "json":
                case "ndjson":
                    format = CommandLogFormat.Json;
                    return true;
                default:
                    format = CommandLogFormat.Human;
                    return false;
            }
        }

        /// <summary>
        /// The format a <see cref="FormatOption"/> value names. <c>ndjson</c> is <c>json</c>, whose
        /// records are one JSON value per line already, and <c>github</c> is <c>human</c>: the CLI turns
        /// errors into GitHub annotations itself. Case-sensitive, as the CLI is.
        /// </summary>
        static bool TryParseFormat(string value, out OutputFormat format)
        {
            switch (value)
            {
                case "human":
                case "github":
                    format = OutputFormat.Human;
                    return true;
                case "tsv":
                    format = OutputFormat.Tsv;
                    return true;
                case "json":
                case "ndjson":
                    format = OutputFormat.Json;
                    return true;
                default:
                    format = default;
                    return false;
            }
        }

        /// <exception cref="ArgumentException"><paramref name="current"/> picked another format already.</exception>
        static OutputFormat Pick(OutputFormat? current, OutputFormat picked)
        {
            if (current != null && current != picked)
                throw new ArgumentException($"The options pick two formats; pass one {FormatOption}.");
            return picked;
        }

        /// <param name="command">The command as the client named it, or just its group.</param>
        /// <param name="arguments">The argv tokens after <paramref name="command"/>; null for none.</param>
        /// <param name="input">What the client sends the command; null for nothing.</param>
        /// <param name="output">Takes what the command's run produces; null to discard it.</param>
        /// <param name="error">Takes why the command failed; null to discard it.</param>
        public CommandRequest(string command, IReadOnlyList<string> arguments, CommandInput input, TextWriter output,
            TextWriter error)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            Arguments = arguments ?? Array.Empty<string>();
            In = input ?? CommandInput.Empty;
            OutBytes = output as IByteWriter;
            ErrorBytes = error as IByteWriter;
            Out = TextWriter.Synchronized(output ?? TextWriter.Null);
            Error = TextWriter.Synchronized(error ?? TextWriter.Null);
        }

        /// <summary>The command as the client named it, or just its group when the arguments lead with the rest.</summary>
        public string Command { get; }

        /// <summary>
        /// The argv tokens after <see cref="Command"/>, the executor's own options among them. For a
        /// request with <see cref="ParametersJson"/>, only the executor's own options.
        /// </summary>
        public IReadOnlyList<string> Arguments { get; }

        /// <summary>What the client sends the command.</summary>
        public CommandInput In { get; }

        /// <summary>Takes what the command's run produces.</summary>
        public TextWriter Out { get; }

        /// <summary>Takes why the command failed.</summary>
        public TextWriter Error { get; }

        /// <summary>
        /// Takes bytes for <see cref="Out"/> as they are, in order with its text, when the client's
        /// stream carries bytes (the Pipeline protocol's); null when it carries only text.
        /// </summary>
        internal IByteWriter OutBytes { get; }

        /// <summary>The same as <see cref="OutBytes"/>, for <see cref="Error"/>.</summary>
        internal IByteWriter ErrorBytes { get; }

        /// <summary>How long the command may run, the wait for the commands before it included; null for its own default.</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>Who sent the request, for the reports once it ends; its default when the client didn't say.</summary>
        public Requester Requester { get; set; }

        /// <summary>
        /// The arguments as a JSON object by parameter name, in place of <see cref="Arguments"/>,
        /// for the HTTP transport's structured requests. Null for an argv request.
        /// </summary>
        internal string ParametersJson { get; set; }

        /// <summary>Whether the result is written in the format the options pick; JSON otherwise.</summary>
        internal bool FormatOutput { get; set; }

        /// <summary>Whether the client's Out is a terminal.</summary>
        internal bool OutIsTerminal { get; set; }

        /// <summary>Whether the client's Error is a terminal.</summary>
        internal bool ErrorIsTerminal { get; set; }

        /// <summary>
        /// Whether log lines written to Error are formatted for the client, as <see cref="LogFormatOption"/>
        /// picks: labeled, colored or JSON. False, the default, where Error is read back as data, such as
        /// an HTTP response's <c>errorDetails</c>: each line is then the message alone.
        /// </summary>
        internal bool FormatsLogs { get; set; }

        /// <summary>The client's terminal width in columns; null when unknown.</summary>
        internal int? TerminalWidth { get; set; }

        /// <summary>
        /// How many of <see cref="Arguments"/>, once the executor's options are taken out, lead with
        /// the rest of the command's name rather than being passed to it: 1 for <c>test run --x 1</c>
        /// sent as <see cref="Command"/> <c>test</c>, 0 when <see cref="Command"/> names it whole.
        /// Set by the executor when it binds the request.
        /// </summary>
        internal int NamingArguments { get; set; }
    }
}
