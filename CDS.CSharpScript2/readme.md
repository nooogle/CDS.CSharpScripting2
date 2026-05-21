# CDS.CSharpScript2

Roslyn-powered C# scripting engine for .NET 10.

## Description

`CDS.CSharpScript2` is the core scripting engine that lets you embed full C# scripting — compilation,
execution, IntelliSense, and syntax classification — into any .NET 10 application.

## Features

- **Compile & execute** C# scripts with Roslyn, including support for globals (host-supplied state).
- **IntelliSense** — code completion, symbol classification, and signature help (call tips).
- **Diagnostics** — surface compiler errors and warnings back to your UI.
- **Assembly references** — reference any managed assembly, including third-party NuGet packages.
- **Immutable configuration** — compose `ScriptEnvironment` instances fluently; safe to cache and share.

## Quick Start

```csharp
using CDS.CSharpScript2;

// Build an environment (add namespaces, assembly refs, optional globals type)
var env = ScriptEnvironment.Default
    .WithImport("System.Math")
    .WithAssembly(typeof(MyGlobals).Assembly);

// Compile
var compiled = await ScriptCompiler.CompileAsync("Math.PI * 2", env);

// Execute
double result = await ScriptRunner.RunAsync<double>(compiled);
```

## Key Types

| Type | Purpose |
|------|---------|
| `ScriptEnvironment` | Immutable configuration: imports, references, globals type |
| `ScriptCompiler` | Compiles a script string into a `CompiledScript` |
| `CompiledScript` | Snapshot: syntax tree, semantic model, diagnostics, classified spans |
| `ScriptRunner` | Executes a `CompiledScript`, returning a typed result |
| `ScriptManager` | Higher-level orchestrator wiring compilation, completion, and API info for editor use |

## Editor Integration

Pair with one of the editor packages for a ready-to-use WinForms control:

- **[CDS.CSharpScript2.ScintillaEditor](https://www.nuget.org/packages/CDS.CSharpScript2.ScintillaEditor)** — full-featured Scintilla5-based editor
- **[CDS.CSharpScript2.RTFEditor](https://www.nuget.org/packages/CDS.CSharpScript2.RTFEditor)** — lightweight RTF-based editor

## Requirements

- .NET 10.0

## Attributions

Icon by [Flaticon Uicons](https://www.flaticon.com/uicons)
