using System;
using System.Collections.Generic;
using Unity.Pipeline.Commands;

namespace Unity.Pipeline.Editor.Commands.ProjectSettings
{
    /// <summary>
    /// Shared plumbing the per-group project-settings commands (CLI-202) wrap, so each group's
    /// <c>get</c>/<c>set</c> handler stays thin and the policy is applied uniformly:
    /// <list type="bullet">
    /// <item><description><see cref="Get"/> — write a group's values as a <see cref="ProjectSettingsResponse"/>.</description></item>
    /// <item><description><see cref="Apply"/> — gate a write with the <c>confirm</c>/<c>dry-run</c> convention and
    /// apply it (project-settings writes are not part of Unity's Undo), then re-read the group so the result
    /// reflects the post-change state, and surface domain-reload / reimport signals.</description></item>
    /// </list>
    ///
    /// Group handlers supply two callbacks: <c>readValues</c> (snapshot the group's current values)
    /// and, for writes, <c>apply</c> (perform the mutation) plus a <c>plan</c> describing the intended
    /// effect. All run on the main thread (these commands are <c>MainThreadRequired = true</c>).
    /// </summary>
    static class ProjectSettingsCommand
    {
        /// <summary>
        /// Read-only path: write the group's current values; fails when they can't be read.
        /// </summary>
        public static ExitCode Get(CommandContext cmd, string group, Func<Dictionary<string, object>> readValues)
        {
            if (readValues == null)
                throw new ArgumentNullException(nameof(readValues));

            if (!TryReadValues(readValues, out var values, out var readError))
            {
                cmd.Log.Error($"Failed to read {group} settings: {readError}");
                return ExitCode.Failed;
            }

            cmd.Result.Write(new ProjectSettingsResponse { Group = group, Values = values });
            return ExitCode.Success;
        }

        /// <summary>
        /// A <c>set</c> that asked for nothing different from the current values: says so and writes
        /// the group's values, as <see cref="Get"/> does.
        /// </summary>
        public static ExitCode NoChanges(CommandContext cmd, string group, Func<Dictionary<string, object>> readValues)
        {
            cmd.Log.Info("No changes specified; nothing to apply.");
            return Get(cmd, group, readValues);
        }

        /// <summary>Logs why a <c>set</c> can't go ahead and returns <paramref name="code"/>; writes no result.</summary>
        public static ExitCode Fail(CommandContext cmd, ExitCode code, string message)
            => CommandFailure.Fail(cmd, code, message);

        /// <summary>
        /// Write path: gate the change with the <c>confirm</c>/<c>dry-run</c> convention and apply it
        /// (project-settings writes are not undoable via Ctrl+Z), then re-read the
        /// group. <paramref name="requiresDomainReload"/> / <paramref name="requiresReimport"/> declare
        /// side effects of this specific change (Unity defers them, so the result returns first);
        /// they are reported for an applied change and for a dry-run preview alike. A refused change
        /// exits <see cref="ExitCode.InvalidUsage"/> and a failed one <see cref="ExitCode.Failed"/>,
        /// both with the reason logged and no result written.
        /// </summary>
        public static ExitCode Apply(
            CommandContext cmd,
            string group,
            bool confirm,
            bool dryRun,
            object auditParams,
            Func<string> plan,
            Action apply,
            Func<Dictionary<string, object>> readValues,
            bool requiresDomainReload = false,
            bool requiresReimport = false)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (apply == null)
                throw new ArgumentNullException(nameof(apply));
            if (readValues == null)
                throw new ArgumentNullException(nameof(readValues));

            string planText;
            try
            {
                planText = plan();
            }
            catch (Exception ex)
            {
                cmd.Log.Error($"Failed to compute the operation preview: {ex.Message}");
                return ExitCode.Failed;
            }

            // Inline confirm / dry-run gate (the codebase-wide mutating convention).
            var response = new ProjectSettingsResponse
            {
                Group = group,
                RequiresDomainReload = requiresDomainReload,
                RequiresReimport = requiresReimport
            };
            if (dryRun)
            {
                response.DryRun = true;
                cmd.Log.Info($"Dry run — no changes made. Intended effect: {planText}");
            }
            else if (!confirm)
            {
                cmd.Log.Error("Refused: this operation mutates the project. Re-run with confirm=true to apply, " +
                    "or dry-run=true to preview the change.");
                return ExitCode.InvalidUsage;
            }
            else
            {
                try
                {
                    apply();
                }
                catch (Exception ex)
                {
                    cmd.Log.Error($"Operation failed: {ex.Message}");
                    return ExitCode.Failed;
                }
                response.Applied = true;
                cmd.Log.Info(planText);
            }

            // Re-read so the caller sees the resulting (or, for a dry run, the unchanged) state. A
            // read failure must not mask the write outcome, so it is non-fatal here.
            if (TryReadValues(readValues, out var values, out var readError))
                response.Values = values;
            else
                cmd.Log.Warning($"Read-back of {group} settings failed: {readError}");

            cmd.Result.Write(response);
            return ExitCode.Success;
        }

        private static bool TryReadValues(Func<Dictionary<string, object>> readValues,
            out Dictionary<string, object> values, out string error)
        {
            try
            {
                values = readValues() ?? new Dictionary<string, object>();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                values = null;
                error = ex.Message;
                return false;
            }
        }
    }
}
