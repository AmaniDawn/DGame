using System;
using System.Collections.Generic;
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Runtime.Commands
{
    /// <summary>
    /// What the HTTP API's /api/progress and /api/commands serve, as commands, so a client of the
    /// Pipeline protocol reads them without HTTP. Both are immediate: they answer while another
    /// command runs.
    /// </summary>
    static class PipelineCommands
    {
        const string Group = "pipeline";

        [CliGroupCommand(Group, "Get the progress of the running command, as /api/progress serves it: whether a command runs, and what it last reported.",
            MainThreadRequired = false, Tags = new[] { "pipeline" },
            Effect = CommandEffect.ReadOnly)]
        [ImmediateCommand]
        internal static ExitCode Progress(CommandContext cmd)
        {
            var state = CommandExecutionContext.Current.Progress;
            var active = state.IsActive;
            cmd.Result.Write(new
            {
                active,
                progress = active ? BasePipelineServer.BuildProgressPayload(CommandProgress.From(state.Current)) : null
            });
            return ExitCode.Success;
        }

        [CliGroupCommand(Group, "List the registered commands, filtered, sorted and grouped as /api/commands serves them.",
            MainThreadRequired = false, Tags = new[] { "pipeline" },
            Effect = CommandEffect.ReadOnly)]
        [ImmediateCommand]
        internal static ExitCode ListCommands(CommandContext cmd,
            [CliArg("detail", "full (default) with parameters and schemas, compact, tags (an index of tags) or groups (an index of groups).")] string detail = null,
            [CliArg("query", "Keep the commands whose full name, description or a tag contains this, ignoring case.")] string query = null,
            [CliArg("group", "Keep a group and its subgroups.")] string group = null,
            [CliArg("tag", "Keep a tag and the tags beneath it.")] string tag = null,
            [CliArg("include-tests", "Also list the commands the test assemblies declare.")] bool includeTests = false,
            [CliArg("group-by", "flat (default), package, tag or group.")] string groupBy = null,
            [CliArg("sort", "name (default) or package.")] string sort = null,
            [CliArg("order", "asc (default) or desc.")] string order = null,
            [CliArg("offset", "How many to skip, after sorting.")] int? offset = null,
            [CliArg("limit", "How many to return at most; all by default.")] int? limit = null)
        {
            var catalog = BuildCatalog(detail, query, group, tag, includeTests, groupBy, sort, order, offset, limit);

            // JSON as the catalog unless --fields picks fields; otherwise one record per entry, which the
            // result writes in the format picked. Human laid out as the CLI lays out a listing: output that isn't a result.
            var current = CommandExecutionContext.Current;
            if (!(current?.Result is ResultWriter writer) || current.Std == null)
            {
                cmd.Result.Write(catalog);
                return ExitCode.Success;
            }

            var kind = CommandListing.KindOf(detail);
            switch (writer.Format)
            {
                case OutputFormat.Json when !writer.SelectsFields:
                    cmd.Result.Write(catalog);
                    break;
                case OutputFormat.Json:
                case OutputFormat.Tsv:
                    foreach (var entry in CommandListing.Entries(catalog, kind))
                        cmd.Result.WriteRecord(entry);
                    break;
                default:
                    cmd.Std.Out.Write(CommandListing.Human(catalog, kind, current.Std.OutIsTerminal));
                    break;
            }
            return ExitCode.Success;
        }

        /// <summary>
        /// The <c>/api/commands</c> answer for the same options, without its server block.
        /// </summary>
        /// <exception cref="ArgumentException">An option is unknown, or can't be combined with another.</exception>
        internal static Dictionary<string, object> BuildCatalog(string detail = null, string query = null, string group = null,
            string tag = null, bool includeTests = false, string groupBy = null, string sort = null, string order = null,
            int? offset = null, int? limit = null)
        {
            var options = new Dictionary<string, string>
            {
                ["detail"] = detail,
                ["query"] = query,
                ["group"] = group,
                ["tag"] = tag,
                ["include_tests"] = includeTests ? "true" : null,
                ["group_by"] = groupBy,
                ["sort"] = sort,
                ["order"] = order,
                ["offset"] = offset?.ToString(),
                ["limit"] = limit?.ToString()
            };
            return BasePipelineServer.BuildCommandCatalog(name => options[name], null);
        }
    }
}
