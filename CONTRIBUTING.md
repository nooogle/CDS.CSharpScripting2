# Contributing

## Build and test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test UnitTests/UnitTests.csproj --configuration Release
```

The full pipeline (clean → restore → build → test) is in `build.cake`, runnable via `./build.ps1`.

## Coding conventions

Follow the conventions in [`.github/copilot-instructions.md`](.github/copilot-instructions.md):

- Allman braces, file-scoped namespaces
- Nullable reference types enabled; annotate all public APIs
- `PascalCase` public members, `_camelCase` private fields, `s_camelCase` statics
- Async methods end in `Async`; include `CancellationToken` for I/O-bound operations
- No default parameters on public APIs — use overloads
- XML-doc all public types, properties, and methods
- One public type per file; filename matches the type name

## Pull requests

- Open an issue first for non-trivial changes.
- Keep PRs focused — one concern per PR.
- All tests must pass. Add tests for new behaviour.
- Public API changes require XML-doc updates.
