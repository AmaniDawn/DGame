using System.Collections.Generic;
using System.Reflection;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// Interface for command discovery mechanism.
    /// Allows Runtime assembly to discover commands without directly using Editor APIs.
    /// </summary>
    public interface ICommandDiscovery
    {
        /// <summary>
        /// Find all methods marked with CliCommand attribute.
        /// Implementation uses TypeCache in Editor or reflection fallback in Runtime.
        /// </summary>
        IEnumerable<MethodInfo> GetMethodsWithAttribute<T>() where T : System.Attribute;
    }
}