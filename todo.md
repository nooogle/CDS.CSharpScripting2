

dark mode support for scintilla editor
plan:
- [x] pull Coloriser's hardcoded per-classification colors out into a theme abstraction (EditorTheme: background, default foreground, per-classification overrides, caret line, selection, brace highlight, fold margin) so light/dark are just two instances instead of two codepaths
  - EditorTheme.cs (CDS.CSharpScript2/Classification) - positional record, immutable, two static presets
  - Coloriser now takes an EditorTheme (defaults to Light for back-compat with RTFEditor's parameterless `new Coloriser()`)
- [x] ship built-in Light and Dark presets (EditorTheme.Light / EditorTheme.Dark)
- [x] detect OS theme the way most WinForms/native apps do: HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme (same approach VS Code/Windows Terminal use), plus SystemEvents.UserPreferenceChanged to catch a live toggle while running
  - OsThemeWatcher.cs (CDS.CSharpScript2.ScintillaEditor) - IDisposable, static ReadIsDarkThemeActive() for a one-shot read, instance + ThemeChanged event for live watching. Lives in the ScintillaEditor project (not core) since it needs Microsoft.Win32.Registry + SystemEvents, both Windows-only/WinForms-only
  - BasicDemo now has a Theme group box (Light/Dark/System radio buttons) wired to it - System creates/disposes an OsThemeWatcher and follows live toggles; Light/Dark stop following and set the editor's Theme directly
  - smoke-tested end-to-end by driving the real compiled BasicDemo (reflection-set the radio buttons, screenshotted): System correctly picked up the OS's actual light theme, Dark/Light switched the editor's colors correctly, switching back to System re-read the OS theme correctly
- [x] expose an explicit Theme property on ScintillaScriptEditor - host app opts in by assigning EditorTheme.Light/.Dark, no auto-follow
  - deliberately NOT added to IScriptEditor - RTFEditor doesn't implement theming yet and VirtualScriptEditor (headless, used in tests) would need a no-op stub for no benefit
- [x] re-theme everything currently hardcoded: Style.Default fore/back, CaretLineBackColor, BraceLight/BraceBad, error/warning indicator colors, fold margin marker colors, AutocompleteListSelectedBackColor
- [x] selection colors - Scintilla had no explicit selection colors set before (used its own default); EditorTheme now sets SelectionForeground/SelectionBackground explicitly for both themes
- RTFEditor has none of this either - still out of scope, Coloriser's parameterless ctor keeps it on the Light palette unchanged
- [x] follow-up: form strip (buttons/theme picker) and the RTF output panel were still white in dark mode
  - FormBasicDemo.ApplyTheme now also sets the form's own BackColor + groupBoxTheme.ForeColor (cascades to the radio labels)
  - RTFOutputPanel got the same Theme property/pattern as ScintillaScriptEditor
- [x] follow-up: fold margin (gutter) background stayed white in dark mode - Scintilla renders the fold margin's own background from a separate SetFoldMarginColor/SetFoldMarginHighlightColor pair, not from Style.Default - now themed in ConfigureFoldMarkers

gutter markers for errors/warnings, respecting collapsed regions
- [x] margin 1 (previously reserved, unused, Mask=0) now shows a marker dot per line with an error/warning - red circle for errors, orange for warnings (EditorTheme.ErrorIndicatorForeColor/WarningIndicatorForeColor, now reused for both the squiggle and the gutter dot; light theme's warning color changed from Green to DarkOrange to match)
- [x] diagnostics hidden inside a collapsed fold are aggregated onto the nearest visible ancestor header line (worst severity wins) via ApplyDiagnosticMarkersToEditor + GetNearestVisibleLine (walks Line.FoldParent until Line.Visible)
- research finding (confirmed live, not guessed): UpdateUI does NOT fire after a programmatic FoldAll()/ToggleFold(), and MarginClick is suppressed entirely for fold-margin clicks under AutomaticFold.Click - neither hook works
- fix: dropped AutomaticFold.Click (kept .Show), added a scintilla_MarginClick handler that does the fold toggle itself (Line.ToggleFold()) then repositions markers; ExpandAllFolds/CollapseAllFolds/SetCollapsedFoldLines also recompute markers directly; UpdateUI hook kept as a backstop for AutomaticFold.Show auto-revealing hidden lines
- smoke-tested end-to-end against the real compiled BasicDemo: error+warning on adjacent lines inside a foldable block showed red+orange dots individually when expanded, collapsing aggregated to a single red dot (error wins) on the fold header line
- considered a VS-style scrollbar error/warning ribbon instead - confirmed via reflection Scintilla has no scrollbar-annotation API at all (VS's is custom WPF chrome); would need a fully custom-drawn scrollbar control, decided that was too large - gutter markers chosen instead

FlaUI-based UI tests for the Sample project / BasicDemo
- [x] new UITests project (net10.0-windows only, MSTest MTP style per CLAUDE.md, packages: MSTest, Microsoft.NET.Test.Sdk, Microsoft.Testing.Extensions.TrxReport/CodeCoverage, AwesomeAssertions, FlaUI.Core, FlaUI.UIA3), added to CDS.CSharpScripting2.slnx
- [x] tests launch the Sample app with a `--demo=basic` CLI switch (Program.cs) that opens FormBasicDemo directly, bypassing FormMain's docking-tree shell/demo picker (user's call - avoids depending on whether the third-party CDS.WinFormsMenus tree control is UIA-automatable)
- [x] confirmed live: WinForms controls' AutomationId resolves from Control.Name with no extra wiring needed - FlaUI found scintillaScriptEditor/rbThemeDark/rbThemeLight by ByAutomationId("...") out of the box
- [x] StaThreadRunner.cs - runs FlaUI-driven test bodies on a dedicated STA thread (UIA COM interop needs one, MSTest doesn't guarantee one) and rethrows the original exception via ExceptionDispatchInfo so MSTest still reports the real assertion failure
- [x] UT_BasicDemoTheme.cs - first test: launches the real app out-of-process, clicks the Dark/Light radio buttons via FlaUI, captures the editor element (FlaUI's Capture.Element) and samples a background pixel to confirm it actually recolors on screen, not just that the property got set. Needed ~1s settle time after each click before capturing - 500ms was flaky, 1s was reliably fine
- run via `dotnet run --project UITests` (MTP, not `dotnet test`, per CLAUDE.md)
- only one test so far - more coverage (fold/collapse, diagnostics markers, System theme following) would be natural next additions

