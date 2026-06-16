# CDS.CSharpScript2.ScintillaEditor

Scintilla5-based WinForms editor control for C# script editing, powered by Roslyn.

## Description

`CDS.CSharpScript2.ScintillaEditor` provides a drop-in WinForms `UserControl` that gives your
application a professional C# script editor backed by the Scintilla5 native editing component
and full Roslyn IntelliSense.

## Features

- **Syntax highlighting** — real Roslyn token classification (keywords, types, literals, comments, …).
- **Code completion** — member lists, type suggestions, and smart single-letter prioritisation.
- **Signature help (call tips)** — parameter info as you type method calls.
- **Hover tooltips** — type and XML-doc info on mouse hover.
- **Error indicators** — squiggles and a live diagnostic list tied to the Roslyn compiler.
- **Find/Replace** — built-in find and replace dialog.
- **Output panel** — companion `RTFOutputPanel` for displaying script results.

## Quick Start

Add `ScintillaScriptEditor` to a form and wire up a `ScriptEnvironment`:

```csharp
using CDS.CSharpScript2;
using CDS.CSharpScript2.ScintillaEditor;

var editor = new ScintillaScriptEditor();
editor.Dock = DockStyle.Fill;
Controls.Add(editor);

// Setting Environment starts live analysis immediately
editor.API.Environment = ScriptEnvironment.Default;
```

Compile and run the script the user has typed:

```csharp
var executable = await editor.API.CompileAsync();

if (!executable.HasErrors)
    await executable.RunAsync();
```

All scripting properties and methods are accessed through `editor.API` to keep them
separate from the standard WinForms `Control` surface.

## API Surface (`editor.API`)

| Member | Description |
|--------|-------------|
| `Environment` | Get/set the `ScriptEnvironment` (references, namespaces, globals type). Setting it restarts live analysis. |
| `Script` | Get/set the script text shown in the editor. |
| `HasErrors` | `true` when the most recent live-analysis pass found at least one error. |
| `CurrentDiagnostics` | Diagnostics from the most recent analysis pass. |
| `CompileAsync()` | Full Roslyn compilation; returns an `ExecutableScript`. |
| `CurrentCompiledScript` | The result of the last `CompileAsync()` call, or `null` if the script has changed since. |
| `HighlightText(start, length)` | Highlights a range in the editor. |
| `ClearHighlightText()` | Removes the active highlight. |
| `ScriptChanged` | Event raised when the user modifies the script text. |
| `DiagnosticsUpdated` | Event raised when the live-analysis diagnostic set changes. |

See the [CDS.CSharpScript2.WinForms.Sample](https://github.com/nooogle/CDS.CSharpScripting2/tree/master/CDS.CSharpScript2.WinForms.Sample)
project for a full working example.

## Requirements

- .NET 10.0 (Windows)
- x64 or ARM64 — Scintilla5 is a native library; AnyCPU is not supported at runtime

## Dependencies

- [CDS.CSharpScript2](https://www.nuget.org/packages/CDS.CSharpScript2) — core scripting engine
- [Scintilla5.NET](https://www.nuget.org/packages/Scintilla5.NET) — managed wrapper for Scintilla5

## Attributions

Icon by [Flaticon Uicons](https://www.flaticon.com/uicons)
