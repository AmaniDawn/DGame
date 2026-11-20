using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Unity.Pipeline.Commands
{
    /// <summary>
    /// What a command method's signature says about how it is invoked: whether it takes a
    /// <see cref="CommandContext"/>, and whether it returns its exit code (<see cref="ExitCode"/>, or a
    /// <see cref="Task{TResult}"/> of one for a command that runs across frames) or a result to write
    /// (the legacy signature).
    /// </summary>
    static class CommandSignature
    {
        /// <summary>The index of the method's <see cref="CommandContext"/> parameter; -1 for none.</summary>
        internal static int ContextParameterIndex(MethodInfo method)
        {
            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (IsContext(parameters[i]))
                    return i;
            }
            return -1;
        }

        /// <summary>Whether <paramref name="parameter"/> is the one the pipeline passes the context to.</summary>
        /// <remarks>
        /// Also an <c>in</c>, <c>ref</c> or <c>out</c> one, so it is never taken for an argument the
        /// user gives; <see cref="Error"/> refuses those.
        /// </remarks>
        internal static bool IsContext(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;
            if (type.IsByRef)
                type = type.GetElementType();
            return type == typeof(CommandContext);
        }

        /// <summary>Whether the method returns its <see cref="ExitCode"/>, at once or as a <see cref="Task{TResult}"/>.</summary>
        internal static bool ReturnsExitCode(MethodInfo method) =>
            method.ReturnType == typeof(ExitCode) || method.ReturnType == typeof(Task<ExitCode>);

        /// <summary>
        /// Why the method can't be a command, or null when it can. A command that takes a
        /// <see cref="CommandContext"/> takes it by value, returns its exit code, and takes one context at most.
        /// </summary>
        internal static string Error(MethodInfo method)
        {
            var contexts = 0;
            foreach (var parameter in method.GetParameters())
            {
                if (!IsContext(parameter))
                    continue;
                if (parameter.ParameterType.IsByRef)
                    return $"it takes its {nameof(CommandContext)} by reference ('{parameter.Name}'); take it by value, as the pipeline passes it.";
                contexts++;
            }

            if (contexts > 1)
                return $"it takes {contexts} {nameof(CommandContext)} parameters; a command takes one at most.";
            if (contexts == 1 && !ReturnsExitCode(method))
                return $"it takes a {nameof(CommandContext)} but returns {method.ReturnType.Name}; such a command returns its {nameof(ExitCode)}, or a Task<{nameof(ExitCode)}>.";
            return null;
        }
    }
}
