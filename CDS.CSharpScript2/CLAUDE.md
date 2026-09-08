# CLAUDE.md

Guidance for working within `CDS.CSharpScript2` specifically.

### Sub-namespaces

- **`Classification`** — maps Roslyn classification spans to `ClassificationColorScheme` entries for syntax highlighting.
- **`CodeCompletion`** — wraps Roslyn's Completion API; `SingleLetterMatchSorter` applies smart prioritisation, and `EnclosingBracket` tells an editor which closing bracket should commit the open list.
- **`APIInfo`** — extracts type/member metadata and XML-doc for signature help and hover info.
