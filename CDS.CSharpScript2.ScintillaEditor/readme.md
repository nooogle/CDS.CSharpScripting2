# CDS.CSharpScript2.ScintillaEditor

Scintilla5-based WinForms editor control for C# script editing, powered by Roslyn.

## Description

`CDS.CSharpScript2.ScintillaEditor` provides a drop-in WinForms `UserControl` that gives your
application a professional C# script editor backed by the Scintilla5 native editing component
and full Roslyn IntelliSense.

## Features

- **Syntax highlighting** — real Roslyn classification (keywords, types, literals, comments, …).
- **Code completion** — member lists, type suggestions, and smart single-letter prioritisation.
- **Signature help (call tips)** — parameter info as you type method calls.
- **Hover tooltips** — type and XML-doc info on mouse hover.
- **Error indicators** — squiggles and a diagnostic list tied to the Roslyn compiler.
- **Output panel** — companion `RTFOutputPanel` for displaying script results.

## Quick Start

Add the control to a WinForms form in the designer or in code:

```csharp
using CDS.CSharpScript2.ScintillaEditor;

var editor = new ScintillaScriptEditor();
editor.Dock = DockStyle.Fill;
Controls.Add(editor);

// Configure a ScriptEnvironment and wire it up
editor.ScriptManager = new ScriptManager(env);
```

See the [WinFormsTest](https://github.com/nooogle/CDS.CSharpScripting2/tree/master/WinFormsTest)
project for a full working example.

## Requirements

- .NET 10.0 (Windows)
- x64 or ARM64 — Scintilla5 is a native library; AnyCPU is not supported at runtime

## Dependencies

- [CDS.CSharpScript2](https://www.nuget.org/packages/CDS.CSharpScript2) — core scripting engine
- [Scintilla5.NET](https://www.nuget.org/packages/Scintilla5.NET) — managed wrapper for Scintilla5

## Attributions

Icon by [Flaticon Uicons](https://www.flaticon.com/uicons)
