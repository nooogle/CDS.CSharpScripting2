using AwesomeAssertions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.Avalonia.UITests;

[TestClass]
public class UT_DocsPanel
{
    private const string Script = "var price = 19.987;\nvar shown = Math.Round(price, 2);\nvar text = string.Empty;";

    /// <summary>
    /// Verifies that F1 on a method call shows the panel on that method's page, following the caret.
    /// </summary>
    [TestMethod]
    public Task F1_ShowsPanelWithMethodPage()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            driver.IsDocsPanelVisible.Should().BeFalse();

            await PressF1OnAsync(driver, "Round");

            driver.IsDocsPanelVisible.Should().BeTrue();
            driver.DocsTitle.Should().Be("Math.Round");
            driver.DocsState.Should().Be(DocsPanelState.Following);
        });
    }

    /// <summary>
    /// Verifies that while following, moving the caret onto another symbol shows its page.
    /// </summary>
    [TestMethod]
    public Task MovingCaret_UpdatesPage()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            await PressF1OnAsync(driver, "Round");

            driver.MoveCaretTo(OffsetIn("Math"));

            await driver.WaitUntilAsync(() => driver.DocsTitle == "Math", $"docs page 'Math' (showing '{driver.DocsTitle}')");
        });
    }

    /// <summary>
    /// Verifies that the close button hides the panel and closes the controller.
    /// </summary>
    [TestMethod]
    public Task CloseButton_HidesPanel()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            await PressF1OnAsync(driver, "Round");

            driver.ClickDocsButton("✕");

            await driver.WaitUntilAsync(() => !driver.IsDocsPanelVisible, "docs panel to hide");
            driver.DocsState.Should().Be(DocsPanelState.Closed);
        });
    }

    /// <summary>
    /// Verifies that Escape while the panel has keyboard focus hides the panel.
    /// </summary>
    [TestMethod]
    public Task Escape_InPanel_HidesPanel()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            await PressF1OnAsync(driver, "Round");

            driver.DocsPanel.Focus();
            driver.PressKey(Key.Escape, PhysicalKey.Escape);

            await driver.WaitUntilAsync(() => !driver.IsDocsPanelVisible, "docs panel to hide");
            driver.DocsState.Should().Be(DocsPanelState.Closed);
        });
    }

    /// <summary>
    /// Verifies that replacing the editor's environment, which replaces its manager, does not
    /// stop the open panel following the caret.
    /// </summary>
    [TestMethod]
    public Task ChangingEnvironment_WhileOpen_KeepsWorking()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();
            await PressF1OnAsync(driver, "Round");

            driver.Editor.Environment = driver.Editor.Environment;
            driver.MoveCaretTo(OffsetIn("Math"));

            await driver.WaitUntilAsync(() => driver.DocsTitle == "Math", $"docs page 'Math' (showing '{driver.DocsTitle}')");
        });
    }

    /// <summary>
    /// Verifies that a long page scrolls inside the panel rather than stretching it past the window.
    /// </summary>
    [TestMethod]
    public Task LongPage_Scrolls()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync();

            // Short enough that String's member list cannot fit.
            driver.Window.Height = 400;

            driver.MoveCaretTo(OffsetIn("string"));
            driver.PressKey(Key.F1, PhysicalKey.F1);
            await driver.WaitUntilAsync(() => driver.DocsTitle == "String", $"docs page 'String' (showing '{driver.DocsTitle}')");

            var scrollViewer = driver.DocsPanel.GetVisualDescendants().OfType<ScrollViewer>().First();
            await driver.WaitUntilAsync(
                () => scrollViewer.Extent.Height > scrollViewer.Viewport.Height,
                $"the docs body to overflow its viewport (extent {scrollViewer.Extent}, viewport {scrollViewer.Viewport})");

            driver.DocsPanel.Bounds.Height.Should().BeLessThanOrEqualTo(driver.Window.ClientSize.Height);
        });
    }

    private static int OffsetIn(string word) => Script.IndexOf(word, StringComparison.Ordinal) + 1;

    private static async Task PressF1OnAsync(SampleWindowDriver driver, string word)
    {
        driver.MoveCaretTo(OffsetIn(word));
        driver.PressKey(Key.F1, PhysicalKey.F1);

        var expected = word == "Round" ? "Math.Round" : word;
        await driver.WaitUntilAsync(
            () => driver.DocsTitle == expected,
            $"docs page '{expected}' (showing '{driver.DocsTitle}')");
    }

    private static async Task<SampleWindowDriver> StartAsync()
    {
        var driver = new SampleWindowDriver();
        await driver.WaitForInitialAnalysisAsync();
        driver.SetScriptWithCaretAtEnd(Script);
        return driver;
    }
}
