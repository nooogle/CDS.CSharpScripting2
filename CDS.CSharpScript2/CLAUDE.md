# CLAUDE.md

Guidance for working within `CDS.CSharpScript2` specifically.

### Sub-namespaces

- **`Classification`** — maps Roslyn classification spans to `ClassificationColorScheme` entries for syntax highlighting.
- **`CodeCompletion`** — wraps Roslyn's Completion API; `CompletionMatcher` filters and ranks items the way Visual Studio does (the typed text need not be a prefix — "Me" and "CM" both offer `CreateMenu`), and `EnclosingBracket` tells an editor which closing bracket should commit the open list.
- **`APIInfo`** — extracts type/member metadata and XML-doc for signature help and hover info.
