using AwesomeAssertions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace UITests;

/// <summary>
/// Drives the real Sample app's Basic demo out-of-process via FlaUI to verify that the completion
/// list offers items that merely contain the typed text, the way Visual Studio and VS Code do.
/// </summary>
/// <remarks>
/// The matching rules themselves are covered by <c>UT_CompletionMatcher</c> and
/// <c>UT_CodeCompletions</c> in the UnitTests project. These tests exist because Scintilla applies
/// filtering of its own: it highlights the first entry beginning with the typed word and, unless
/// <c>AutoCAutoHide</c> is turned off, closes the list outright when no entry does. A substring
/// match survives the engine but dies in the control unless both are handled.
/// </remarks>
[TestClass]
public class UT_BasicDemoSubstringCompletion
{
    [TestMethod]
    public void TextFromInsideAName_OffersAndCommitsThatMember()
    {
        RunInBasicDemo(() =>
        {
            // No Console member begins with "rite"; Write and WriteLine only contain it.
            Keyboard.Type("Console.rite");
            Thread.Sleep(500);
            BasicDemo.InvokeCompletionList();

            Keyboard.Type(VirtualKeyShort.RETURN);
            Thread.Sleep(300);

            var text = BasicDemo.CopyEditorText();
            text.Should().Be("Console.Write",
                "the list should stay open on a substring-only match and Enter should accept the " +
                "highlighted 'Write'; got {0}", text);
        });
    }

    [TestMethod]
    public void TextStartingAnInnerWord_IsRankedBelowPrefixMatches()
    {
        RunInBasicDemo(() =>
        {
            // "Window" starts a word inside SetWindowSize, but WindowHeight starts with it, so
            // the prefix match is the one that should be highlighted.
            Keyboard.Type("Console.Window");
            Thread.Sleep(500);
            BasicDemo.InvokeCompletionList();

            Keyboard.Type(VirtualKeyShort.RETURN);
            Thread.Sleep(300);

            var text = BasicDemo.CopyEditorText();
            text.Should().Be("Console.WindowHeight",
                "substring matches must not displace the items that start with the typed text; got {0}", text);
        });
    }

    /// <summary>
    /// Launches the Basic demo on an STA thread, focuses and clears its editor, runs
    /// <paramref name="body"/>, and closes the app.
    /// </summary>
    /// <param name="body">The test body, run with the editor focused and empty.</param>
    private static void RunInBasicDemo(Action body)
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();

            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                var scintillaEditor = BasicDemo.FocusEditor(window!);
                scintillaEditor.Should().NotBeNull("the Scintilla editor control should be reachable by its AutomationId");

                BasicDemo.SelectAllAndDelete();
                body();
            }
            finally
            {
                app.Close();
            }
        });
    }
}
