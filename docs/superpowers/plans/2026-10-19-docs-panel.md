# Docs Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An F1 docs panel for the Avalonia editor: shared core builds documentation pages and holds the panel's behaviour; a native Avalonia control renders them; the sample shows it in a fixed right-hand column.

**Architecture:** A new core namespace `CDS.CSharpScript2.Documentation` turns the symbol at a position (or a link target) into a UI-neutral `DocPage` made of text runs and links, and a headless `DocsPanelController` holds the Closed/Following/Browsing states and history. `AvaloniaScriptEditor` raises a help-requested event on F1 and exposes caret movement; a public `DocsPanel` control drives the controller and renders pages; the host decides where the panel lives.

**Tech Stack:** C#, Roslyn 5.9 (`SymbolFinder`, `DocumentationCommentId`, `SymbolDisplay`), Avalonia 12 + AvaloniaEdit 12, MSTest + AwesomeAssertions, Avalonia.Headless.

**Spec:** `docs/superpowers/specs/2026-10-19-docs-panel-design.md`

## Global Constraints

- `CDS.CSharpScript2` multi-targets `net48` + `net10.0`; every core change must build on both with no warnings: `dotnet build CDS.CSharpScript2/CDS.CSharpScript2.csproj`.
- Follow `CLAUDE.md`: Allman braces, file-scoped namespaces, nullable annotations, one public type per file named after it, XML docs on every public type and member, no default parameters in public APIs (use overloads), `Async` suffix with a `CancellationToken` overload for core query methods, `ConfigureAwait(false)` in core library code (exception: `DocsPanelController`, see Task 5).
- The library never decides where the panel lives and never shows or hides it; it raises events.
- No web view, HTML, or new package dependencies.
- Leave `FormAPIInfo`, `GetAPIInfo`, `DocumentationParser`, `IScriptEditor`, `VirtualScriptEditor` and the Scintilla editor unchanged.
- UI tests go through `HeadlessTestSession.RunAsync` and `SampleWindowDriver`, wait with `WaitUntilAsync`, never fixed sleeps.
- Unit tests run on the Mac with `-f net10.0-windows`. Core internals are not visible to `UnitTests`; test through public APIs.
- Commits only when Jon asks. Each task ends with a checkpoint at which both full suites pass:
  `dotnet test UnitTests/UnitTests.csproj -f net10.0-windows` and `dotnet test CDS.CSharpScript2.Avalonia.UITests`.

## Review Focus

- A link from the script's own XML docs (`<see cref="Math.Sqrt(double)"/>`) and links to script-declared symbols must resolve after the script is edited — pinned in Task 4.
- A `<see cref>` that does not resolve (`!:` prefix, or a type not referenced) must render as plain text, never a dead link or a crash — pinned in Task 4.
- Very large types (`string`) must build a page promptly and the panel must scroll rather than grow the window — pinned in Tasks 2 and 7.
- Changing the editor's `Environment` while the panel is open replaces its `EditorManager`; the panel must keep working against the new one — pinned in Task 7.
- Fast caret movement must never let a slow, older page overwrite a newer one — pinned in Task 5.

---

### Task 1: Page model and method pages

**Files:**
- Create in `CDS.CSharpScript2/Documentation/`: `DocRunKind.cs`, `DocRun.cs`, `DocTarget.cs`, `DocNamedItem.cs`, `DocOverload.cs`, `DocMember.cs`, `DocMemberGroup.cs`, `DocPage.cs`, `DocRunExtensions.cs`, `DocCommentParser.cs` (internal), `DocPageBuilder.cs` (internal)
- Modify: `CDS.CSharpScript2/ScriptAnalyser.cs` (add `GetDocPageAsync(int)` overloads)
- Modify: `CDS.CSharpScript2/CLAUDE.md` (add a `Documentation` bullet to the sub-namespace list)
- Test: `UnitTests/UT_DocPages.cs`

