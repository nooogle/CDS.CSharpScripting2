# CDS.CSharpScripting2

> **Work in progress** — this documentation will be expanded soon.

A Roslyn-powered C# scripting framework for .NET. Embed a full C# script engine — with compilation, execution, code completion, syntax highlighting, and signature help — into any .NET application.

## Packages

| Package | Purpose |
|---------|---------|
| `CDS.CSharpScript2` | Core scripting engine |
| `CDS.CSharpScript2.ScintillaEditor` | WinForms editor control (Scintilla5-based) |

---

## Core Engine

### Simple execution

```csharp
using CDS.CSharpScript2;

// Create a context, apply a script, compile, and run
using var context = await ScriptContext.CreateAsync();
var ctx = context.ApplyScript("1 + 1");

var executor = new ScriptExecutor(ctx);
var executable = await executor.CompileAsync<int>();

int result = await executable.RunAsync<int>(); // 2
```

### With global variables

Globals expose host-application state to the script. Define a public class:

```csharp
public class MyGlobals
{
    public double X { get; set; }
    public double Y { get; set; }
}
```

Configure the environment with the globals type, then pass an instance at run time:

```csharp
var env = ScriptEnvironment.Default.WithGlobalType<MyGlobals>();

using var context = await ScriptContext.CreateAsync(env);
var ctx = context.ApplyScript("X * Y");

var executor = new ScriptExecutor(ctx);
var executable = await executor.CompileAsync<double>();

var globals = new MyGlobals { X = 3.0, Y = 4.0 };
double result = await executable.RunAsync<double>(globals); // 12.0
```

### With additional namespaces and references

`ScriptEnvironment` is immutable — each `With…` call returns a new instance:

```csharp
var env = ScriptEnvironment.Default
    .WithAdditionalNamespaceForType<System.Text.StringBuilder>()       // adds System.Text
    .WithAdditionalReferenceForType<MathNet.Numerics.Constants>();     // adds MathNet.Numerics

using var context = await ScriptContext.CreateAsync(env);
var ctx = context.ApplyScript("""
    var sb = new StringBuilder();
    sb.Append("pi ≈ ");
    sb.Append(MathNet.Numerics.Constants.Pi);
    sb.ToString()
    """);

var executable = await new ScriptExecutor(ctx).CompileAsync<string>();
string result = await executable.RunAsync<string>();
```

### Checking for compilation errors

```csharp
var executable = await new ScriptExecutor(ctx).CompileAsync();

if (executable.HasErrors)
{
    foreach (var diag in executable.Diagnostics)
        Console.WriteLine(diag);
}
else
{
    await executable.RunAsync();
}
```

---

## Scintilla Editor (WinForms)

`CDS.CSharpScript2.ScintillaEditor` provides a drop-in `UserControl` with live Roslyn analysis:

- Syntax highlighting (full Roslyn token classification)
- IntelliSense completion list
- Signature help (call tips)
- Hover tooltips with XML-doc summaries
- Error/warning squiggles and a live diagnostic list
- Find/Replace

Add the control to a form and wire up an environment:

```csharp
using CDS.CSharpScript2.ScintillaEditor;

var editor = new ScintillaScriptEditor();
editor.Dock = DockStyle.Fill;
Controls.Add(editor);

// Set the environment — live analysis starts immediately
editor.API.Environment = ScriptEnvironment.Default;
```

Compile and run the script the user has typed:

```csharp
var executable = await editor.API.CompileAsync();

if (!executable.HasErrors)
    await executable.RunAsync();
```

All scripting properties and methods are accessed through `editor.API` to keep them separate from the standard WinForms `Control` surface.

---

## Attributions

Icons by [Flaticon Uicons](https://www.flaticon.com/uicons)
