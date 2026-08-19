using AwesomeAssertions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;

namespace UITests;

/// <summary>
/// Drives the real Sample app's Basic demo out-of-process via FlaUI to verify the Light/Dark theme
/// picker actually recolors the editor, the way a user would see it — not just that the underlying
/// API sets a property.
/// </summary>
[TestClass]
public class UT_BasicDemoTheme
{
    private static string SampleAppPath =>
        Path.Combine(AppContext.BaseDirectory, "CDS.CSharpScript2.WinForms.Sample.exe");

    [TestMethod]
    public void ThemeRadioButtons_SwitchBetweenDarkAndLight_RecolorsEditorBackground()
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

                var rbDark = window.FindFirstDescendant(cf => cf.ByAutomationId("rbThemeDark")).AsRadioButton();
                var rbLight = window.FindFirstDescendant(cf => cf.ByAutomationId("rbThemeLight")).AsRadioButton();
                rbDark.Should().NotBeNull("the Dark radio button should be reachable by its AutomationId");
                rbLight.Should().NotBeNull("the Light radio button should be reachable by its AutomationId");

                rbDark.Click();
                Thread.Sleep(1000);
                var darkBackground = CaptureAveragePixel(scintillaEditor!);
                IsDark(darkBackground).Should().BeTrue(
                    "selecting Dark should recolor the editor's background, but it captured as {0}", darkBackground);

                rbLight.Click();
                Thread.Sleep(1000);
                var lightBackground = CaptureAveragePixel(scintillaEditor!);
                IsDark(lightBackground).Should().BeFalse(
                    "selecting Light should restore the editor's light background, but it captured as {0}", lightBackground);
            }
            finally
            {
                app.Close();
            }
        });
    }

    /// <summary>
    /// Captures the element and samples a pixel a little inside its top-left corner — inside the
    /// editor's blank background rather than over any text, so the sample reflects the theme's
    /// background color regardless of what script is loaded.
    /// </summary>
    private static System.Drawing.Color CaptureAveragePixel(FlaUI.Core.AutomationElements.AutomationElement element)
    {
        using var capture = Capture.Element(element);
        return capture.Bitmap.GetPixel(10, 60);
    }

    private static bool IsDark(System.Drawing.Color color) =>
        (color.R + color.G + color.B) / 3 < 128;
}
