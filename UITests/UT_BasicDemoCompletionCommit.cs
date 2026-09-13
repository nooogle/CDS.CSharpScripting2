using AwesomeAssertions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;

namespace UITests;

/// <summary>
/// Drives the real Sample app's Basic demo out-of-process via FlaUI to verify that typing a
/// commit character accepts the highlighted completion, the way Visual Studio does — and that a
/// closing bracket only does so while the caret is genuinely inside a bracket.
/// </summary>
/// <remarks>
/// The behaviour is implemented with Scintilla autocomplete fill-up characters. Some hold for any
/// caret position — ";" among them — while the closing bracket is chosen per session from
/// <see cref="CDS.CSharpScript2.CodeCompletion.EnclosingBracket"/>. That helper's own rules are
/// covered by <c>UT_EnclosingBracket</c> in the UnitTests project; these tests exist to prove the
/// halves are actually wired together in the editor.
/// </remarks>
[TestClass]
public class UT_BasicDemoCompletionCommit
{
    [TestMethod]
    public void ClosingParen_InsideArgumentList_AcceptsTheCompletionAndInsertsTheBracket()
    {
        RunInBasicDemo(() =>
        {
            Keyboard.Type("Console.WriteLine(Consol");
            Thread.Sleep(500);
            BasicDemo.InvokeCompletionList();

            Keyboard.Type(")");
            Thread.Sleep(300);

            var text = BasicDemo.CopyEditorText();
            text.Should().Be("Console.WriteLine(Console)",
                "')' should accept the highlighted 'Console' and then insert the bracket itself; got {0}", text);
        });
    }

    [TestMethod]
    public void ClosingBracket_InsideIndexer_AcceptsTheCompletionAndInsertsTheBracket()
    {
        RunInBasicDemo(() =>
        {
            Keyboard.Type("var values = new int[10]; var x = values[valu");
            Thread.Sleep(500);
            BasicDemo.InvokeCompletionList();

            Keyboard.Type("]");
            Thread.Sleep(300);

            var text = BasicDemo.CopyEditorText();
            text.Should().Be("var values = new int[10]; var x = values[values]",
                "']' should accept the highlighted 'values' inside the indexer; got {0}", text);
        });
    }

    [TestMethod]
    public void ClosingParen_OutsideAnyBracket_IsTypedWithoutAcceptingTheCompletion()
    {
        RunInBasicDemo(() =>
        {
            Keyboard.Type("var x = Consol");
            Thread.Sleep(500);
            BasicDemo.InvokeCompletionList();

            Keyboard.Type(")");
            Thread.Sleep(300);

            var text = BasicDemo.CopyEditorText();
            text.Should().Be("var x = Consol)",
                "the caret is not inside a bracket, so ')' stays an ordinary character and the " +
                "list is simply dismissed; got {0}", text);
        });
    }

    [TestMethod]
    public void Semicolon_AcceptsTheCompletionAndInsertsTheStatementTerminator()
    {
        RunInBasicDemo(() =>
        {
            Keyboard.Type("int qwerty = 10; int b = qw");
            Thread.Sleep(700);

            Keyboard.Type(";");
            Thread.Sleep(300);

            var text = BasicDemo.CopyEditorText();
            text.Should().Be("int qwerty = 10; int b = qwerty;",
                "';' should accept the highlighted 'qwerty' before terminating the statement; got {0}", text);
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
                var editor = BasicDemo.FocusEditor(window!);
                editor.Should().NotBeNull("the Scintilla editor control should be reachable by its AutomationId");

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
