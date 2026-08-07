# CDS.CSharpScript2

Roslyn-powered C# scripting engine for .NET 10.

## Description

`CDS.CSharpScript2` lets you embed full C# scripting — compilation, execution, IntelliSense, and
syntax classification — into any .NET 10 application.

## Features

- **Compile & execute** C# scripts with Roslyn, with support for globals (host-supplied state).
- **IntelliSense** — code completion, symbol classification, and signature help (call tips).
- **Diagnostics** — surface compiler errors and warnings back to your UI.
- **Assembly references** — reference any managed assembly, including third-party NuGet packages.
- **Script directives** — `#r` to reference an assembly and `#load` to pull in another script file,
  honoured identically by the editor and the execution engine.
- **Immutable configuration** — compose `ScriptEnvironment` instances fluently; safe to cache and share.

## Quick Start

### Simple execution

```csharp
using CDS.CSharpScript2;

using var context = await ScriptContext.CreateAsync();
var ctx = context.ApplyScript("1 + 1");

var executable = await new ScriptExecutor(ctx).CompileAsync<int>();
int result = await executable.RunAsync<int>(); // 2
```

### With global variables

```csharp
public class MyGlobals
{
    public double X { get; set; }
    public double Y { get; set; }
}

var env = ScriptEnvironment.Default.WithGlobalType<MyGlobals>();

using var context = await ScriptContext.CreateAsync(env);
var ctx = context.ApplyScript("X * Y");

var executable = await new ScriptExecutor(ctx).CompileAsync<double>();
double result = await executable.RunAsync<double>(new MyGlobals { X = 3, Y = 4 }); // 12.0
```

### With additional namespaces and references

```csharp
var env = ScriptEnvironment.Default
    .WithAdditionalNamespaceForType<System.Text.StringBuilder>()
    .WithAdditionalReferenceForType<MathNet.Numerics.Constants>();

using var context = await ScriptContext.CreateAsync(env);
```

### Script directives

Scripts can reference assemblies and pull in other scripts themselves:

```csharp
using var context = await ScriptContext.CreateAsync();

var ctx = context.ApplyScript("""
    #r "C:\libs\MyLibrary.dll"
    #load "C:\scripts\helpers.csx"
    return Helper.Run(new MyLibrary.Thing());
    """);
```

Set a base directory so scripts can use relative paths instead of hard-coding machine-specific
ones — typically the folder the user's script file lives in:

```csharp
var env = ScriptEnvironment.Default.WithBaseDirectory(@"C:\scripts");

using var context = await ScriptContext.CreateAsync(env);
var ctx = context.ApplyScript("""
    #r "libs/MyLibrary.dll"
    #load "helpers.csx"
    """);
```

## Key Types

| Type | Purpose |
|------|---------|
| `ScriptEnvironment` | Immutable configuration: namespace imports, assembly references, globals type, directive base directory |
| `ScriptContext` | Roslyn workspace document paired with an environment; disposable |
| `ScriptExecutor` | Compiles a `ScriptContext` into an `ExecutableScript` |
| `ExecutableScript` | Compiled script ready to run; reports diagnostics and accepts a globals object |

## Editor Integration

Pair with the editor package for a ready-to-use WinForms control:

- **[CDS.CSharpScript2.ScintillaEditor](https://www.nuget.org/packages/CDS.CSharpScript2.ScintillaEditor)** — full-featured Scintilla5-based editor with live IntelliSense

## Requirements

- .NET 10.0

## Attributions

Icon by [Flaticon Uicons](https://www.flaticon.com/uicons)
