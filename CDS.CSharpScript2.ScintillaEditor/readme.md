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
- **Code folding** — fold and unfold regions; save and restore the folded state.
- **Find/Replace** — built-in find and replace dialog.
- **Documentation panel** — companion `DocsPanel` showing full XML docs for the symbol at the caret (F1).
- **Output panel** — companion `RTFOutputPanel` for displaying script results.
- **Ready-made panel** — `ScriptEditorPanel` combines the editor, output panel and docs panel in one control.

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
| `CompileAsync()` / `CompileAsync(cancellationToken)` | Full Roslyn compilation; returns an `ExecutableScript`. |
| `CurrentCompiledScript` | The result of the last `CompileAsync()` call, or `null` if the script has changed since. |
| `HighlightText(start, length)` | Highlights a range in the editor. |
| `ClearHighlightText()` | Removes the active highlight. |
| `ExpandAllFolds()` / `CollapseAllFolds()` | Expands or collapses every foldable region. |
| `CollapsedFoldLines` | Get/set the 0-based line numbers of the collapsed folds, for saving and restoring fold state. |
| `Manager` | The underlying `EditorManager`, for hosts that need the analysis results directly (for example the syntax tree). |
| `ScriptChanged` | Event raised when the user modifies the script text. |
| `DiagnosticsUpdated` | Event raised when the live-analysis diagnostic set changes. |

## Documentation panel (F1)

`DocsPanel` is a plain `UserControl` that shows the full XML documentation for the symbol at the
caret: signature, summary, parameters, returns, exceptions, remarks, overloads, and a type's members,
with links between related symbols. Press F1 in the editor to bring it up; it then follows the caret,
until you click a link (it then stays on the pages you choose, with Back and Forward) and F1 returns it
to following the caret. Colours follow the editor's `Theme`.

The panel never shows or hides itself, so you decide where it lives (a fixed column, a docking pane,
a tab, a floating form). Connect it to the editor and react to its two events:

```csharp
var docsPanel = new DocsPanel { Visible = false, Width = 340, Dock = DockStyle.Right };
Controls.Add(docsPanel);

docsPanel.Editor = editor;
docsPanel.ShowRequested += (_, _) => docsPanel.Visible = true;    // F1 in the editor
docsPanel.CloseRequested += (_, _) => docsPanel.Visible = false;  // close button or Escape
```

If you hide the panel by some other means (a docking pane's own close button), call
`docsPanel.Close()` so that F1 starts fresh. While nothing is subscribed to the editor's
`ScriptHelpRequested` event (which the panel does when `Editor` is set), F1 opens the older
provisional API information window instead.

See the [CDS.CSharpScript2.WinForms.Sample](https://github.com/nooogle/CDS.CSharpScripting2/tree/master/CDS.CSharpScript2.WinForms.Sample)
project for a full working example.

## Ready-made panel (`ScriptEditorPanel`)

`ScriptEditorPanel` is the three controls above already wired together: the editor on top, the
output panel below it, and the documentation panel in a column to the right that appears on F1 and
goes away when closed. For most hosts it is the quickest way in:

```csharp
var panel = new ScriptEditorPanel { Dock = DockStyle.Fill };
Controls.Add(panel);

panel.API.Environment = ScriptEnvironment.Default;   // the editor's scripting API
panel.API.Script = "Console.WriteLine(\"Hello\");";

var executable = await panel.API.CompileAsync();
panel.Output.AppendLine("Compiled");                 // the output panel
panel.Theme = EditorTheme.Dark;                      // editor + output + docs, together
```

`Editor`, `Output` and `Docs` expose the individual controls when you need their WinForms surface.
If you need a different arrangement, use the controls individually instead.

## Requirements

- .NET 10.0 (Windows) or .NET Framework 4.8
- x64 or ARM64 — Scintilla5 is a native library; AnyCPU is not supported at runtime

## Dependencies

- [CDS.CSharpScript2](https://www.nuget.org/packages/CDS.CSharpScript2) — core scripting engine
- [Scintilla5.NET](https://www.nuget.org/packages/Scintilla5.NET) — managed wrapper for Scintilla5

## Attributions

Icon by [Flaticon Uicons](https://www.flaticon.com/uicons)
