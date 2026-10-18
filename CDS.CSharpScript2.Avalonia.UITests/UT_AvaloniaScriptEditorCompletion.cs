using Avalonia.Input;
using AwesomeAssertions;

namespace CDS.CSharpScript2.Avalonia.UITests;

[TestClass]
public class UT_AvaloniaScriptEditorCompletion
{
    private const string TextVariable = "var text = \"abc\";\n";

    /// <summary>
    /// Verifies that typing a dot after an expression opens a list of its members.
    /// </summary>
    [TestMethod]
    public Task TypingDot_OpensMemberList()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".");

            await driver.WaitUntilAsync(() => driver.Editor.IsCompletionListOpen, "the completion list to open");
            driver.CompletionItems.Should().Contain(["Length", "ToUpper", "Substring"]);
        });
    }

    /// <summary>
    /// Verifies that typing on narrows the list and highlights the best match.
    /// </summary>
    [TestMethod]
    public Task TypingAfterDot_NarrowsListToBestMatch()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".Len");

            await driver.WaitForSelectedCompletionAsync("Length");
            driver.CompletionItems.Should().NotContain("ToUpper");
        });
    }

    /// <summary>
    /// Verifies that the list shows the core's word-initial matches in the core's order
    /// (AvaloniaEdit's own filter is off): "WL" offers exactly WindowLeft and WriteLine.
    /// </summary>
    [TestMethod]
    public Task TypingWordInitials_ShowsCoreMatchesInCoreOrder()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync("Console");

            driver.Type(".WL");

            await driver.WaitUntilAsync(
                () => driver.CompletionItems.SequenceEqual(["WindowLeft", "WriteLine"]),
                "the list to hold exactly WindowLeft, WriteLine");
            driver.SelectedCompletion.Should().Be("WindowLeft");
        });
    }

    /// <summary>
    /// Verifies that Enter inserts the highlighted entry and closes the list.
    /// </summary>
    [TestMethod]
    public Task PressingEnter_InsertsSelectedCompletion()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".Len");
            await driver.WaitForSelectedCompletionAsync("Length");

            driver.PressKey(Key.Enter, PhysicalKey.Enter);

            driver.Editor.Script.Should().EndWith("text.Length");
            driver.Editor.IsCompletionListOpen.Should().BeFalse();
        });
    }

    /// <summary>
    /// Verifies that Tab inserts the highlighted entry.
    /// </summary>
    [TestMethod]
    public Task PressingTab_InsertsSelectedCompletion()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".Len");
            await driver.WaitForSelectedCompletionAsync("Length");

            driver.PressKey(Key.Tab, PhysicalKey.Tab);

            driver.Editor.Script.Should().EndWith("text.Length");
        });
    }

    /// <summary>
    /// Verifies that typing a fill-up character commits the entry and then inserts the character,
    /// which in the case of a dot opens the next member list.
    /// </summary>
    [TestMethod]
    public Task TypingFillUpDot_CommitsThenOpensNextList()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".Len");
            await driver.WaitForSelectedCompletionAsync("Length");

            driver.Type(".");

            driver.Editor.Script.Should().EndWith("text.Length.");
            await driver.WaitUntilAsync(
                () => driver.CompletionItems.Contains("CompareTo"),
                "the int member list to open");
        });
    }

    /// <summary>
    /// Verifies that a semicolon commits the entry too.
    /// </summary>
    [TestMethod]
    public Task TypingSemicolon_CommitsThenInsertsSemicolon()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "var n = text");

            driver.Type(".Len");
            await driver.WaitForSelectedCompletionAsync("Length");

            driver.Type(";");

            driver.Editor.Script.Should().EndWith("var n = text.Length;");
            driver.Editor.IsCompletionListOpen.Should().BeFalse();
        });
    }

    /// <summary>
    /// Verifies that Escape closes the list and leaves the typed text alone.
    /// </summary>
    [TestMethod]
    public Task PressingEscape_ClosesListWithoutInserting()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".Len");
            await driver.WaitForSelectedCompletionAsync("Length");

            driver.PressKey(Key.Escape, PhysicalKey.Escape);

            driver.Editor.IsCompletionListOpen.Should().BeFalse();
            driver.Editor.Script.Should().EndWith("text.Len");
        });
    }

    /// <summary>
    /// Verifies that typing a space (not a fill-up character) dismisses the list without inserting.
    /// </summary>
    [TestMethod]
    public Task TypingSpace_DismissesListWithoutInserting()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".Len");
            await driver.WaitForSelectedCompletionAsync("Length");

            driver.Type(" ");

            driver.Editor.IsCompletionListOpen.Should().BeFalse();
            driver.Editor.Script.Should().EndWith("text.Len ");
        });
    }

    /// <summary>
    /// Verifies that Ctrl+Space opens the list for the identifier at the caret.
    /// </summary>
    [TestMethod]
    public Task CtrlSpace_OpensListForIdentifierAtCaret()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "tex");

            driver.PressKey(Key.Space, PhysicalKey.Space, RawInputModifiers.Control);

            await driver.WaitForSelectedCompletionAsync("text");
            driver.Editor.Script.Should().EndWith("tex");
        });
    }

    /// <summary>
    /// Verifies that backspacing within the word re-filters the open list.
    /// </summary>
    [TestMethod]
    public Task Backspace_RefiltersOpenList()
    {
        return HeadlessTestSession.RunAsync(async () =>
        {
            using var driver = await StartAsync(TextVariable + "text");

            driver.Type(".ToU");
            await driver.WaitForSelectedCompletionAsync("ToUpper");
            driver.CompletionItems.Should().NotContain("ToLower");

            driver.PressKey(Key.Back, PhysicalKey.Backspace);

            await driver.WaitUntilAsync(() => driver.CompletionItems.Contains("ToLower"), "'To' to offer ToLower again");
            driver.Editor.Script.Should().EndWith("text.To");
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
