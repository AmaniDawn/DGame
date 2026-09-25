using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Unity.Pipeline.CodeGen
{
    /// <summary>
    /// Weaves a hot-reload dispatch prologue into every method tagged [HotReload] at compile
    /// time, so a running method can route to a hot-reloaded override with no domain reload. The
    /// injected prologue is the auto-generated equivalent of the helper workflow's
    /// HotReloadHelper.ExecuteWithHotReload(...) call:
    ///
    ///     if (HotReloadRegistry.TryInvokeHotReload("Type.Method", this, args)) return;
    ///     // ... original body ...
    ///
    /// MVP: instance methods returning void (parameters supported, boxed into object[]). Non-void
    /// methods are skipped with a diagnostic (return-value dispatch is deferred).
    /// </summary>
    public class HotReloadInPlaceILPostProcessor : ILPostProcessor
    {
        private const string RuntimeAssemblyName = "Unity.Pipeline";
        private const string AttributeName = "HotReloadAttribute";
        private const string RegistryTypeFullName = "Unity.Pipeline.HotReload.HotReloadRegistry";
        private const string DispatchMethodName = "TryInvokeHotReload";

        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            // Only assemblies that reference the runtime assembly can use [HotReload] or call
            // the registry. This naturally excludes the runtime assembly itself (it does not
            // reference itself) and the CodeGen assembly (no runtime reference).
            return compiledAssembly.References.Any(
                r => Path.GetFileNameWithoutExtension(r) == RuntimeAssemblyName);
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var diagnostics = new List<DiagnosticMessage>();

            using (var resolver = new PostProcessorAssemblyResolver(compiledAssembly.References))
            using (var assembly = ReadAssembly(compiledAssembly, resolver))
            {
                var module = assembly.MainModule;

                var targets = module.GetTypes()
                    .SelectMany(t => t.Methods)
                    .Where(m => m.HasBody && m.CustomAttributes.Any(a => a.AttributeType.Name == AttributeName))
                    .ToList();

                if (targets.Count == 0)
                    return new ILPostProcessResult(null, diagnostics);

                var dispatch = ResolveDispatchMethod(module, resolver, diagnostics);
                if (dispatch == null)
                    return new ILPostProcessResult(null, diagnostics);

                int wovenCount = 0;
                foreach (var method in targets)
                {
                    if (method.IsStatic)
                    {
                        diagnostics.Add(Warn($"[HotReload] '{method.FullName}' is static and was skipped (instance methods only)."));
                        continue;
                    }

                    if (method.ReturnType.MetadataType != MetadataType.Void)
                    {
                        diagnostics.Add(Warn($"[HotReload] '{method.FullName}' returns a value and was skipped (void methods only for now)."));
                        continue;
                    }

                    WeaveDispatchPrologue(method, dispatch);
                    wovenCount++;
                }

                if (wovenCount == 0)
                    return new ILPostProcessResult(null, diagnostics);

                return new ILPostProcessResult(WriteAssembly(assembly), diagnostics);
            }
        }

        /// <summary>
        /// Inject, at the very top of the method:
        ///     if (HotReloadRegistry.TryInvokeHotReload("Type.Method", this, args)) return;
        /// </summary>
        private static void WeaveDispatchPrologue(MethodDefinition method, MethodReference dispatch)
        {
            var body = method.Body;
            body.SimplifyMacros();

            var il = body.GetILProcessor();
            var first = body.Instructions[0];
            var methodId = $"{method.DeclaringType.Name}.{method.Name}";

            var prologue = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldstr, methodId),
                Instruction.Create(OpCodes.Ldarg_0), // this
            };

            // Build the object[] of parameters (null when parameterless).
            var ps = method.Parameters;
            if (ps.Count == 0)
            {
                prologue.Add(Instruction.Create(OpCodes.Ldnull));
            }
            else
            {
                prologue.Add(Instruction.Create(OpCodes.Ldc_I4, ps.Count));
                prologue.Add(Instruction.Create(OpCodes.Newarr, method.Module.TypeSystem.Object));
                for (int i = 0; i < ps.Count; i++)
                {
                    prologue.Add(Instruction.Create(OpCodes.Dup));
                    prologue.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                    prologue.Add(Instruction.Create(OpCodes.Ldarg, ps[i]));
                    if (ps[i].ParameterType.IsValueType || ps[i].ParameterType.IsGenericParameter)
                        prologue.Add(Instruction.Create(OpCodes.Box, ps[i].ParameterType));
                    prologue.Add(Instruction.Create(OpCodes.Stelem_Ref));
                }
            }

            prologue.Add(Instruction.Create(OpCodes.Call, dispatch));
            prologue.Add(Instruction.Create(OpCodes.Brfalse, first)); // no override -> run original body
            prologue.Add(Instruction.Create(OpCodes.Ret));            // override ran -> skip body

            foreach (var instr in prologue)
                il.InsertBefore(first, instr);

            body.OptimizeMacros();
        }

        /// <summary>
        /// Resolve HotReloadRegistry.TryInvokeHotReload(string, object, object[]) from the runtime
        /// assembly and import it into the target module.
        /// </summary>
        private static MethodReference ResolveDispatchMethod(
            ModuleDefinition module, PostProcessorAssemblyResolver resolver, List<DiagnosticMessage> diagnostics)
        {
            var runtimeRef = module.AssemblyReferences.FirstOrDefault(a => a.Name == RuntimeAssemblyName);
            if (runtimeRef == null)
            {
                diagnostics.Add(Warn($"Could not find a reference to {RuntimeAssemblyName} while weaving {module.Name}."));
                return null;
            }

            var runtime = resolver.Resolve(runtimeRef);
            var registry = runtime?.MainModule.GetType(RegistryTypeFullName);
            if (registry == null)
            {
                diagnostics.Add(Warn($"Could not resolve {RegistryTypeFullName} in {RuntimeAssemblyName}."));
                return null;
            }

            var method = registry.Methods.FirstOrDefault(m =>
                m.Name == DispatchMethodName &&
                m.IsStatic &&
                !m.HasGenericParameters &&
                m.Parameters.Count == 3 &&
                m.Parameters[0].ParameterType.MetadataType == MetadataType.String &&
                m.Parameters[1].ParameterType.MetadataType == MetadataType.Object);

            if (method == null)
            {
                diagnostics.Add(Warn($"Could not find non-generic {DispatchMethodName}(string, object, object[]) on {RegistryTypeFullName}."));
                return null;
            }

            return module.ImportReference(method);
        }

        private static AssemblyDefinition ReadAssembly(ICompiledAssembly compiledAssembly, IAssemblyResolver resolver)
        {
            var pdb = compiledAssembly.InMemoryAssembly.PdbData;
            var hasSymbols = pdb != null && pdb.Length > 0;

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadingMode = ReadingMode.Immediate,
                ReadWrite = true,
                ReadSymbols = hasSymbols,
                SymbolReaderProvider = hasSymbols ? new PortablePdbReaderProvider() : null,
                SymbolStream = hasSymbols ? new MemoryStream(pdb) : null,
            };

            var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData);
            return AssemblyDefinition.ReadAssembly(peStream, readerParameters);
        }

        private static InMemoryAssembly WriteAssembly(AssemblyDefinition assembly)
        {
            var pe = new MemoryStream();
            var pdb = new MemoryStream();
            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                WriteSymbols = true,
                SymbolStream = pdb,
            };

            assembly.Write(pe, writerParameters);
            return new InMemoryAssembly(pe.ToArray(), pdb.ToArray());
        }

        private static DiagnosticMessage Warn(string message) => new DiagnosticMessage
        {
            DiagnosticType = DiagnosticType.Warning,
            MessageData = "HotReloadInPlace: " + message,
        };
    }

    /// <summary>
    /// Minimal IAssemblyResolver that resolves referenced assemblies from the compiled assembly's
    /// reference paths (which are absolute file paths supplied by the compilation pipeline).
    /// </summary>
    internal sealed class PostProcessorAssemblyResolver : IAssemblyResolver
    {
        private readonly string[] _referencePaths;
        private readonly Dictionary<string, AssemblyDefinition> _cache = new Dictionary<string, AssemblyDefinition>();

        public PostProcessorAssemblyResolver(string[] referencePaths)
        {
            _referencePaths = referencePaths;
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
            => Resolve(name, new ReaderParameters(ReadingMode.Deferred));

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(name.Name, out var cached))
                    return cached;

                var path = _referencePaths.FirstOrDefault(
                    r => Path.GetFileNameWithoutExtension(r) == name.Name);
                if (path == null || !File.Exists(path))
                    return null;

                parameters.AssemblyResolver = this;
                var definition = AssemblyDefinition.ReadAssembly(path, parameters);
                _cache[name.Name] = definition;
                return definition;
            }
        }

        public void Dispose()
        {
            lock (_cache)
            {
                foreach (var def in _cache.Values)
                    def.Dispose();
                _cache.Clear();
            }
        }
    }
}
