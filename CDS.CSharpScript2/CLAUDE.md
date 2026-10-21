# CLAUDE.md

Guidance for working within `CDS.CSharpScript2` specifically.

### Sub-namespaces

- **`Classification`** — maps Roslyn classification spans to `ClassificationColorScheme` entries for syntax highlighting.
- **`CodeCompletion`** — wraps Roslyn's Completion API; `CompletionMatcher` filters and ranks items the way Visual Studio does (the typed text need not be a prefix — "Me" and "CM" both offer `CreateMenu`), and `EnclosingBracket` tells an editor which closing bracket should commit the open list.
- **`APIInfo`** — extracts type/member metadata and XML-doc for signature help and hover info.
- **`Documentation`** — the UI-neutral F1 documentation page model (`DocPage`, `DocRun`, `DocTarget`) and the builder behind `ScriptAnalyser.GetDocPageAsync`; XML docs are parsed into runs (text, code, link). `DocsPanelController` (following/browsing/history, loading state) and `IDocPageSource` (implemented by `EditorManager`) are UI-neutral and are the parts a UI port reuses; the Avalonia `DocsPanel` and the WinForms `DocsPanel` (in `CDS.CSharpScript2.ScintillaEditor`) are thin views over them.
