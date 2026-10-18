using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using CDS.CSharpScript2.Avalonia.Sample;
using CDS.CSharpScript2.AvaloniaEditor;

namespace CDS.CSharpScript2.Avalonia.UITests;

/// <summary>
/// Shows the sample's <see cref="MainWindow"/> headlessly and drives it with simulated input.
/// Must be created and used on the headless UI thread (inside <see cref="HeadlessTestSession.RunAsync"/>).
/// </summary>
internal sealed class SampleWindowDriver : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public SampleWindowDriver()
    {
        Window = new MainWindow();
        Window.Show();

        Editor = Window.FindControl<AvaloniaScriptEditor>("ScriptEditor")!;
        TextEditor = (TextEditor)Editor.Content!;
        Output = Window.FindControl<TextBox>("OutputBox")!;
        Status = Window.FindControl<TextBlock>("StatusText")!;
    }

    public MainWindow Window { get; }

    public AvaloniaScriptEditor Editor { get; }

    public TextEditor TextEditor { get; }

    public TextBox Output { get; }

    public TextBlock Status { get; }

    /// <summary>Focuses the editor, moves the caret to the end, and types <paramref name="text"/>.</summary>
    public void TypeAtEnd(string text)
    {
        TextEditor.TextArea.Focus();
        TextEditor.CaretOffset = TextEditor.Document.TextLength;
        Window.KeyTextInput(text);
    }

    /// <summary>Presses and releases a key with no modifiers.</summary>
    public void PressKey(Key key, PhysicalKey physicalKey)
    {
        Window.KeyPress(key, RawInputModifiers.None, physicalKey, null);
        Window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
    }

    /// <summary>Moves the pointer over the character at <paramref name="offset"/> in the script.</summary>
    public void HoverOverOffset(int offset)
    {
        var textView = TextEditor.TextArea.TextView;
        var location = TextEditor.Document.GetLocation(offset);
        var visual = textView.GetVisualPosition(new TextViewPosition(location), VisualYPosition.LineMiddle)
            - textView.ScrollOffset;

        // Aim a few pixels into the character rather than at its leading edge.
        var target = textView.TranslatePoint(visual + new Vector(3, 0), Window)
            ?? throw new InvalidOperationException("Text view is not in the window.");

        // Arrive from nearby so the text view sees a genuine move, then rest for the hover delay.
        Window.MouseMove(target + new Vector(-20, 0), RawInputModifiers.None);
        Window.MouseMove(target, RawInputModifiers.None);
    }

    /// <summary>
    /// Pumps the headless UI (layout, render, timers) until <paramref name="condition"/> holds,
    /// failing the test if it does not within the timeout.
    /// </summary>
    public async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow + DefaultTimeout;

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail($"Timed out after {DefaultTimeout.TotalSeconds:0}s waiting for: {description}");
            }

            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await Task.Delay(20);
        }
    }

    /// <summary>Waits for the editor's initial analysis pass to report.</summary>
    public Task WaitForInitialAnalysisAsync()
    {
        return WaitUntilAsync(() => Status.Text != "Analysing…", "initial analysis to complete");
    }

    public void Dispose()
    {
        Window.Close();
    }
}
