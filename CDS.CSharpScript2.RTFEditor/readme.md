# CDS.CSharpScript2.RTFEditor

Lightweight RTF-based WinForms editor control for C# script editing, powered by Roslyn.

## Description

`CDS.CSharpScript2.RTFEditor` provides a simple WinForms `UserControl` for C# script editing
using the built-in `RichTextBox`. It has no native library dependency, making it easier to
distribute than the Scintilla-based editor while still offering Roslyn-powered IntelliSense.

## Features

- **Syntax highlighting** — Roslyn classification rendered via RTF colour runs.
- **Code completion** — member lists and type suggestions.
- **Signature help** — parameter info for method calls.
- **Error indicators** — diagnostic list from the Roslyn compiler.
- **No native dependencies** — runs as AnyCPU; no Scintilla5 native DLL required.

## Quick Start

```csharp
using CDS.CSharpScript2.RTFEditor;

var editor = new RTFScriptEditor();
editor.Dock = DockStyle.Fill;
Controls.Add(editor);

// Configure a ScriptEnvironment and wire it up
editor.ScriptManager = new ScriptManager(env);
```

See the [WinFormsTest](https://github.com/nooogle/CDS.CSharpScripting2/tree/master/WinFormsTest)
project for a full working example.

## When to use each editor

| | ScintillaEditor | RTFEditor |
|---|---|---|
| Rendering quality | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| Native dependency | Scintilla5 (x64/ARM64) | None |
| AnyCPU support | ✗ | ✓ |

## Requirements

- .NET 10.0 (Windows)

## Dependencies

- [CDS.CSharpScript2](https://www.nuget.org/packages/CDS.CSharpScript2) — core scripting engine

## Attributions

Icon by [Flaticon Uicons](https://www.flaticon.com/uicons)
