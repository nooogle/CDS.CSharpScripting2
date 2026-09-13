using AwesomeAssertions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace UITests;

/// <summary>
/// Drives the real Sample app's Basic demo out-of-process via FlaUI to verify that Ctrl+Space
/// invokes code completion the way it does in Visual Studio / VS Code, rather than leaking a
/// literal space character into the script.
/// </summary>
[TestClass]
public class UT_BasicDemoCodeCompletion
{
    /// <remarks>
    /// Regression test: Windows translates Ctrl+Space into a normal WM_CHAR space unless the
    /// KeyDown handler sets <c>SuppressKeyPress</c>. Without it, the leaked space reaches
    /// <c>scintilla_CharAdded</c>, which sees a non-identifier character while the completion list
    /// is already open and cancels it — so the shortcut appeared to type a space and do nothing.
    /// Pressing Enter afterwards distinguishes the two outcomes: with the list still open it accepts
    /// "Console"; with the list cancelled it just inserts a newline after "Consol ".
    /// </remarks>
    [TestMethod]
    public void CtrlSpace_OnPartialKeyword_InvokesCompletionWithoutInsertingASpace()
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
                Keyboard.Type("Consol");
                Thread.Sleep(500);

                BasicDemo.InvokeCompletionList();

                Keyboard.Type(VirtualKeyShort.RETURN);
                Thread.Sleep(300);

                var text = BasicDemo.CopyEditorText();
                text.Should().Be("Console",
                    "Ctrl+Space should reopen the completion list on the partial keyword so Enter " +
                    "accepts 'Console', with no stray space inserted by the shortcut itself; got {0}", text);
            }
            finally
            {
                app.Close();
            }
        });
    }
}
