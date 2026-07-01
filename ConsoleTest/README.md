# ConsoleTest

A console application that serves three overlapping purposes for the `CDS.CSharpScript2` library:

1. **Demos** — runnable examples that show how to use the public API correctly.
2. **Functional testing** — a human-in-the-loop harness for verifying behaviour that automated tests don't cover well (e.g. timing, UI feel, real assembly loading).
3. **Library development sandbox** — a scratch space for R&D on new Roslyn features before promoting them into the library proper.

It is **not** a replacement for the automated test suite in `UnitTests/`. Those tests are deterministic and run in CI; this app is for eyes-on verification and experimentation.

## Running

```powershell
dotnet run --project ConsoleTest/ConsoleTest.csproj
```

A Spectre.Console menu lets you navigate between demos with arrow keys. Each demo runs and then waits for a keypress before returning to the menu.

## Demos

| Demo | What it shows |
|---|---|
| **Basic function** | Compile and run a script that contains its own inputs; read the return value. |
| **Shared data** | Pass a host object into the script as global state; run the same compiled script multiple times without recompiling. |
| **MathNet demo** | Give the script access to an external NuGet assembly (`MathNet.Numerics`); shows how to add assembly references to the `ScriptEnvironment`. |
| **OpenCvSharp demo** | Same pattern as MathNet but with a native-backed library; also exercises namespace injection and shared data with complex types. |
| **Code completion → Built-in demos** | Drives `ScriptAnalyser.GetCompletionsAsync` at various cursor positions and prints the top suggestions; useful for checking completion quality after engine changes. |
| **Code completion → User entered script** | Stub — intended as an interactive completion explorer. Currently unimplemented. |
| **XML documentation** | Drives `ScriptAnalyser.GetAPIInfoAsync` for `Math.Pow(` and `Console.WriteLine`; verifies that type/member metadata and XML-doc summaries are extracted correctly. |
| **Environment information** | Prints runtime environment details (from `TestUtils.RuntimeEnvironmentInfo`); useful on new machines or after SDK upgrades to confirm the expected runtime is being used. |

All demos use `TimedConsoleLogger`, which stamps each step with total elapsed time and the delta since the previous step. This makes it easy to spot regressions in compilation or startup latency.

## Adding a new demo

1. Create a class in `Demos/` (or a sub-folder if it needs its own menu).
2. Give it `public static string Name`, `public static string Description`, and `public static void Run()`.
3. Wire it into the `SelectionPrompt` in `Program.cs` (or into a sub-menu's `SelectionPrompt` if it belongs to a group).

The menu is a simple `while (true)` loop around `AnsiConsole.Prompt`; no framework to learn.

## R&D files

**`CompletionsRnD.cs`** is a scratchpad for experimenting with the Roslyn APIs directly — bypassing the library's abstractions to test `AdhocWorkspace`, `SemanticModel`, and `SymbolInfo` at the metal. It is not wired into any menu. To run it, call `CompletionsRnD.Run()` temporarily from `Program.Main` and remove it when done.

**`ScriptSamples.cs`** holds a set of canned scripts (`S1`–`S5`) with a specific cursor position each. They were used during development of the completion and API-info features to test against fixed, reproducible inputs. Add new samples here when debugging a specific completion or hover scenario.

## Project references

| Reference | Why |
|---|---|
| `CDS.CSharpScript2` | The library under development |
| `TestUtils` | Shared test utilities (`RuntimeEnvironmentInfo`, etc.) |
| `MathNet.Numerics` | Referenced by the MathNet demo script at runtime |
| `OpenCvSharp4.Windows` | Referenced by the OpenCvSharp demo script at runtime |
| `Spectre.Console` | Menu rendering |
