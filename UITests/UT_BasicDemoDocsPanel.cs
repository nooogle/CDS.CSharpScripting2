using AwesomeAssertions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace UITests;

/// <summary>
/// Drives the Sample app's Basic demo out-of-process to verify the F1 docs panel: it stays hidden
/// until F1, then shows the page for the symbol at the caret in the column beside the editor.
/// </summary>
[TestClass]
public class UT_BasicDemoDocsPanel
{
    private const string Script = "var shown = Math.Round(2.5, 0); var root = Math.Sqrt(4);";

    [TestMethod]
    public void F1_OnMethodCall_ShowsDocsPanelBesideTheEditor()
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                FindPanel(window!).Should().BeNull("the docs panel is hidden until the user asks for it");

                PlaceCaretInRound(window!);
                Keyboard.Type(VirtualKeyShort.F1);

                var panel = WaitForPanel(window!);
                panel.Should().NotBeNull("F1 on a symbol should show the docs panel");

                var editor = window!.FindFirstDescendant(cf => cf.ByAutomationId("scintillaScriptEditor"))!;
                panel!.BoundingRectangle.Left.Should().BeGreaterThanOrEqualTo(
                    editor.BoundingRectangle.Right - 2,
                    "the panel sits in its own column to the right of the editor");

                var text = WaitForText(panel, "Math.Round");
                text.Should().Contain("Math.Round");
                text.Should().Contain("OVERLOADS");
            }
            finally
            {
                app.Close();
            }
        });
    }

    [TestMethod]
    public void Escape_InDocsPanel_HidesThePanel()
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                PlaceCaretInRound(window!);
                Keyboard.Type(VirtualKeyShort.F1);
                var panel = WaitForPanel(window!);
                panel.Should().NotBeNull("F1 on a symbol should show the docs panel");

                // Focus moves into the panel with a click on its body, as a user reading it would.
                panel!.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Document))?.Click();
                Keyboard.Type(VirtualKeyShort.ESCAPE);

                WaitUntil(() => FindPanel(window!) is null).Should().BeTrue("Escape in the panel should close it");
            }
            finally
            {
                app.Close();
            }
        });
    }

    [TestMethod]
    public void ClickingALink_ShowsTheLinkedPageAndSwitchesToBrowsing()
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                PlaceCaretInRound(window!);
                Keyboard.Type(VirtualKeyShort.F1);
                var panel = WaitForPanel(window!);
                panel.Should().NotBeNull("F1 on a symbol should show the docs panel");
                WaitForText(panel!, "Math.Round");

                // "Math" in the kind line ("Method · Math") links to the type's page.
                window!.SetForeground();
                ClickLink(panel!, "Math");

                WaitForText(panel, "Provides constants and static methods").Should().Contain("METHODS");
                panel.FindFirstDescendant(cf => cf.ByName("Browsing")).Should().NotBeNull("following a link enters browsing mode");
            }
            finally
            {
                app.Close();
            }
        });
    }

    [TestMethod]
    public void MovingTheCaret_WhileFollowing_UpdatesThePage()
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                PlaceCaretInRound(window!);
                Keyboard.Type(VirtualKeyShort.F1);
                var panel = WaitForPanel(window!);
                panel.Should().NotBeNull("F1 on a symbol should show the docs panel");
                WaitForText(panel!, "Math.Round");

                // Move the caret into "Sqrt" on the second statement; the panel follows after a short pause.
                BasicDemo.PressCtrlKey(VirtualKeyShort.END);
                for (var i = 0; i < 6; i++)
                {
                    Keyboard.Type(VirtualKeyShort.LEFT);
                }

                WaitForText(panel, "Math.Sqrt").Should().Contain("Math.Sqrt");
            }
            finally
            {
                app.Close();
            }
        });
    }

    [TestMethod]
    public void BackButton_AfterFollowingALink_ReturnsToThePreviousPage()
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                PlaceCaretInRound(window!);
                Keyboard.Type(VirtualKeyShort.F1);
                var panel = WaitForPanel(window!);
                panel.Should().NotBeNull("F1 on a symbol should show the docs panel");
                WaitForText(panel!, "Math.Round");

                window!.SetForeground();
                ClickLink(panel!, "Math");
                WaitForText(panel, "Provides constants and static methods");

                window!.SetForeground();
                panel.FindFirstDescendant(cf => cf.ByName("◀"))!.AsButton().Click();
                WaitForText(panel, "Math.Round").Should().Contain("OVERLOADS");
            }
            finally
            {
                app.Close();
            }
        });
    }

    [TestMethod]
    public void CloseButton_HidesThePanel_AndF1BringsItBackFresh()
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                PlaceCaretInRound(window!);
                Keyboard.Type(VirtualKeyShort.F1);
                var panel = WaitForPanel(window!);
                panel.Should().NotBeNull("F1 on a symbol should show the docs panel");
                WaitForText(panel!, "Math.Round");

                window!.SetForeground();
                panel!.FindFirstDescendant(cf => cf.ByName("✕"))!.AsButton().Click();
                WaitUntil(() => FindPanel(window!) is null).Should().BeTrue("the close button should hide the panel");

                window.SetForeground();

                // The editor has focus again, so F1 reopens the panel on the symbol at the caret.
                Keyboard.Type(VirtualKeyShort.F1);
                panel = WaitForPanel(window!);
                panel.Should().NotBeNull("F1 should bring the panel back");
                WaitForText(panel!, "Math.Round");
                panel!.FindFirstDescendant(cf => cf.ByName("◀"))!.AsButton().IsEnabled.Should().BeFalse("history starts fresh");
            }
            finally
            {
                app.Close();
            }
        });
    }

    [TestMethod]
    public void ThemeRadioButtons_Dark_RecolorsTheDocsPanel()
    {
        StaThreadRunner.Run(() =>
        {
            using var app = BasicDemo.Launch();
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
            window.Should().NotBeNull("the Basic demo window should appear after launch");

            try
            {
                window!.FindFirstDescendant(cf => cf.ByAutomationId("rbThemeLight"))!.AsRadioButton().Click();
                PlaceCaretInRound(window);
                Keyboard.Type(VirtualKeyShort.F1);
                var panel = WaitForPanel(window);
                panel.Should().NotBeNull("F1 on a symbol should show the docs panel");
                WaitForText(panel!, "Math.Round");
                BrightnessOf(panel!).Should().BeGreaterThan(0.6f, "the light theme has a light panel");

                window.FindFirstDescendant(cf => cf.ByAutomationId("rbThemeDark"))!.AsRadioButton().Click();
                WaitUntil(() => BrightnessOf(panel!) < 0.4f).Should().BeTrue("the dark theme has a dark panel");
            }
            finally
            {
                app.Close();
            }
        });
    }

    /// <summary>The panel's average brightness; text is sparse enough that the background dominates.</summary>
    private static float BrightnessOf(AutomationElement element)
    {
        using var capture = Capture.Element(element);
        using var bitmap = new System.Drawing.Bitmap(capture.Bitmap);

        float total = 0;
        var samples = 0;
        for (var y = 0; y < bitmap.Height; y += 4)
        {
            for (var x = 0; x < bitmap.Width; x += 4)
            {
                total += bitmap.GetPixel(x, y).GetBrightness();
                samples++;
            }
        }

        return total / samples;
    }

    private static void ClickLink(AutomationElement panel, string text)
    {
        var document = panel.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document))!;
        var range = document.Patterns.Text.Pattern.DocumentRange.FindText(text, false, false);
        range.Should().NotBeNull();
        var rectangle = range.GetBoundingRectangles().First();
        Mouse.Click(new System.Drawing.Point(
            (int)(rectangle.Left + rectangle.Width / 2),
            (int)(rectangle.Top + rectangle.Height / 2)));
    }

    private static void PlaceCaretInRound(Window window)
    {
        // Mouse and keyboard input is global, so make sure the app (not another window) receives it.
        window.SetForeground();
        BasicDemo.FocusEditor(window).Should().NotBeNull();
        BasicDemo.SelectAllAndDelete();

        System.Windows.Forms.Clipboard.SetText(Script);
        BasicDemo.PressCtrlKey(VirtualKeyShort.KEY_V);
        Thread.Sleep(300);

        // Back from the end of the line into the middle of "Round".
        BasicDemo.PressCtrlKey(VirtualKeyShort.HOME);
        for (var i = 0; i < Script.IndexOf("Round") + 2; i++)
        {
            Keyboard.Type(VirtualKeyShort.RIGHT);
        }
    }

    private static AutomationElement? FindPanel(Window window)
    {
        var panel = window.FindFirstDescendant(cf => cf.ByAutomationId("docsPanel"));
        return panel is { IsOffscreen: false } ? panel : null;
    }

    private static AutomationElement? WaitForPanel(Window window)
    {
        AutomationElement? panel = null;
        WaitUntil(() => (panel = FindPanel(window)) is not null);
        return panel;
    }

    private static string WaitForText(AutomationElement panel, string expected)
    {
        var text = string.Empty;
        WaitUntil(() =>
        {
            var document = panel.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Document));
            text = document?.Patterns.Text.PatternOrDefault?.DocumentRange.GetText(-1) ?? string.Empty;
            return text.Contains(expected);
        });
        return text;
    }

    private static bool WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(150);
        }

        return condition();
    }
}
