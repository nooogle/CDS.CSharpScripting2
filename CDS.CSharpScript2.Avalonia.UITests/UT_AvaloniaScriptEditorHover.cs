using AwesomeAssertions;

namespace CDS.CSharpScript2.Avalonia.UITests;

[TestClass]
public class UT_AvaloniaScriptEditorHover
{
    private const string RoundScript = "var price = 19.987;\nvar shown = Math.Round(price, 2);";

    /// <summary>
    /// Verifies that resting the pointer on a method shows the overload called and its summary.
    /// </summary>
    [TestMethod]
    public Task HoveringOverMethod_ShowsSignatureAndSummary()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(RoundScript);

            driver.HoverOverOffset(RoundScript.IndexOf("Round", StringComparison.Ordinal) + 1);

            await driver.WaitUntilAsync(() => driver.HoverInfo is not null, "the hover popup to open");
            var info = driver.HoverInfo!;
            info.SignatureText.Should().Be("double Math.Round(double value, int digits) (+ 7 overloads)");
            info.SummaryText.Should().StartWith("Rounds a double-precision floating-point value");
            info.DiagnosticText.Should().BeEmpty();
        });
    }

    /// <summary>
    /// Verifies that hovering a symbol with an error shows the error above the symbol's info.
    /// </summary>
    [TestMethod]
    public Task HoveringOverSymbolWithError_ShowsErrorAndQuickInfo()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            const string script = "int whole = Math.PI;";
            using var driver = await StartAsync(script);
            await driver.WaitUntilAsync(() => driver.Editor.HasErrors, "the conversion error");

            driver.HoverOverOffset(script.IndexOf("PI", StringComparison.Ordinal) + 1);

            await driver.WaitUntilAsync(() => driver.HoverInfo is not null, "the hover popup to open");
            var info = driver.HoverInfo!;
            info.DiagnosticText.Should().Contain("CS0266");
            info.SignatureText.Should().Contain("Math.PI");
        });
    }

    /// <summary>
    /// Verifies that hovering where there is no symbol shows nothing.
    /// </summary>
    [TestMethod]
    public Task HoveringOverWhitespace_ShowsNothing()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(RoundScript);
            var completed = driver.Editor.CompletedHoverRequests;

            driver.HoverOverOffset(RoundScript.IndexOf(" = 19", StringComparison.Ordinal));

            await driver.WaitUntilAsync(
                () => driver.Editor.CompletedHoverRequests > completed,
                "the hover request to finish");
            driver.HoverInfo.Should().BeNull();
        });
    }

    /// <summary>
    /// Verifies that resting just past the end of a line does not describe the word it ends with.
    /// </summary>
    [TestMethod]
    public Task HoveringPastEndOfLine_ShowsNothing()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            const string script = "var price = Math.PI\n;";
            using var driver = await StartAsync(script);
            var completed = driver.Editor.CompletedHoverRequests;

            // The newline at the end of line 1: the pointer lands just past "PI".
            driver.HoverOverOffset(script.IndexOf('\n'));

            await driver.WaitUntilAsync(
                () => driver.Editor.CompletedHoverRequests > completed,
                "the hover request to finish");
            driver.HoverInfo.Should().BeNull();
        });
    }

    /// <summary>
    /// Verifies that moving the pointer away closes the popup.
    /// </summary>
    [TestMethod]
    public Task MovingPointerAway_ClosesPopup()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(RoundScript);
            driver.HoverOverOffset(RoundScript.IndexOf("Math", StringComparison.Ordinal) + 1);
            await driver.WaitUntilAsync(() => driver.HoverInfo is not null, "the hover popup to open");

            driver.MovePointerAway();

            await driver.WaitUntilAsync(() => driver.HoverInfo is null, "the hover popup to close");
        });
    }

    /// <summary>
    /// Verifies that typing closes the popup.
    /// </summary>
    [TestMethod]
    public Task Typing_ClosesPopup()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(RoundScript);
            driver.HoverOverOffset(RoundScript.IndexOf("Math", StringComparison.Ordinal) + 1);
            await driver.WaitUntilAsync(() => driver.HoverInfo is not null, "the hover popup to open");

            driver.Type(" ");

            driver.HoverInfo.Should().BeNull();
        });
    }

    private static async Task<SampleWindowDriver> StartAsync(string script)
    {
        var driver = new SampleWindowDriver();
        await driver.WaitForInitialAnalysisAsync();
        driver.SetScriptWithCaretAtEnd(script);
        return driver;
    }
}
