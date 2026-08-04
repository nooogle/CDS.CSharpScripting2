# Script Libraries via Roslyn Submission Chaining — Research Notes

**Status:** research complete, design not yet chosen. No code committed.
**Date:** 2026-08-03
**Roslyn version tested:** `Microsoft.CodeAnalysis.*` 5.6.0, net10.0

## Goal

Let a host application keep a small library of reusable code blocks, each compiled through this
library. A main script is then chained on top of those blocks so it can call the functions and use
the types they declare.

Roslyn's scripting API supports this through `Script.ContinueWith`, which builds a chain of
*submissions*. Each submission is its own compilation and its own emitted assembly, and every
submission can see the declarations of all submissions before it.

## Findings

All findings below were verified empirically against Roslyn 5.6.0 rather than taken from
documentation. The probe project used to produce them is described at the end.

### Execution chaining works as needed

A chain of `block1 → block2 → main` gives the main script full access to classes, methods and
variables declared in the earlier blocks, together with the globals object set at the root of the
chain.

```
block1: 0 diagnostics
block2: 0 diagnostics
main:   0 diagnostics
result = 11.414213562373096
```

The main script called `MakeVec()` (a function declared in block2), which constructed a `Vec` (a
class declared in block1), and multiplied by `Scale` (a property on the globals object). Each
submission gets its own compilation and assembly:

```
main:            ℛ*e8897bc3-…#1-2
main.Previous:   ℛ*e8897bc3-…#1-1
main.Previous.Previous: ℛ*e8897bc3-…#1-0
```

### IntelliSense can model the same chain

This was the main unknown. It works.

In an `AdhocWorkspace`, create one project per block with `isSubmission: true`, and give each
project a `ProjectReference` to its predecessor. The main script's document lives in the last
project. Roslyn resolves the previous-submission relationship from the project reference, and
completion in the main document then sees chained symbols:

```
member access 'v.'        -> Length, X, Y     (members of a type declared in block1)
identifier prefix 'Make'  -> MakeVec          (function declared in block2)
identifier prefix 'defaultV' -> defaultVec    (variable declared in block2)
identifier prefix 'Ve'    -> Vec              (type declared in block1)
```

Edits to an *earlier* block flow through to the main document. Renaming a library function produced
the expected error against the main script on the next compilation:

```
before edit: 0 diagnostics
after renaming the library method: 1 diagnostic
    (1,8): error CS0103: The name 'Twice' does not exist in the current context
```

So live editing of library blocks is viable, not just static preloading.

### Caching the chain pays off

The library chain should be built and compiled once, with the tail `Script` object retained. Each
main-script edit is then a `ContinueWith` on that cached tail.

Measured with a 20-block library:

| Operation | Cost |
|---|---|
| Build the 20-block chain (lazy, no compile) | 0 ms |
| Compile the whole chain | 472 ms (once) |
| Compile the main script on top | 24 ms |
| First run | 3 ms |
| Second run of the same main script | 0 ms |
| New main-script text → compile + run | 23 ms |

The 472 ms is paid once per library, not per execution. Discarding the tail `Script` between runs
would pay it every time.

## Constraints

These are the behaviours that must shape any design.

