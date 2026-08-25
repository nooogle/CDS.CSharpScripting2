using AwesomeAssertions;
using FlaUI.Core;
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
    private static string SampleAppPath =>
        Path.Combine(AppContext.BaseDirectory, "CDS.CSharpScript2.WinForms.Sample.exe");

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
            using var app = Application.Launch(SampleAppPath, "--demo=basic");
            using var automation = new UIA3Automation();

            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                var scintillaEditor = window!.FindFirstDescendant(cf => cf.ByAutomationId("scintillaScriptEditor"));
                scintillaEditor.Should().NotBeNull("the Scintilla editor control should be reachable by its AutomationId");

                scintillaEditor!.Click();
                Thread.Sleep(300);

                SelectAllAndDelete();
                Keyboard.Type("Consol");
                Thread.Sleep(500);

                PressCtrlKey(VirtualKeyShort.SPACE);
                Thread.Sleep(700);

                Keyboard.Type(VirtualKeyShort.RETURN);
                Thread.Sleep(300);

                var text = CopyEditorText();
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

    private static void SelectAllAndDelete()
    {
        PressCtrlKey(VirtualKeyShort.KEY_A);
        Thread.Sleep(100);
        Keyboard.Press(VirtualKeyShort.DELETE);
        Thread.Sleep(100);
    }

    private static string CopyEditorText()
    {
        PressCtrlKey(VirtualKeyShort.KEY_A);
        PressCtrlKey(VirtualKeyShort.KEY_C);
        Thread.Sleep(300);
        return System.Windows.Forms.Clipboard.GetText();
    }

    private static void PressCtrlKey(VirtualKeyShort key)
    {
        Keyboard.Press(VirtualKeyShort.CONTROL);
        Keyboard.Type(key);
        Keyboard.Release(VirtualKeyShort.CONTROL);
    }
}
