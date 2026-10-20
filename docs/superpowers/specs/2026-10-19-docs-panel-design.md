# Docs panel — design

Date: 2026-10-19 · Status: draft for Jon's approval

## Intent

Script authors need somewhere to read the full documentation for whatever they are working
with, without leaving the editor. The editor already has three *light* views that appear next to
the caret or pointer: the completion description, the call tip, and the hover quick info. This adds
the *rich* view: a separate panel showing everything the XML docs say about a symbol, with links
to related symbols.

It replaces the provisional F1 `FormAPIInfo` window, which only proved that the core could extract
the information. This is "a simple scripting system": anyone who needs a powerful tool will use a
real IDE. The panel should be clear and useful, not a feature race with Visual Studio.

The Avalonia version is built first, in this repo, as groundwork for ManifoldSharp. Jon will port
it to WinForms/Scintilla later, so everything that is not drawing belongs in the shared core.

## Goals

- F1 opens the panel on the symbol at the caret.
- While open, the panel stays live and follows the caret.
- Links in the panel move to related symbols. Back and Forward step through what was visited.
- Shared core: page content, link resolution, and panel behaviour (following, browsing, history)
  live in `CDS.CSharpScript2`, so the Avalonia and WinForms panels behave identically and the
  behaviour is unit-testable without a UI.
- Separate, native UI implementations: an Avalonia panel now; a WinForms panel later.
- The host application decides where the panel lives.

## Non-goals

- The WinForms panel (later, by Jon, from this design).
- Removing `FormAPIInfo`, `GetAPIInfo`, or anything else the Scintilla editor uses today.
- Inherited members on type pages, source view, go-to-definition, search, or examples.
- Online documentation, or any web view or HTML rendering.
- Syntax colouring inside the panel beyond what is listed below.
- Errors in the panel: those stay in the hover and the Problems list.

## Anti-goals (would make it a failure even if it technically works)

- The library deciding where the panel goes, or adding a docking framework.
- The panel silently stopping updating with no visible reason.
- Flicker: the panel blanking or redrawing on every keystroke.
- A heavy new dependency (WebView2, a browser engine) in a packaged library.

## Constraints

- `CDS.CSharpScript2` is packaged and multi-targets `net48` + `net10.0`. New code must compile on
  both. Follow `CLAUDE.md` conventions: no default parameters in public APIs (use overloads),
  `Async` suffix and `CancellationToken` overloads, `ConfigureAwait(false)` in library code,
  XML docs on all public members, one public type per file, Allman braces, file-scoped namespaces.
- `CDS.CSharpScript2.AvaloniaEditor` is .NET 10 only and cross-platform. UI behaviour is verified
  with the headless Avalonia tests (`HeadlessTestSession.RunAsync`, `SampleWindowDriver`,
  `WaitUntilAsync`; never fixed sleeps). Keystroke automation does not work on Jon's Mac.
- Must work the same on macOS, Linux, and Windows.

## Decided by Jon

### Lifecycle

- The panel is **hidden** until the user asks for it. **F1** in the editor brings it up, showing
  the symbol at the caret.
- Once open it is **live**: it follows the caret until the user closes it.
- Two states while open:
  - **Following**, entered by F1. Shows whatever the caret is on and updates as the caret moves.
  - **Browsing**, entered as soon as the user clicks a link. Ignores the caret so the user can
    edit while reading. **F1 in the editor returns it to Following**, showing what is at the caret.
- Back and Forward step through visited pages. Going back to the first page does **not** resume
  Following; only F1 does.
- The header shows which state the panel is in ("● Following caret" or "Browsing"). In Browsing
  it also has a "Follow caret (F1)" button that does the same as F1.
- History lasts until the panel is closed. Reopening with F1 starts fresh.
- When the caret is on nothing (whitespace, a comment, a string), the panel keeps showing the
  last symbol rather than going blank.
- While typing, it updates after a short pause rather than on every keystroke.
- Closing: a close button in the header, and Escape while the panel has keyboard focus. Escape in
  the editor keeps its current meaning (completion list, call tip).

### Placement

- The library's panel is a plain control with no opinion about where it lives: a docking pane, a
  tab, a fixed column, or a floating window are all the host's choice.
- The library never shows or hides the panel itself. It raises events; the host decides.
- The Avalonia sample puts the panel in a **fixed right-hand column** beside the editor, hidden at
  first. That wiring lives only in the sample.