| Behaviour | Consequence |
|---|---|
| Blocks cannot declare a `namespace` — `error: Cannot declare namespace in script code` (both block-scoped and file-scoped) | The library is a flat namespace; no namespacing available for collision avoidance |
| **Extension methods are impossible** — `Extension methods must be defined in a top level static class; Ext is a nested class` | Submissions compile to nested classes, so no block in a chain can declare an extension method |
| Duplicate type names across blocks compile with 0 diagnostics; the later block silently wins | Name collisions become silent behaviour changes rather than errors |
| Errors in a broken block surface in the main script's diagnostics (4 errors from the block appeared against the main compilation) | Editor squiggles must be filtered by `Location.SourceTree`, with library errors reported separately |
| Every `RunAsync` replays the entire chain from the start (verified: a block's `Console.WriteLine` fired on both runs) | Library blocks must be declaration-only, or their side effects repeat on every execution |
| `ScriptState.ContinueWithAsync` shares mutable state across branches — a counter read 1, 2, 3 when branching three times from the same state | The "run the library once and keep the state" optimisation leaks state between runs; unsuitable for repeated independent execution |
| One non-collectible assembly per compilation — 20 runs added 20 assemblies, `IsCollectible: false` | A long-lived editor host grows monotonically. Chain caching limits this to one assembly per main-script edit, but cannot eliminate it |
| A block only sees blocks before it | Ordering is the host's responsibility; the library must be topologically sorted by dependency |

### What a block may legally contain

Verified against Roslyn 5.6.0:

| Construct | Result |
|---|---|
| `public class` | OK |
| `public record` | OK (usable from later blocks — `Pt { X = 1, Y = 2 }`) |
| `public interface` + implementation | OK |
| `public static class` | OK |
| Generic method | OK |
| `enum` | OK |
| `delegate` | OK |
| Top-level statement | OK (but re-executes on every run) |
| `using` directive | OK — and usings **do** flow forward to later blocks |
| `#r` directive | OK |
| `namespace` (block or file-scoped) | **Error** |
| Extension method | **Error** |

## Proposed design

Three touch points, all additive to the existing pipeline.

### 1. `ScriptCompiler` — chain support

[`ScriptCompiler.Compile<T>`](CDS.CSharpScript2/ScriptCompiler.cs#L91) currently calls
`CSharpScript.Create<T>(...)`. Add an overload accepting a `Script? previous`, which calls
`previous.ContinueWith<T>(...)` when non-null and falls back to `Create` otherwise.

### 2. `ScriptLibrary` / `CompiledScriptLibrary` — new types

`ScriptLibrary`: an immutable, ordered list of named blocks, built with a fluent API in the style of
[`ScriptEnvironment`](CDS.CSharpScript2/ScriptEnvironment.cs).

`CompiledScriptLibrary`: holds the compiled tail `Script` of the chain so it survives across
main-script edits. This is what turns 472 ms per run into 24 ms per run.

[`ScriptExecutor.CompileAsync<T>`](CDS.CSharpScript2/ScriptExecutor.cs#L26) gains a library
parameter and passes the cached tail through to the compiler.

### 3. `ScriptContext` — chained workspace

[`ScriptContext.CreateCore`](CDS.CSharpScript2/ScriptContext.cs#L67) currently builds a single
project. It should build N chained submission projects — one per library block plus one for the main
script — each referencing its predecessor. `ApplyScript` updates only the last document.

`ScriptAnalyser`, code completion, classification and API info then work unchanged; this was
verified against a chained workspace.

### 4. Diagnostics filtering

[`ScriptAnalyser.GetDiagnosticsAsync`](CDS.CSharpScript2/ScriptAnalyser.cs#L37) needs to filter by
source tree so that errors originating in library blocks do not squiggle the main editor. Library
errors should be surfaced through a separate channel so the host can report a broken library
distinctly from a broken script.

## The open decision

There is a genuine alternative to submission chaining: compile the library blocks into **one real
assembly** (`CSharpCompilation` emitted to an in-memory DLL) and add it to
`ScriptEnvironment.References`.

| | Submission chaining | Compiled assembly |
|---|---|---|
| Namespaces in blocks | No | Yes |
| Extension methods | No | Yes |
| Name collisions | Silent shadowing | Real compiler errors |
| Assembly lifetime | Non-collectible, accumulates | Collectible `AssemblyLoadContext` |
| IntelliSense plumbing | Chained submission projects | Just a metadata reference |
| Block authoring style | Script syntax — top-level statements, globals access | Ordinary C# file syntax only |
| Blocks can use the globals object | Yes | No |

Chaining suits script-style authoring, where blocks are written in the same editor as the main script
and feel like scripts. The compiled-assembly route is materially cleaner if the library blocks are
really just types and helper functions.

**Recommendation:** chaining, on the basis that blocks are authored as scripts in the same editor.
But the extension-method restriction alone may be enough to decide the other way, so this should be
settled before implementation begins.

## Reproducing the research

The probe project lives in the session scratchpad at `ChainProbe/` (a standalone net10.0 console app
referencing the same four Roslyn 5.6.0 packages as `CDS.CSharpScript2`). It covers:

- **A** — execution chaining across blocks, with globals
- **B** — recompilation cost when only the main script changes
- **C** — workspace submission chain and completion resolution
- **D** — `ScriptState` reuse and cross-branch state leakage
- **E** — duplicate declarations across blocks
- **F** — `using` directives flowing forward
- **G** — diagnostics from a broken library block
- **H** — assembly accumulation over repeated runs
- **I** — editing an earlier block in the workspace chain
- **J** — which constructs a block may legally contain
- **K** — re-execution of library blocks on each run
- **L** — cost at a realistic 20-block library size

The scratchpad is session-scoped and will not persist; the probes are cheap to recreate from the
findings above if needed.