**Interfaces:**
- Produces (all public, namespace `CDS.CSharpScript2.Documentation`):
  - `enum DocRunKind { Text, Code, Link }`
  - `sealed record DocRun(string Text, DocRunKind Kind, DocTarget? Target)`
  - `sealed record DocTarget(string DocumentationId)` — a Roslyn documentation comment ID.
  - `sealed record DocNamedItem(IReadOnlyList<DocRun> Name, IReadOnlyList<DocRun> Description)` — parameters (name as one `Code` run) and exceptions (name as one `Link` run).
  - `sealed record DocOverload(string Label, DocTarget Target, bool IsCurrent)`
  - `sealed record DocMember(string Name, int OverloadCount, DocTarget Target)`
  - `sealed record DocMemberGroup(string Heading, IReadOnlyList<DocMember> Members)`
  - `sealed record DocPage` with init-only properties, lists defaulting to empty: `DocTarget? Target` (null for variables/parameters without an ID), `IReadOnlyList<DocRun> KindLine`, `string Title`, `string Signature`, `IReadOnlyList<DocRun> Summary`, `IReadOnlyList<DocNamedItem> Parameters`, `IReadOnlyList<DocRun> Returns`, `IReadOnlyList<DocNamedItem> Exceptions`, `IReadOnlyList<DocRun> Remarks`, `IReadOnlyList<DocOverload> Overloads`, `IReadOnlyList<DocMemberGroup> MemberGroups`, `bool IsUnavailable`; plus `static DocPage Unavailable(DocTarget target)`.
  - `static class DocRunExtensions { static string ToPlainText(this IReadOnlyList<DocRun> runs); }` — concatenated run text with whitespace runs collapsed to single spaces and trimmed.
  - `ScriptAnalyser.GetDocPageAsync(int position)` and `GetDocPageAsync(int position, CancellationToken ct)` → `Task<DocPage?>`.

- [ ] **Step 1: Write the failing tests** in `UnitTests/UT_DocPages.cs` (helper `GetPageAsync(script, marker)` asks one character into `marker`, like `UT_QuickInfo`), with `const string RoundScript = "var price = 19.987; var shown = Math.Round(price, 2);"`:
  - `MethodPage_FromCall_DescribesTheOverloadCalled`: `Title == "Math.Round"`; `Signature == "public static double Round(double value, int digits)"`; `KindLine.ToPlainText() == "Method · Math"` and the `Math` run is a `Link` with target `T:System.Math`; `Summary.ToPlainText()` starts with `"Rounds a double-precision floating-point value to a specified number of fractional digits"`.
  - `MethodPage_ListsParametersReturnsAndExceptions`: two parameters named `value`, `digits` with descriptions `"A double-precision floating-point number to be rounded."` and `"The number of fractional digits in the return value."`; `Returns.ToPlainText() == "The number nearest to value that contains a number of fractional digits equal to digits."` with `value` and `digits` as `Code` runs; one exception whose name run is a `Link` labelled `ArgumentOutOfRangeException` targeting `T:System.ArgumentOutOfRangeException`, description `"digits is less than 0 or greater than 15."`.
  - `MethodPage_ListsEveryOverloadWithTheCurrentOneMarked`: `Overloads.Count == 8`; exactly one `IsCurrent`, labelled `"Round(double, int)"`; every label starts with `"Round("`.
