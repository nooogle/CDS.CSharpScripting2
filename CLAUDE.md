# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A Roslyn-powered C# scripting framework providing compilation, execution, IntelliSense (code completion, classification, API info), and editor controls. It ships as NuGet packages consumed by host applications that want to embed C# scripting.

## Build & Test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test UnitTests/UnitTests.csproj --configuration Release
dotnet test UnitTests/UnitTests.csproj --configuration Release --filter "ClassName.MethodName"
dotnet pack -c Release
```

CI runs restore/build/test on every push/PR via `.github/workflows/ci.yml`; tagged
releases are built, packed, and published via `.github/workflows/release.yml`.

## Project Layout

Every project except `ConsoleTest` multi-targets `net48` alongside its modern target,
so a change that only compiles on .NET 10 will break the build.

| Project | Target | Role |
|---------|--------|------|
| `CDS.CSharpScript2` | net48 + net10.0 | Core scripting engine and editor framework (Roslyn-based) — packaged |
| `CDS.CSharpScript2.ScintillaEditor` | net48 + net10.0-windows | Scintilla5-based editor control — packaged |
| `CDS.CSharpScript2.RTFEditor` | net48 + net10.0-windows | RTF-based editor control |
| `TestUtils` | net48 + net10.0-windows | Shared helpers for the sample and harness apps |
| `UnitTests` | net48 + net10.0-windows | MSTest suite |
| `CDS.CSharpScript2.WinForms.Sample` | net48 + net10.0-windows | Demo app |
| `ConsoleTest` | net10.0-windows | Manual test harness |

Only `CDS.CSharpScript2` and `CDS.CSharpScript2.ScintillaEditor` are packed; everything
else sets `IsPackable=false`.

## Architecture

### Core Engine (`CDS.CSharpScript2`)

The public surface is `ScriptEnvironment` → `ScriptContext` → either `ScriptAnalyser`
(editor feedback) or `ScriptExecutor` → `ExecutableScript` (execution).

- **`ScriptEnvironment`** — immutable configuration (namespace imports, assembly references, global type, `#r`/`#load` resolvers). Built with a fluent API; compose environments rather than mutating them. **This is the single source of truth for both compilation paths** — anything that affects how a script compiles belongs here, not in the paths themselves.
- **`ScriptContext`** — pairs script text with a configured Roslyn workspace document. Create via `CreateAsync`, then produce updated contexts with `ApplyScript`. Only the instance from `CreateAsync` owns the workspace and should be disposed; those from `ApplyScript` share it and must not be.
- **`ScriptAnalyser`** — the editor-facing path. Wraps a context to serve diagnostics, syntax tree, semantic model, classifications, completions, and API info. Construct a fresh one whenever the context changes.
- **`ScriptExecutor`** — the execution path. Compiles a context through the Roslyn scripting API into an `ExecutableScript`. Intended for run-time, not per-keystroke.
- **`ExecutableScript`** — a compiled script plus its diagnostics; `RunAsync` executes it, optionally with a globals object, and can be run repeatedly.

`ScriptCompiler`, `CompiledScript`, and `ScriptRunner` are `internal` implementation
details behind `ScriptExecutor` — not part of the package's API.

See `CDS.CSharpScript2/CLAUDE.md` for the sub-namespaces within the core engine (`Classification`, `CodeCompletion`, `APIInfo`).

### Editor Framework (`CDS.CSharpScript2/Editors`)

Lives inside the core project rather than a separate assembly. Defines `IScriptEditor`
(the contract a UI control implements), `EditorManager` (which drives analysis and raises
`DiagnosticsUpdated`), and `VirtualScriptEditor` (a headless implementation used in tests).
Editor controls wire directly to `EditorManager`; the delegate-based indirection that
previously decoupled them was removed.

## Coding Conventions

Follow these strictly:

- **Braces:** Allman style (opening brace on its own line).
- **Namespaces:** File-scoped (`namespace X;`).
- **Nullable:** Enable nullable reference types; annotate all APIs.
- **Naming:** `PascalCase` public members, `_camelCase` private fields, `s_camelCase` statics, `t_camelCase` `[ThreadStatic]`.
- **Async:** Methods returning tasks end in `Async`; include `CancellationToken` for I/O-bound ops; use `ConfigureAwait(false)` in library code.
- **APIs:** No default parameters in public APIs — use overloads instead.
- **Documentation:** XML-doc all public types, properties, and methods.
- **Files:** One public type per file; filename matches the type name.

See the `release` skill for the version-bump and tagging workflow.

## Testing Notes

- Framework: MSTest + AwesomeAssertions (`using AwesomeAssertions;`). FluentAssertions is not referenced anywhere — don't reintroduce it.
- Test categories mirror the engine subsystems: compilation, classifications, completions, diagnostics, use-cases, XML doc info.
- `UT_EditorExecutionParity` compiles a table of scripts through **both** paths (`ScriptAnalyser` and `ScriptExecutor`) and asserts they report the same errors. The two paths use different Roslyn APIs — a workspace project versus the scripting API — so anything configured in only one of them shows up as squiggles on code that compiles fine. Add a case here when you add a language or directive feature.
- `UnitTests` references `MathNet.Numerics` and the `OpenCvSharp4` packages (`OpenCvSharp4`, `.Extensions`, and the matching `runtime.win` / `runtime.win-arm64`) to verify that real-world assembly references work inside compiled scripts.
- `UnitTests` references only `CDS.CSharpScript2`; the editor controls have no direct test coverage. `VirtualScriptEditor` is the headless `IScriptEditor` used to exercise `EditorManager` without a UI.