### Content (from the approved mockup)

Every page has the header described above, then the body.

**Method page** (also used for constructors and other member kinds, with sections omitted when
empty):

1. A kind line: member kind · containing type, as a link (e.g. "Method · Math").
2. The title: `Type.Member` (e.g. `Math.Round`).
3. The full signature, including modifiers (`public static double Round(double value, int digits)`).
   For an overloaded method reached from code, this is the overload the code actually calls.
4. The summary.
5. **Parameters**: each parameter name with its documentation.
6. **Returns**.
7. **Exceptions**: each exception type, as a link, with its condition.
8. **Overloads (N)**: every overload's short signature (`Round(double, int)`), each a link, with
   the one being shown highlighted ("← this call" when it came from the caret).

**Type page**:

1. A kind line: kind · namespace (e.g. "Static class · System").
2. The title: the type name.
3. The declaration signature (`public static class Math`).
4. The summary.
5. The type's members grouped by kind — Fields, Properties, Methods, Events, Constructors
   (groups omitted when empty) — each name a link, with an overload count when there is more than
   one (e.g. `Round (8)`).

Every reference to another symbol in the documentation text (XML-doc `<see cref>`, exception
types, containing type, overloads, members) is a link that moves the panel to that symbol's page.
`<paramref>` and `<typeparamref>` show the name in code style.

## Architecture

### Core (`CDS.CSharpScript2`)

New sub-namespace `CDS.CSharpScript2.Documentation` (add it to `CDS.CSharpScript2/CLAUDE.md`).

**Page model.** A UI-neutral description of one page, rendered by each UI with native controls.
Text is made of runs so both UIs can draw links and code style without parsing anything:

- `DocRun`: a piece of text plus its kind — plain, code (keywords, type and parameter names in a
  signature or a `<c>`/`<paramref>`), or link — and, for a link, its `DocTarget`.
- `DocTarget`: an opaque, value-equal link target. Implemented as the symbol's documentation
  comment ID (e.g. `M:System.Math.Round(System.Double,System.Int32)`), which is exactly what
  `<see cref>` attributes already contain and what Roslyn's `DocumentationCommentId` resolves
  against a compilation. Works for framework symbols and for symbols declared in the script.
- `DocPage`: the target it describes, the kind line, title, signature, summary, and the optional
  sections listed under **Content**, each as runs or lists of runs.

**Page builder.** Given a position, find the symbol actually bound there (for a method call, the
overload being called) and build its page. Given a `DocTarget`, resolve it against the current
compilation and build that page. Returns `null` when there is no symbol or the target no longer
resolves. Public entry points follow the existing pattern:

- `ScriptAnalyser.GetDocPageAsync(int position)` and `GetDocPageAsync(DocTarget target)`, each
  with a `CancellationToken` overload.
- `EditorManager.GetDocPageAsync(...)` mirroring them, running on the thread pool like the others.

XML docs are parsed into runs by a new parser that handles `<summary>`, `<param>`, `<returns>`,
`<exception cref>`, `<remarks>`, `<see cref>`, `<see langword>`, `<paramref>`, `<typeparamref>`,
`<c>`, and `<para>`. The existing `DocumentationParser` (used by `GetAPIInfo` and the Scintilla
editor) is left as it is; it drops `<see>`/`<paramref>` text, which is a separate fix for later.

**Panel controller.** A headless class that holds the panel's behaviour, so both UIs share it:

- State: `Closed`, `Following`, `Browsing`.
- `ShowAtAsync(position)` — what F1 calls. Opens the panel if closed, enters Following, shows
  the page at the caret. If there is no symbol there and nothing has been shown yet, the page is
  empty and the UI shows a short hint ("Put the caret on a symbol and press F1").
- `CaretMovedAsync(position)` — the UI calls this after its own debounce. Ignored unless
  Following. A position with no symbol keeps the current page.
- `NavigateAsync(target)` — a link click. Enters Browsing and pushes the page onto the history.
- `Back()`, `Forward()`, `CanGoBack`, `CanGoForward`.
- `Close()` — clears history and returns to Closed.
- A `PageChanged`/`StateChanged` event (shape left to the builder) that the UI renders from.
- A superseded request is cancelled and its result discarded; a slow page never replaces a newer
  one.

