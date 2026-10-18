using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using AwesomeAssertions;
using CDS.CSharpScript2.Classification;
using CDS.CSharpScript2.Editors;

namespace CDS.CSharpScript2.Avalonia.UITests;

[TestClass]
public class UT_AvaloniaScriptEditor
{
    /// <summary>
    /// Verifies that the sample's starting script analyses cleanly.
    /// </summary>
    [TestMethod]
    public Task InitialScript_HasNoProblems()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = new SampleWindowDriver();

            await driver.WaitForInitialAnalysisAsync();

            driver.Status.Text.Should().Be("No problems");
            driver.Editor.HasErrors.Should().BeFalse();
        });
    }

    /// <summary>
    /// Verifies that typing an error raises DiagnosticsUpdated with that error and updates the status bar.
    /// </summary>
    [TestMethod]
    public Task TypingAnError_RaisesDiagnosticsUpdated_AndShowsInStatus()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = new SampleWindowDriver();
            await driver.WaitForInitialAnalysisAsync();

            DiagnosticsUpdatedEventArgs? latest = null;
            driver.Editor.DiagnosticsUpdated += (_, e) => latest = e;

            driver.TypeAtEnd(" + missingSymbol");

            await driver.WaitUntilAsync(() => latest?.HasErrors == true, "an error diagnostic");

            driver.Editor.Script.Should().EndWith(" + missingSymbol");
            latest!.Diagnostics.Should().Contain(d => d.Id == "CS0103");
            driver.Status.Text.Should().Be("1 error(s), 0 warning(s)");
        });
    }

    /// <summary>
    /// Verifies that hovering over a squiggled identifier opens a tooltip carrying the diagnostic.
    /// </summary>
    [TestMethod]
    public Task HoveringOverAnError_ShowsDiagnosticTooltip()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = new SampleWindowDriver();
            await driver.WaitForInitialAnalysisAsync();

            driver.Editor.Script = "var total = missingSymbol;";
            await driver.WaitUntilAsync(() => driver.Editor.HasErrors, "an error diagnostic");

            driver.HoverOverOffset(driver.Editor.Script.IndexOf("missingSymbol", StringComparison.Ordinal) + 2);

            await driver.WaitUntilAsync(() => ToolTip.GetIsOpen(driver.TextEditor), "the diagnostic tooltip to open");

            ToolTip.GetTip(driver.TextEditor).Should().BeOfType<string>()
                .Which.Should().Contain("CS0103").And.Contain("missingSymbol");
        });
    }

    /// <summary>
    /// Verifies that pressing F5 compiles and runs the script, capturing console output and the result.
    /// </summary>
    [TestMethod]
    public Task PressingF5_RunsScript_AndWritesOutput()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = new SampleWindowDriver();
            await driver.WaitForInitialAnalysisAsync();

            driver.TextEditor.TextArea.Focus();
            driver.PressKey(Key.F5, PhysicalKey.F5);

            await driver.WaitUntilAsync(() => driver.Output.Text?.Contains("Finished.") == true, "the run to finish");

            driver.Output.Text.Should().Contain("Squares: 1, 4, 9, 16, 25").And.Contain("Result: 55");
        });
    }

    /// <summary>
    /// Verifies that running a script with a compile error reports the error instead of running.
    /// </summary>
    [TestMethod]
    public Task PressingF5_WithCompileError_ReportsError()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = new SampleWindowDriver();
            await driver.WaitForInitialAnalysisAsync();

            driver.Editor.Script = "missingSymbol";
            driver.TextEditor.TextArea.Focus();
            driver.PressKey(Key.F5, PhysicalKey.F5);

            await driver.WaitUntilAsync(() => driver.Output.Text?.Contains("CS0103") == true, "the compile error");

            driver.Output.Text.Should().StartWith("Compilation failed:").And.NotContain("Finished.");
        });
    }

    /// <summary>
    /// Verifies that switching the app to dark mode switches the editor to the dark theme.
    /// </summary>
    [TestMethod]
    public Task SwitchingAppToDark_AppliesDarkEditorTheme()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = new SampleWindowDriver();
            var app = Application.Current!;

            try
            {
                app.RequestedThemeVariant = ThemeVariant.Dark;

                await driver.WaitUntilAsync(
                    () => driver.Editor.EffectiveTheme == EditorTheme.Dark,
                    "the editor to pick up the dark theme");

                var background = driver.TextEditor.Background.Should().BeAssignableTo<ISolidColorBrush>().Subject;
                var expected = EditorTheme.Dark.Background;
                background.Color.Should().Be(Color.FromArgb(expected.A, expected.R, expected.G, expected.B));
            }
            finally
            {
                app.RequestedThemeVariant = ThemeVariant.Default;
            }
        });
    }

    /// <summary>
    /// Verifies that an explicit ColorTheme overrides the app's theme variant.
    /// </summary>
    [TestMethod]
    public Task ExplicitColorTheme_OverridesAppThemeVariant()
    {
        return HeadlessTestSession.RunAsync(() =>
        {
            using var driver = new SampleWindowDriver();

            driver.Editor.ColorTheme = EditorTheme.Dark;

            driver.Editor.EffectiveTheme.Should().BeSameAs(EditorTheme.Dark);
            return Task.CompletedTask;
        });
    }
}