- [ ] **Step 2: Run** `dotnet test UnitTests/UnitTests.csproj -f net10.0-windows --filter "FullyQualifiedName~UT_DocPages"` — expect a compile failure (types missing).
- [ ] **Step 3: Implement.** `DocPageBuilder.BuildAsync(Document, int position, CancellationToken)` finds the symbol with `SymbolFinder.FindSymbolAtPositionAsync` (it returns the bound overload) and builds the page. `DocCommentParser` parses `GetDocumentationCommentXml(expandIncludes: true)` into runs for `summary`, `param`, `returns`, `exception`, `remarks`, handling `see cref` (Link, label = the resolved symbol's short display name), `see langword` (Code), `paramref`/`typeparamref`/`c` (Code), `para` (paragraph break), collapsing XML whitespace. Signature via `SymbolDisplay` with accessibility, modifiers, return type, parameter types and names, name-only type qualification and special-type keywords; overload labels are `Name(` + parameter types + `)`. Order overloads by declaration order. Kind line for members: `"{Method|Constructor|Property|Field|Event} · "` + containing-type link. Targets come from `DocumentationCommentId.CreateDeclarationId`.
- [ ] **Step 4: Run** the filter again — expect PASS; build the core for both targets with no warnings.
- [ ] **Step 5: Checkpoint** — both full suites pass.

### Task 2: Type pages

**Files:**
- Modify: `CDS.CSharpScript2/Documentation/DocPageBuilder.cs`
- Test: `UnitTests/UT_DocPages.cs`

**Interfaces:**
- Consumes: Task 1's model and `GetDocPageAsync(int)`.
- Produces: type pages with `MemberGroups` in the order Fields, Properties, Methods, Events, Constructors (empty groups omitted), members sorted by name (ordinal, case-insensitive), one entry per name with `OverloadCount`, `Target` = the first overload in declaration order.

- [ ] **Step 1: Write the failing tests:**
  - `TypePage_ForStaticClass`: on `Math` in `RoundScript`: `Title == "Math"`, `KindLine.ToPlainText() == "Static class · System"`, `Signature == "public static class Math"`, summary starts `"Provides constants and static methods"`.
  - `TypePage_GroupsDeclaredPublicMembers`: group headings are `["Fields", "Methods"]`; Fields names include `"E"`, `"PI"`, `"Tau"`; Methods contains `Round` with `OverloadCount == 8`; no member name is a property accessor or operator (no name starts with `"get_"`, `"set_"`, `"op_"`).
  - `TypePage_ForLargeType_BuildsAllGroups`: on `string` in `"string s = \"\";"`: `Title == "String"`, groups include `"Properties"`, `"Methods"`, `"Constructors"`, and the call completes inside 5 seconds.
- [ ] **Step 2: Run** the filter — expect FAIL.
- [ ] **Step 3: Implement** type pages. Kind word from the type: `Static class` (static class), `Class`, `Record`, `Struct`, `Record struct`, `Interface`, `Enum`, `Delegate`; then `" · "` + namespace as plain text. Members: declared, public, not implicitly declared; Methods are `MethodKind.Ordinary` only.
- [ ] **Step 4: Run** — expect PASS.
- [ ] **Step 5: Checkpoint.**

### Task 3: Variables, keywords, and nothing

**Files:**
- Modify: `CDS.CSharpScript2/Documentation/DocPageBuilder.cs`
- Test: `UnitTests/UT_DocPages.cs`

**Interfaces:**
- Produces: variable/parameter pages; `null` for namespaces, whitespace, comments.

- [ ] **Step 1: Write the failing tests:**
  - `VariablePage_ForScriptVariable`: on `price,` in `RoundScript`: `Title == "price"`, `Signature == "double price"`, `KindLine.ToPlainText() == "Variable · Double"` with `Double` a `Link` to `T:System.Double`, `Target` not null.
  - `ParameterPage_HasNoTarget`: in `"double Hyp(double a) => a;"` on the `a` before `) =>`: `KindLine.ToPlainText() == "Parameter · Double"`, `Signature == "double a"`, `Target` is null.
  - `KeywordForBuiltInType_OpensTheTypePage`: on `int` in `"int n = 1;"`: `Title == "Int32"`; on `var` in `RoundScript`: `Title == "Double"`.
  - `Namespace_ReturnsNull`: on `System` in `"System.Console.WriteLine();"` → null.
  - `BlankLine_ReturnsNull`: script `"var a = 1;\n\nvar b = 2;"`, position = the empty line (`IndexOf("\n\n") + 1`) → null. (A caret just after a word, e.g. `price|`, *should* describe that word — that is where the caret sits while typing — so do not test "whitespace touching a word" as null.)
  - `Comment_ReturnsNull`: `"// a comment here"` on `comment` → null.
- [ ] **Step 2: Run** — expect FAIL.
- [ ] **Step 3: Implement.** Fields whose containing type is the compilation's script class, and locals, are "Variable"; parameters are "Parameter". Signature is type + name. Namespaces and anything with no symbol → null.
- [ ] **Step 4: Run** — expect PASS.
- [ ] **Step 5: Checkpoint.**

### Task 4: Link targets and the `EditorManager` entry points

**Files:**
- Create: `CDS.CSharpScript2/Documentation/IDocPageSource.cs`
- Modify: `CDS.CSharpScript2/Documentation/DocPageBuilder.cs`, `CDS.CSharpScript2/ScriptAnalyser.cs`, `CDS.CSharpScript2/Editors/EditorManager.cs`
- Test: `UnitTests/UT_DocPages.cs`

**Interfaces:**
- Produces:
  - `ScriptAnalyser.GetDocPageAsync(DocTarget target)` and `(DocTarget target, CancellationToken ct)` → `Task<DocPage?>`: resolves with `DocumentationCommentId.GetFirstSymbolForDeclarationId` against the document's compilation; returns `DocPage.Unavailable(target)` when it no longer resolves. For a method target, the targeted overload is the one marked `IsCurrent`.
  - `public interface IDocPageSource { Task<DocPage?> GetDocPageAsync(int position, CancellationToken cancellationToken); Task<DocPage?> GetDocPageAsync(DocTarget target, CancellationToken cancellationToken); }`
  - `EditorManager : IDocPageSource` with all four `GetDocPageAsync` overloads, each `EnsureContext` + `RunAnalysisAsync` like `GetQuickInfoAsync`.

- [ ] **Step 1: Write the failing tests:**
  - `Target_ResolvesToItsPage`: `GetDocPageAsync(new DocTarget("T:System.ArgumentOutOfRangeException"))` → `Title == "ArgumentOutOfRangeException"`.
  - `ScriptDocs_SeeCrefBecomesALink`: script `"/// <summary>Uses <see cref=\"Math.Sqrt(double)\"/> on <paramref name=\"a\"/>.</summary>\ndouble Hyp(double a) => a;\nHyp(2);"`, page on `Hyp(2`: the summary has exactly one `Link` run, its `Target.DocumentationId == "M:System.Math.Sqrt(System.Double)"`, resolving that target gives `Title == "Math.Sqrt"`, and `a` is a `Code` run. (The link's label text is the builder's choice.)
  - `ScriptSymbolTarget_ResolvesAfterAnEdit`: take the `Hyp` page's `Target`, apply the script with an extra line appended, resolve the target → `Title == "Hyp"`; apply a script without `Hyp` → `IsUnavailable`.
  - `UnresolvableCref_IsPlainText`: script doc `<see cref="!:NoSuchThing"/>` → the summary contains no `Link` runs and its text contains `NoSuchThing`.
  - `EditorManager_GetDocPageAsync_ByPositionAndTarget` through `new EditorManager(ScriptEnvironment.Default)`.
- [ ] **Step 2: Run** — expect FAIL.
- [ ] **Step 3: Implement** the target overloads, `IDocPageSource`, and the `EditorManager` methods.
- [ ] **Step 4: Run** — expect PASS; core builds for both targets without warnings.
- [ ] **Step 5: Checkpoint.**

### Task 5: `DocsPanelController`

**Files:**
- Create: `CDS.CSharpScript2/Documentation/DocsPanelState.cs`, `CDS.CSharpScript2/Documentation/DocsPanelController.cs`
- Test: `UnitTests/UT_DocsPanelController.cs` (with a private fake `IDocPageSource` whose answers are `TaskCompletionSource`s the test completes, so ordering is controlled)

**Interfaces:**
- Consumes: `IDocPageSource`, `DocPage`, `DocTarget`.
- Produces:
  - `public enum DocsPanelState { Closed, Following, Browsing }`
  - `public sealed class DocsPanelController` — ctor `(IDocPageSource source)`; `DocsPanelState State`; `DocPage? CurrentPage`; `bool CanGoBack`; `bool CanGoForward`; `event EventHandler? Changed` (raised only when any of those four change); `Task ShowAtAsync(int position)`; `Task CaretMovedAsync(int position)`; `Task NavigateAsync(DocTarget target)`; `Task BackAsync()`; `Task ForwardAsync()`; `void Close()`.
  - Behaviour, from the spec: `ShowAtAsync` → Following (from any state), shows the page at the position; a null page keeps the current page (and from Closed leaves `CurrentPage` null, which the UI shows as the hint). `CaretMovedAsync` is ignored unless Following; null keeps the page; a page equal to the current one (same `Target`, `Title` and `Signature`) changes nothing. `NavigateAsync` → Browsing and pushes the page (dropping forward history). `BackAsync`/`ForwardAsync` move through history, re-resolving entries that have a `Target` (so a deleted symbol shows its Unavailable page) and reusing the stored page otherwise; they never change state back to Following. `Close` → Closed, clears page and history. Every request cancels the previous one; a result arriving after a newer request started is discarded.
  - The controller's awaits deliberately omit `ConfigureAwait(false)` so `Changed` is raised on the caller's (UI) thread; say so in a comment.

- [ ] **Step 1: Write the failing tests:** `ShowAt_FromClosed_EntersFollowing`; `ShowAt_OnNothing_FromClosed_HasNoPage`; `CaretMoved_WhileFollowing_UpdatesPage`; `CaretMoved_OnNothing_KeepsPage`; `CaretMoved_WithinSameSymbol_RaisesNothing`; `Navigate_EntersBrowsing_AndIgnoresCaret`; `BackAndForward_StepThroughHistory`; `BackToFirstPage_StaysBrowsing`; `ShowAt_WhileBrowsing_ReturnsToFollowing`; `Back_ToDeletedTarget_ShowsUnavailable`; `Close_ClearsHistory`; `SlowOlderRequest_DoesNotOverwriteNewer` (start a request, start a second, complete the second then the first: `CurrentPage` is the second's).
- [ ] **Step 2: Run** `--filter "FullyQualifiedName~UT_DocsPanelController"` — expect FAIL.
- [ ] **Step 3: Implement** `DocsPanelController`.
- [ ] **Step 4: Run** — expect PASS.
- [ ] **Step 5: Checkpoint.**

### Task 6: Editor hooks — F1, caret, and a current document

**Files:**
- Create: `CDS.CSharpScript2/Editors/HelpRequestedEventArgs.cs` (core, so the WinForms port can share it)
- Create: `CDS.CSharpScript2.AvaloniaEditor/AvaloniaScriptEditor.Help.cs`, `CDS.CSharpScript2.AvaloniaEditor/EditorDocPageSource.cs` (internal)
- Modify: `CDS.CSharpScript2.AvaloniaEditor/AvaloniaScriptEditor.cs` (initialise/detach the new partial)
- Test: `CDS.CSharpScript2.Avalonia.UITests/UT_AvaloniaScriptEditorHelp.cs`

**Interfaces:**
- Produces:
  - `public sealed class HelpRequestedEventArgs : EventArgs { public HelpRequestedEventArgs(int position); public int Position { get; } }`
  - On `AvaloniaScriptEditor`: `public event EventHandler<HelpRequestedEventArgs>? HelpRequested`; `public void RequestHelp()` (raises it at the caret); F1 (no modifiers) in the text area calls `RequestHelp()` and is handled; `public int CaretOffset { get; }`; `public event EventHandler? CaretMoved` (from AvaloniaEdit's caret `PositionChanged`).
  - `internal sealed class EditorDocPageSource : IDocPageSource` wrapping an `AvaloniaScriptEditor`: before each request it brings the manager's document up to date (the same `_managerDocumentVersion` check the hover uses — expose it as an internal `Task EnsureManagerDocumentCurrentAsync(CancellationToken)` on the editor) and reads `editor.Manager` per call, returning null when there is none.
- [ ] **Step 1: Write the failing tests:** `PressingF1_RaisesHelpRequestedAtCaret` (press `Key.F1, PhysicalKey.F1`; the event's `Position == driver.TextEditor.CaretOffset`); `MovingCaret_RaisesCaretMoved`; `DocPageSource_SeesUnanalysedEdits` (set the script, immediately ask the internal source for the page at a symbol typed in that edit → non-null).
- [ ] **Step 2: Run** `dotnet test CDS.CSharpScript2.Avalonia.UITests --filter "FullyQualifiedName~UT_AvaloniaScriptEditorHelp"` — expect FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** — expect PASS.
- [ ] **Step 5: Checkpoint.**

### Task 7: `DocsPanel` control and the sample's right-hand column

**Files:**
- Create in `CDS.CSharpScript2.AvaloniaEditor/`: `DocsPanel.cs` (public), `DocPageRenderer.cs` (internal: `DocPage` → controls), `DocLink.cs` (internal `TextBlock` subclass with a `DocTarget Target`, hand cursor, left-click invokes navigation)
- Modify: `CDS.CSharpScript2.Avalonia.Sample/MainWindow.axaml` and `.axaml.cs`, `CDS.CSharpScript2.Avalonia.UITests/SampleWindowDriver.cs`, `CLAUDE.md` (Avalonia editor row: add "docs panel")
- Test: `CDS.CSharpScript2.Avalonia.UITests/UT_DocsPanel.cs`

**Interfaces:**
- Consumes: Tasks 5–6.
- Produces:
  - `public sealed class DocsPanel : UserControl` — `AvaloniaScriptEditor? Editor { get; set; }`; `event EventHandler? ShowRequested`; `event EventHandler? CloseRequested`; `internal DocsPanelController Controller`. On the editor's `HelpRequested` it calls `ShowAtAsync(e.Position)` and raises `ShowRequested`; on `CaretMoved` it restarts a 300 ms `DispatcherTimer` whose tick calls `CaretMovedAsync(Editor.CaretOffset)`; the close button and Escape (panel focused) call `Close()` and raise `CloseRequested`.
  - Header: Back, Forward (disabled per `CanGoBack`/`CanGoForward`), a state label exactly `"● Following caret"` or `"Browsing"`, in Browsing a `"Follow caret (F1)"` button calling `ShowAtAsync(Editor.CaretOffset)`, and a close button `"✕"`.
  - Body in a `ScrollViewer`: the sections in the spec's order with headings `PARAMETERS`, `RETURNS`, `EXCEPTIONS`, `REMARKS`, `OVERLOADS (N)` and the member group headings upper-cased; current overload highlighted with `"← this call"` when Following; the hint `"Put the caret on a symbol and press F1"` when `CurrentPage` is null; `"This item is no longer available."` for Unavailable pages. Signature and code runs in the editor's font. Colours from `Editor.EffectiveTheme`, re-rendered on `ActualThemeVariantChanged`, matching the hover popup.
  - Sample: wrap the existing content grid in `Grid ColumnDefinitions="*,Auto"`; `DocsPanel x:Name="DocsPanel" Grid.Column="1" Width="340" IsVisible="False"`; in code `DocsPanel.Editor = ScriptEditor`, `ShowRequested` → `IsVisible = true`, `CloseRequested` → `IsVisible = false` (comment that this is the host's choice); a View menu item `"_Docs"` with `InputGesture="F1"` (display only, no `HotKey`) whose click calls `ScriptEditor.RequestHelp()`.
  - Driver: `DocsPanel DocsPanel`, `bool IsDocsPanelVisible`, `string? DocsTitle` (`Controller.CurrentPage?.Title`), `DocsPanelState DocsState`, `void ClickDocLink(string text)` (finds a visible `DocLink` with that text and clicks its centre), `void MoveCaretTo(int offset)`.
- [ ] **Step 1: Write the failing tests:** `F1_ShowsPanelWithMethodPage` (caret in `Round`; panel visible; `DocsTitle == "Math.Round"`; state Following); `MovingCaret_UpdatesPage` (move into `Math` → `DocsTitle == "Math"`); `CloseButton_HidesPanel`; `ChangingEnvironment_WhileOpen_KeepsWorking` (set `Editor.Environment` again, move caret → page updates); `LongPage_Scrolls` (F1 on `string`: the panel's height stays within the window's).
- [ ] **Step 2: Run** `--filter "FullyQualifiedName~UT_DocsPanel"` — expect FAIL.
- [ ] **Step 3: Implement** `DocsPanel`, `DocPageRenderer`, `DocLink`, the sample wiring, and the driver helpers.
- [ ] **Step 4: Run** — expect PASS. Capture headless screenshots of the panel in light and dark (a throwaway test, deleted afterwards) and compare against the approved mockup.
- [ ] **Step 5: Checkpoint.**

### Task 8: Links, browsing, and history in the UI

**Files:**
- Modify: `CDS.CSharpScript2.AvaloniaEditor/DocsPanel.cs`, `DocPageRenderer.cs`
- Test: `CDS.CSharpScript2.Avalonia.UITests/UT_DocsPanel.cs`

**Interfaces:**
- Consumes: Task 7's panel and driver helpers.

- [ ] **Step 1: Write the failing tests:** `ClickingLink_BrowsesAndIgnoresCaret` (F1 on `Round`, `ClickDocLink("Math")` → `DocsTitle == "Math"`, `DocsState == Browsing`, header shows `"Browsing"`; move the caret into `price` → still `"Math"`); `F1_WhileBrowsing_ReturnsToFollowing`; `FollowCaretButton_ReturnsToFollowing`; `BackAndForwardButtons_StepThroughPages`; `ReopeningAfterClose_StartsWithEmptyHistory` (`CanGoBack` false).
- [ ] **Step 2: Run** — expect FAIL.
- [ ] **Step 3: Implement** link navigation and the header buttons.
- [ ] **Step 4: Run** — expect PASS, then both full suites, and the core build for both targets.
- [ ] **Step 5: Checkpoint** — report to Jon with the screenshots; commit only if he asks.