The controller is editor-agnostic: it talks to an `EditorManager` (or an equivalent page source,
for tests) and takes positions, not editor controls. It must be fully unit-testable without a UI.

### Avalonia (`CDS.CSharpScript2.AvaloniaEditor`)

- `AvaloniaScriptEditor` raises a public **help-requested** event on F1, carrying the caret
  offset, instead of doing anything itself. It also exposes caret movement (an event or the
  existing caret API) so the panel can follow it.
- A public **`DocsPanel`** control. The host connects it to an editor (`panel.Editor = editor`);
  the panel then listens for help requests and caret moves, drives the controller, and renders
  `DocPage`s with native controls (TextBlocks with inlines, clickable links, a scrolling body).
- The panel raises **show-requested** when a help request arrives and **close-requested** when
  the close button or Escape is used. The host shows and hides the panel in response. Hooking
  this up should be a few lines in the host, documented in the sample.
- It follows the connected editor's light/dark theme and matches the look of the existing popups.

### Avalonia sample

- A fixed right-hand column with the panel, hidden at start, shown on show-requested and hidden
  on close-requested.

## Tests

- **Core unit tests** (`UnitTests`, runnable on the Mac with `-f net10.0-windows`):
  - Method page for `Math.Round(price, 2)` from the caret: the `(double, int)` overload, summary,
    both parameters, returns, the `ArgumentOutOfRangeException` link, eight overloads with the
    current one marked.
  - Type page for `Math`: kind line, summary, members grouped with overload counts.
  - A `<see cref>` in doc text becomes a link whose target resolves to the right page.
  - A symbol declared in the script, with and without its own XML doc comment.
  - Whitespace returns `null`.
  - Controller state machine: F1 → Following; caret moves update; caret on nothing keeps the
    page; a link → Browsing and caret moves are ignored; Back/Forward; Back to the first page
    stays Browsing; F1 → Following; Close clears history; a slow superseded request is dropped.
- **Headless UI tests** (`CDS.CSharpScript2.Avalonia.UITests`), driving the sample:
  - F1 shows the panel with the method page.
  - Moving the caret updates it.
  - Clicking a link shows the linked page and the header says Browsing; moving the caret does not
    change it; F1 returns to Following.
  - Back and Forward.
  - The close button hides the panel; F1 brings it back with history cleared.
  - Light and dark themes apply.

## Left to the builder

- Exact type and member names beyond those given above, and the event signatures.
- How links are drawn and clicked in Avalonia.
- How the bound symbol is found at a position (the existing `SymbolLocator`, Roslyn's
  `SymbolFinder`, or the semantic model directly), as long as it picks the overload being called.
- Debounce length for following the caret (the editor's existing analysis delays are a guide).
- Visual details within the approved mockup: spacing, fonts, colours.

## Calls made by Claude (for Jon to check)

- **Remarks**: shown as a section after Exceptions when the docs have them (the mockup's example
  had none).
- **All** overloads and members are listed with the body scrolling; the mockup's "+ 3 more"
  truncation was only to fit the mockup.
- **Type pages list declared members only**, not inherited ones, and only those accessible to the
  script (public).
- **Types in the signature are not links**, matching the mockup; references in the doc text are.
- **The help-requested event lives on `AvaloniaScriptEditor` for now, not on `IScriptEditor`**,
  so the Scintilla editor and `VirtualScriptEditor` don't change until the WinForms port. Moving
  it onto the interface is part of that port.
- **F1 in the Scintilla editor keeps opening `FormAPIInfo`** until the WinForms port.
- **The sample adds a "Docs (F1)" item to its View menu**, since F1 on a Mac keyboard needs the
  fn key.
- **The panel column has a fixed width** in the sample (about 340 px), with no splitter.
- **No redraw when nothing changed**: if the caret moves within the symbol already shown, the
  controller raises nothing, so following the caret never flickers.
- **A member group link** on a type page (`Round (8)`) opens the first overload in declaration
  order; its Overloads section lists the rest.
- **Variables and parameters** (`price`, `n`) get a small page: kind line ("Variable" or
  "Parameter"), the signature (`double price`), and a link to the variable's type. Keywords for
  built-in types (`int`, `string`, and the type `var` stands for) open that type's page.
- **Namespaces** (F1 on `System`) have no page: the panel keeps what it was showing.
- **A target that no longer resolves** (e.g. Back to a function since deleted from the script)
  shows a short "no longer available" page in place of the old one.
