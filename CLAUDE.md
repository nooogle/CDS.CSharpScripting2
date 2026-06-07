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

The full pipeline (clean → restore → build → test) is in `build.cake`, runnable via `./build.ps1`.

## Project Layout

| Project | Target | Role |
|---------|--------|------|
| `CDS.CSharpScript2` | net10.0 | Core scripting engine (Roslyn-based) |
| `CDS.CSharpScript2.Core` | net10.0 | Editor framework interfaces and abstractions — packaged |
| `CDS.CSharpScript2.ScintillaEditor` | net10.0-windows | Scintilla5-based editor control — packaged |
| `CDS.CSharpScript2.RTFEditor` | net10.0-windows | RTF-based editor control |
| `CDS.CSharpScriptUtils` | net8.0-windows + net48 | Legacy dual-target library — packaged |
| `UnitTests` | net10.0-windows | MSTest suite |
| `CDS.CSharpScript2.WinForms.Sample` / `ConsoleTest` | net10.0-windows | Demo app / manual test harness |

## Architecture

### Core Engine (`CDS.CSharpScript2`)

- **`ScriptEnvironment`** — immutable configuration (namespace imports, assembly references, global type). Built with a fluent API; compose environments rather than mutating them.
- **`ScriptCompiler`** — static wrapper over `CSharpScript.Create()`. Takes a `ScriptEnvironment` and returns a `CompiledScript`.
- **`CompiledScript`** — snapshot of a compiled script: syntax tree, semantic model, diagnostics, classified spans.
- **`ScriptRunner`** — executes a `CompiledScript` synchronously or asynchronously, returning a typed result.
- **`ScriptManager`** — higher-level orchestrator that wires compilation, classification, completion, and API info together for editor use.

### Sub-namespaces

- **`Classification`** — maps Roslyn classification spans to `ClassificationColorScheme` entries for syntax highlighting.
- **`CodeCompletion`** — wraps Roslyn's Completion API; `SingleLetterMatchSorter` applies smart prioritisation.
- **`APIInfo`** — extracts type/member metadata and XML-doc for signature help and hover info.

### Editor Framework (`CDS.CSharpScript2.Core`)

Defines `IEditor`, `EditorManager`, and three delegate signatures (`ApplyScriptDelegateAsync`, `GetAutoCompleteListDelegateAsync`, `GetAPIInfoDelegateAsync`) that decouple the engine from specific UI controls.

## Coding Conventions

Taken from `.github/copilot-instructions.md` — follow these strictly:

- **Braces:** Allman style (opening brace on its own line).
- **Namespaces:** File-scoped (`namespace X;`).
- **Nullable:** Enable nullable reference types; annotate all APIs.
- **Naming:** `PascalCase` public members, `_camelCase` private fields, `s_camelCase` statics, `t_camelCase` `[ThreadStatic]`.
- **Async:** Methods returning tasks end in `Async`; include `CancellationToken` for I/O-bound ops; use `ConfigureAwait(false)` in library code.
- **APIs:** No default parameters in public APIs — use overloads instead.
- **Documentation:** XML-doc all public types, properties, and methods.
- **Files:** One public type per file; filename matches the type name.

## Versioning & Releasing

Versions are managed by **MinVer** from Git tags. Patch increments happen automatically; bump minor/major by editing `version.json` then tagging.

```shell
# Bump major/minor
# Edit version.json: { "version": "3.0" }
git commit -am "breaking changes for 3.0"
git tag v3.0.0
git push --tags
dotnet pack -c Release
```

CI (`.github/workflows/ci.yml`) triggers on `v*` tags, builds on `windows-latest`, runs tests, and uploads NuGet packages as artifacts.

## Testing Notes

- Framework: MSTest + FluentAssertions (also AwesomeAssertions in some files).
- Test categories mirror the engine subsystems: compilation, classifications, completions, diagnostics, use-cases, XML doc info.
- Test projects reference `MathNet.Numerics` and `OpenCvSharp4.Windows` to verify that real-world assembly references work inside compiled scripts.
