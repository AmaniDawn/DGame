# Hot reload

Apply edited C# to running code without restarting Play Mode or rebuilding the Player. Two flavors are supported: **in-place** reload of a tagged method body, and **override** reload that routes a method through a separately-compiled replacement.

Hot reload runs in **Editor Play Mode and Mono standalone development builds only** — not IL2CPP. Like code eval, it is gated behind `#if UNITY_EDITOR || (UNITY_STANDALONE && DEBUG)`.

## Flavor 1 — in-place (`reload_file`)

Tag the method you want to be reloadable with `[HotReload]`, then edit its body and apply the file. The running instance picks up the new body.

```csharp
using Unity.Pipeline.HotReload;
using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float rotationSpeed = 90f;

    [HotReload]
    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
```

Edit the body of `Update`, then apply it:

```bash
unity command reload_file "<absolute path>/Spinner.cs"

# Emit debug symbols so breakpoints bind (compiles unoptimized):
unity command reload_file "<absolute path>/Spinner.cs" --pdb
```

`reload_file` parameters:

| Arg | Default | Meaning |
|-----|---------|---------|
| `filename` | *(required)* | Source file containing `[HotReload]` methods. |
| `timeout` | `30000` | Compilation timeout (ms). |
| `assemblyDir` | `null` | Optional directory to also save the compiled assembly to disk (default: in-memory only). |
| `pdb` | `false` | Emit a portable PDB mapped to the original source so debugger breakpoints bind. Compiles unoptimized. |

**Constraints (in-place):** the `[HotReload]` method must be a **`void` instance method** that is **`public`**, and reload works on **Mono only**. The CodeGen ILPostProcessor skips static methods and value-returning methods at build time (with a warning), so only matching methods are weaved for dispatch.

## Flavor 2 — override (`reload_file_override`)

When you want to swap behavior from a *separate* file (leaving the original untouched), tag the method with `[HotReloadWithOverrides]` and route it through `HotReloadHelper.ExecuteWithHotReload`:

```csharp
using Unity.Pipeline.HotReload;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [HotReloadWithOverrides]
    void Update()
    {
        HotReloadHelper.ExecuteWithHotReload(this, "Update", OriginalUpdate);
    }

    public void OriginalUpdate()
    {
        transform.Rotate(45 * Time.deltaTime, 0, 0);
    }
}
```

Put the tweaked behavior in a **separate file** as a **`public static` method** tagged `[HotReloadOverrideMethod("Type.Method")]`. The override **takes the target instance as its first parameter**:

```csharp
// BossOverrides.cs — a separate file
using Unity.Pipeline.HotReload;
using UnityEngine;

public static class BossOverrides
{
    [HotReloadOverrideMethod("BossController.Update")]
    public static void TweakedUpdate(BossController instance)
    {
        instance.transform.Rotate(0, 90 * Time.deltaTime, 0); // new behaviour
    }
}
```

Apply the override file:

```bash
unity command reload_file_override "<absolute path>/BossOverrides.cs"
```

`reload_file_override` parameters:

| Arg | Default | Meaning |
|-----|---------|---------|
| `filename` | *(required)* | The override source file to compile. |
| `timeout` | `30000` | Compilation timeout (ms). |
| `assemblyDir` | `null` | Optional directory to also save the compiled assembly to disk (default: in-memory only). |

The override method's signature must be `public static <ReturnType> Name(<TargetType> instance, ...)` — return type and trailing parameters matching the original. Do **not** redeclare the target type in the override file (a common cause of "signature mismatch"). Until an override is applied, `ExecuteWithHotReload` calls `OriginalUpdate`; once applied, dispatch routes to the override.

`ExecuteWithHotReload` has matching overloads for both value-returning (`Func<object>`) and void (`Action`) originals:

```csharp
public static object ExecuteWithHotReload<T>(T instance, string methodName, Func<object> originalMethod, params object[] parameters);
public static void   ExecuteWithHotReload<T>(T instance, string methodName, Action originalMethod,       params object[] parameters);
```

## Supporting commands

- `hotreload_status` — show registry stats (which methods have overrides registered).
- `cleanup_hotreload` — remove old hot-reload assemblies and clear the registry.

## Architecture

The pipeline that turns an edited source file into running code:

1. **Compile (Roslyn, in-memory).** `RoslynCompilationService.Compile` parses and compiles the source to a `DynamicallyLinkedLibrary` entirely in memory — IL bytes in one `MemoryStream`, and (when `pdb`/`EmitDebugInformation` is requested) a **portable PDB** in another. **Nothing is written to disk** by default. When emitting debug info it compiles unoptimized (`OptimizationLevel.Debug`) so sequence points survive and breakpoints can bind.

2. **Load into the running AppDomain.** The IL (and optional PDB) bytes are loaded via `PipelineUtils.LoadFromBytes`, which selects the load mechanism by Unity version:

   ```csharp
   public static Assembly LoadFromBytes(byte[] bytes, byte[] pdb = null)
   {
   #if UNITY_6000_5_OR_NEWER
       return UnityEngine.Assemblies.CurrentAssemblies.LoadFromBytes(bytes, pdb);
   #else
       return System.Reflection.Assembly.Load(bytes, pdb);
   #endif
   }
   ```

   On Unity 6000.5+ it uses Unity's `CurrentAssemblies.LoadFromBytes`; on older versions it falls back to `Assembly.Load(bytes, pdb)`. Either way the freshly compiled code becomes a **sibling assembly** loaded next to the original in the live AppDomain.

3. **Dispatch.** Calls are routed through `HotReloadRegistry`. The registry keys dispatch on `TypeName.MethodName`; when an override is registered for that id, `TryInvokeHotReload` invokes it with the instance as the first argument (marshaling to the main thread via the injected `Dispatcher` if the override requires it). If no override is registered, the original code runs.

4. **In-place weaving (CodeGen).** For the in-place flavor, the **`HotReloadInPlaceILPostProcessor`** (built on Mono.Cecil, in the `Unity.Pipeline.CodeGen` assembly) weaves a dispatch prologue into every `[HotReload]` method **at build/compile time**. That prologue checks the registry for a reloaded body and calls it instead of the original — which is how an edited `[HotReload]` body takes effect without changing the call sites. The post-processor enforces the void-instance-method constraint, skipping (with a warning) any static or value-returning `[HotReload]` method.

Because hot reload depends on Mono's ability to load a sibling assembly and dispatch into it, it is limited to **Editor Play Mode and Mono standalone dev builds — not IL2CPP**.

## See also

- [Runtime connection & setup](runtime-setup.md) — enabling hot reload in a Player build.
- [Command reference](commands/runtime.md)
