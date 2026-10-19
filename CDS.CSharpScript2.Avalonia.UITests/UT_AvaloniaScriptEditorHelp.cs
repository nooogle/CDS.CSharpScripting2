using AwesomeAssertions;
using Avalonia.Input;
using CDS.CSharpScript2.AvaloniaEditor;
using CDS.CSharpScript2.Editors;

namespace CDS.CSharpScript2.Avalonia.UITests;

[TestClass]
public class UT_AvaloniaScriptEditorHelp
{
    private const string Script = "var price = 19.987;\nvar shown = Math.Round(price, 2);";

    /// <summary>
    /// Verifies that F1 raises the help request at the caret.
    /// </summary>
    [TestMethod]
    public Task PressingF1_RaisesHelpRequestedAtCaret()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            driver.TextEditor.CaretOffset = Script.IndexOf("Round", StringComparison.Ordinal) + 2;
            var raised = new List<HelpRequestedEventArgs>();
            driver.Editor.HelpRequested += (_, e) => raised.Add(e);

            driver.PressKey(Key.F1, PhysicalKey.F1);

            raised.Should().ContainSingle().Which.Position.Should().Be(driver.TextEditor.CaretOffset);
        });
    }

    /// <summary>
    /// Verifies that moving the caret raises CaretMoved and updates CaretOffset.
    /// </summary>
    [TestMethod]
    public Task MovingCaret_RaisesCaretMoved()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            var count = 0;
            driver.Editor.CaretMoved += (_, _) => count++;

            driver.TextEditor.CaretOffset = 3;

            count.Should().BeGreaterThan(0);
            driver.Editor.CaretOffset.Should().Be(3);
        });
    }

    /// <summary>
    /// Verifies that the doc page source sees an edit the debounced analysis has not caught up with.
    /// </summary>
    [TestMethod]
    public Task DocPageSource_SeesUnanalysedEdits()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            driver.Editor.Script = "var shown = Math.Round(2.5);";
            var source = new EditorDocPageSource(driver.Editor);

            var page = await source.GetDocPageAsync(
                driver.Editor.Script.IndexOf("Round", StringComparison.Ordinal) + 1,
                CancellationToken.None);

            page.Should().NotBeNull();
        });
    }

    /// <summary>
    /// Verifies that two concurrent requests right after an edit both see the edit.
    /// </summary>
    [TestMethod]
    public Task ConcurrentDocPageRequests_BothSeeTheEdit()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            driver.Editor.Script = "var shown = Math.Round(2.5);";
            var source = new EditorDocPageSource(driver.Editor);
            var position = driver.Editor.Script.IndexOf("Round", StringComparison.Ordinal) + 1;

            var first = source.GetDocPageAsync(position, CancellationToken.None);
            var second = source.GetDocPageAsync(position, CancellationToken.None);

            (await first).Should().NotBeNull();
            (await second).Should().NotBeNull();
        });
    }

    /// <summary>
    /// Verifies that a request cancelled during the refresh does not leave the manager stale
    /// for the next request.
    /// </summary>
    [TestMethod]
    public Task CancelledRefresh_DoesNotStrandLaterRequests()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            driver.Editor.Script = "var shown = Math.Round(2.5);";
            var source = new EditorDocPageSource(driver.Editor);
            var position = driver.Editor.Script.IndexOf("Round", StringComparison.Ordinal) + 1;

            using var cts = new CancellationTokenSource();
            var cancelled = source.GetDocPageAsync(position, cts.Token);
            cts.Cancel();
            try
            {
                await cancelled;
            }
            catch (OperationCanceledException)
            {
            }

            var page = await source.GetDocPageAsync(position, CancellationToken.None);

            page.Should().NotBeNull();
        });
    }

    private static async Task<SampleWindowDriver> StartAsync()
    {
        var driver = new SampleWindowDriver();
        await driver.WaitForInitialAnalysisAsync();
        driver.SetScriptWithCaretAtEnd(Script);
        return driver;
    }
}
