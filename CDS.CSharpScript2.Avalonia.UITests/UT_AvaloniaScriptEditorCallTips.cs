using Avalonia.Input;
using AwesomeAssertions;

namespace CDS.CSharpScript2.Avalonia.UITests;

[TestClass]
public class UT_AvaloniaScriptEditorCallTips
{
    /// <summary>
    /// Verifies that typing an opening parenthesis shows the signature with the first parameter
    /// highlighted, along with the method summary and that parameter's documentation.
    /// </summary>
    [TestMethod]
    public Task TypingOpenParen_ShowsSignatureWithFirstParameterActive()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Math.Pow");

            driver.Type("(");

            await driver.WaitForCallTipParameterAsync("double x");
            var tip = driver.CallTip!;
            tip.SignatureText.Should().Be("double Pow(double x, double y)");
            tip.DocumentationText.Should().Contain("raised to the specified power");
            tip.DocumentationText.Should().Contain("x: ");
        });
    }

    /// <summary>
    /// Verifies that typing a comma moves the highlight to the next parameter.
    /// </summary>
    [TestMethod]
    public Task TypingComma_HighlightsNextParameter()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Math.Pow");

            driver.Type("(");
            await driver.WaitForCallTipParameterAsync("double x");

            driver.Type("2,");

            await driver.WaitForCallTipParameterAsync("double y");
            driver.CallTip!.DocumentationText.Should().Contain("y: ");
        });
    }

    /// <summary>
    /// Verifies that a method with several overloads shows a counter and Down moves to the next.
    /// </summary>
    [TestMethod]
    public Task PressingDown_CyclesOverloads()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Console.WriteLine");

            driver.Type("(");
            await driver.WaitUntilAsync(
                () => driver.CallTip?.CurrentIndexText.StartsWith("1 of ", StringComparison.Ordinal) == true,
                "the call tip to show the first overload");
            var firstSignature = driver.CallTip!.SignatureText;

            driver.PressKey(Key.Down, PhysicalKey.ArrowDown);

            driver.CallTip!.CurrentIndexText.Should().StartWith("2 of ");
            driver.CallTip.SignatureText.Should().NotBe(firstSignature);
            driver.TextEditor.CaretOffset.Should().Be(driver.Editor.Script.Length,
                "Down should cycle overloads, not move the caret");
        });
    }

    /// <summary>
    /// Verifies that closing a nested call brings back the enclosing call's tip.
    /// </summary>
    [TestMethod]
    public Task TypingCloseParen_InNestedCall_RestoresOuterTip()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Math.Pow(Math.Abs");

            driver.Type("(");
            await driver.WaitUntilAsync(
                () => driver.CallTip?.SignatureText.Contains("Abs(", StringComparison.Ordinal) == true,
                "the Abs call tip");

            driver.Type("1)");

            await driver.WaitUntilAsync(
                () => driver.CallTip?.SignatureText == "double Pow(double x, double y)",
                "the outer Pow call tip to return");
            driver.CallTip!.ActiveParameterText.Should().Be("double x");
        });
    }

    /// <summary>
    /// Verifies that typing the closing parenthesis of an outermost call closes the tip.
    /// </summary>
    [TestMethod]
    public Task TypingCloseParen_OfOutermostCall_ClosesTip()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Math.Sqrt");

            driver.Type("(");
            await driver.WaitForCallTipParameterAsync("double d");

            driver.Type("2)");

            driver.Editor.IsCallTipOpen.Should().BeFalse();
        });
    }

    /// <summary>
    /// Verifies that Escape closes the tip.
    /// </summary>
    [TestMethod]
    public Task PressingEscape_ClosesTip()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Math.Sqrt");

            driver.Type("(");
            await driver.WaitForCallTipParameterAsync("double d");

            driver.PressKey(Key.Escape, PhysicalKey.Escape);

            driver.Editor.IsCallTipOpen.Should().BeFalse();
        });
    }

    /// <summary>
    /// Verifies that deleting the opening parenthesis closes the tip.
    /// </summary>
    [TestMethod]
    public Task BackspacingOverOpenParen_ClosesTip()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Math.Sqrt");

            driver.Type("(");
            await driver.WaitForCallTipParameterAsync("double d");

            driver.PressKey(Key.Back, PhysicalKey.Backspace);

            driver.Editor.Script.Should().EndWith("Math.Sqrt");
            driver.Editor.IsCallTipOpen.Should().BeFalse();
        });
    }

    /// <summary>
    /// Verifies that Ctrl+Shift+Space shows the tip for the argument list the caret is already in.
    /// </summary>
    [TestMethod]
    public Task CtrlShiftSpace_InsideExistingCall_ShowsTip()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Math.Pow(2, ");

            driver.PressKey(Key.Space, PhysicalKey.Space, RawInputModifiers.Control | RawInputModifiers.Shift);

            await driver.WaitForCallTipParameterAsync("double y");
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
