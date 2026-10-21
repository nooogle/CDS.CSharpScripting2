# CLAUDE.md

`CDS.CSharpScript2.Avalonia.UITests` runs the real Avalonia sample window under Avalonia.Headless (Skia rendering, simulated keyboard/mouse), so it works on the Mac and in CI with no display.

- Write tests through `HeadlessTestSession.RunAsync` and `SampleWindowDriver`; wait with `WaitUntilAsync`, never fixed sleeps.
- Two traps it already works around:
  - Passing a `Func<Task>` straight to `HeadlessUnitTestSession.Dispatch` binds the wrong overload and lets every test "pass" without awaiting it.
  - The synchronous `Dispose()` of the session hangs the test host — use `DisposeAsync()`.
