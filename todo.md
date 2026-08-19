

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
- next: OS theme detection + wiring (registry read + SystemEvents.UserPreferenceChanged + live re-apply), likely as a small helper class host apps opt into

