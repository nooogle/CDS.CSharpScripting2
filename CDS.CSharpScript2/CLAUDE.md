# CLAUDE.md

Guidance for working within `CDS.CSharpScript2` specifically.

### Sub-namespaces

- **`Classification`** — maps Roslyn classification spans to `ClassificationColorScheme` entries for syntax highlighting.
- **`CodeCompletion`** — wraps Roslyn's Completion API; `SingleLetterMatchSorter` applies smart prioritisation.
- **`APIInfo`** — extracts type/member metadata and XML-doc for signature help and hover info.
