using System;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Attribute to mark static methods as CLI-accessible commands.
    /// Commands with this attribute can be executed remotely via Pipeline Server.
    /// Adapted from unity-tools [Tool] attribute for Pipeline organization requirements.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CliCommandAttribute : Attribute
    {
        /// <summary>
        /// Unique name of the command for CLI execution.
        /// Used in "unity request command_name" syntax.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Human-readable description of what the command does.
        /// Used in CLI help text and command discovery.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Whether this command requires Unity main thread execution.
        /// Default: true (most Unity APIs require main thread).
        /// </summary>
        public bool MainThreadRequired { get; set; } = true;

        /// <summary>
        /// Whether this command belongs to the runtime (Player) command surface only.
        /// Runtime-only commands are hidden from an Editor server's command listing
        /// (they remain executable, but are not advertised when connected to an Editor).
        /// Default: false (listed on the Editor server).
        /// </summary>
        public bool RuntimeOnly { get; set; } = false;

        /// <summary>
        /// Create a new CLI command attribute.
        /// </summary>
        /// <param name="name">Unique command name for CLI execution</param>
        /// <param name="description">Human-readable description</param>
        public CliCommandAttribute(string name, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }
}