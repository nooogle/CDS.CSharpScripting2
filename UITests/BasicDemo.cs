using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace UITests;

/// <summary>
/// Shared plumbing for the tests that drive the Sample app's Basic demo out-of-process: launching
/// it, reaching its Scintilla editor, and the keyboard gestures used to set up and read back the
/// script text.
/// </summary>
internal static class BasicDemo
{
    /// <summary>The Sample app executable, copied next to the test assembly by the project reference.</summary>
    public static string SampleAppPath =>
        Path.Combine(AppContext.BaseDirectory, "CDS.CSharpScript2.WinForms.Sample.exe");

    /// <summary>Launches the Sample app with the Basic demo selected.</summary>
    public static Application Launch() => Application.Launch(SampleAppPath, "--demo=basic");

    /// <summary>
    /// Finds the Scintilla editor within <paramref name="window"/> and clicks it so that
    /// subsequent keyboard input reaches it.
    /// </summary>
    /// <param name="window">The Basic demo's main window.</param>
    /// <returns>The editor element, or <see langword="null"/> if it could not be found.</returns>
    public static AutomationElement? FocusEditor(Window window)
    {
        var editor = window.FindFirstDescendant(cf => cf.ByAutomationId("scintillaScriptEditor"));

        if (editor is not null)
        {
            editor.Click();
            Thread.Sleep(300);
        }

        return editor;
    }

    /// <summary>Empties the editor so a test starts from a known script.</summary>
    public static void SelectAllAndDelete()
    {
        PressCtrlKey(VirtualKeyShort.KEY_A);
        Thread.Sleep(100);
        Keyboard.Press(VirtualKeyShort.DELETE);
        Thread.Sleep(100);
    }

    /// <summary>Returns the editor's full text, via select-all and copy.</summary>
    public static string CopyEditorText()
    {
        PressCtrlKey(VirtualKeyShort.KEY_A);
        PressCtrlKey(VirtualKeyShort.KEY_C);
        Thread.Sleep(300);
        return System.Windows.Forms.Clipboard.GetText();
    }

    /// <summary>Presses <paramref name="key"/> with Ctrl held down.</summary>
    /// <param name="key">The key to press alongside Ctrl.</param>
    public static void PressCtrlKey(VirtualKeyShort key)
    {
        Keyboard.Press(VirtualKeyShort.CONTROL);
        Keyboard.Type(key);
        Keyboard.Release(VirtualKeyShort.CONTROL);
    }

    /// <summary>
    /// Opens the completion list on the partial word left of the caret and waits for Roslyn to
    /// populate it. Ctrl+Space is used rather than relying on the typing trigger so that a test
    /// asserting what a subsequent keystroke does to the list knows the list was actually open.
    /// </summary>
    public static void InvokeCompletionList()
    {
        PressCtrlKey(VirtualKeyShort.SPACE);
        Thread.Sleep(700);
    }
}
