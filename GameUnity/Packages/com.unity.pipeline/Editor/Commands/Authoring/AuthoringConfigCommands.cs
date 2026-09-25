using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;

namespace Unity.Pipeline.Editor.Commands.Authoring
{
    /// <summary>
    /// Commands to inspect and configure the authoring root — the base folder (under Assets/) that
    /// bare authoring paths resolve against and that authoring writes are confined to. Persisted
    /// per project; the default is "Assets" (full Assets access).
    /// </summary>
    public static class AuthoringConfigCommands
    {
        [CliCommand("get_authoring_root", "Get the base folder (under Assets/) that bare authoring paths resolve against.")]
        public static object GetAuthoringRoot()
        {
            return new { root = ProjectPaths.AuthoringRoot };
        }

        [CliCommand("set_authoring_root", "Set the base folder (under Assets/) that bare authoring paths resolve against and are confined to. Use 'Assets' for full project access.")]
        public static object SetAuthoringRoot(
            [CliArg("root", "Project-relative folder under Assets/, e.g. Assets/AgentWork. Use 'Assets' to allow the whole project.", Required = true)] string root)
        {
            // Throws ArgumentException for invalid roots (outside Assets/ or containing ".."); the
            // server surfaces that message to the caller.
            ProjectPaths.AuthoringRoot = root;
            return new { root = ProjectPaths.AuthoringRoot };
        }
    }
}
