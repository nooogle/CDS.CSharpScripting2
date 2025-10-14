# GitHub Copilot Coding Guidelines

These concise rules align with current Microsoft/.NET coding conventions for C# (targeting .NET 8).

## 1. Formatting & Style
- Use Allman braces (opening brace on a new line).
- Prefer file-scoped namespaces (`namespace X;`).
- Use `var` when the type is obvious from the right side; otherwise use the explicit type.
- Enable nullable reference types and annotate accordingly.
- Use expression-bodied members for simple members when it improves readability.
- Prefer single-line using statements.

## 2. Naming
- PascalCase: public/protected types, members, methods, properties, constants, and enums.
- Private fields: `_camelCase` (use `s_camelCase` for `static` and `t_camelCase` for `[ThreadStatic]`).
- Locals and parameters: `camelCase`.
- Interfaces: `I` prefix (e.g., `IRepository`).
- Generic type parameters: `T`, `TKey`, `TValue`, etc.
- Events: event handlers follow `(object sender, EventArgs e)`; methods that raise events use `On<EventName>()`.

## 3. Documentation & Comments
- XML-doc all public APIs (types, properties, and methods). Keep comments meaningful and non-boilerplate.
- Add brief inline comments only for non-obvious logic or business rules.

## 4. Exceptions
- Validate inputs with guard clauses; throw specific exceptions (`ArgumentNullException`, `ArgumentOutOfRangeException`, etc.).
- Use `throw;` to rethrow without losing the stack trace.
- Do not swallow exceptions; include helpful messages.

## 5. Async
- Use `async`/`await`; methods returning tasks end with `Async`.
- Avoid `async void` except for event handlers.
- Include `CancellationToken` for long-running or I/O-bound operations.
- In library code, use `ConfigureAwait(false)`.

## 6. Organization
- One public type per file; filename matches the type.
- Organize namespaces to match folder structure.
- Group and sort `using` directives alphabetically.

## 7. LINQ & Collections
- Prefer LINQ for readability; method syntax for common operations.
- Be aware of deferred execution and allocate appropriately (`IEnumerable<T>`, `IReadOnlyList<T>`, etc.).

## 8. Testing
- Use MSTest with FluentAssertions.
- Follow Arrange-Act-Assert; use descriptive test names and `[TestCategory]`.
- Test positive and negative scenarios.

## 9. Patterns & Best Practices
- Resource management: `using` for disposables; implement `IDisposable`/`IAsyncDisposable` correctly when needed.
- Dependency management: prefer DI and constructor injection; program to interfaces.
- Method design: single responsibility; avoid default parameter values in public APIs prefer overloads.
- Performance: use `StringBuilder` for many concatenations; consider `Span<T>`/`Memory<T>` in perf-critical paths.

## 10. Security & Validation
- Validate inputs; avoid exposing sensitive data in exceptions.
- Use parameterized queries and appropriate encodings for I/O and persistence.

## 11. Modern C# Features
- Use pattern matching and switch expressions when clearer.
- Consider records for immutable models and init-only properties.
- Use primary constructors and collection expressions where beneficial.
- Use raw string literals for multi-line strings.
